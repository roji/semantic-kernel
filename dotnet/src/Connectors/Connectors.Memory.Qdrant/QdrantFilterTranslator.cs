// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Google.Protobuf.Collections;
using Qdrant.Client.Grpc;

namespace Microsoft.SemanticKernel.Connectors.Qdrant;

internal class QdrantFilterTranslator
{
    private IReadOnlyDictionary<string, string> _storagePropertyNames = null!;
    private ParameterExpression _recordParameter = null!;

    internal Filter Translate(LambdaExpression lambdaExpression, IReadOnlyDictionary<string, string> storagePropertyNames)
    {
        this._storagePropertyNames = storagePropertyNames;

        Debug.Assert(lambdaExpression.Parameters.Count == 1);
        this._recordParameter = lambdaExpression.Parameters[0];

        return this.Visit(lambdaExpression.Body);
    }

    private Filter Visit(Expression? node)
        => node switch
        {
            BinaryExpression { NodeType: ExpressionType.Equal } equal => this.VisitEqual(equal, negated: false),
            BinaryExpression { NodeType: ExpressionType.NotEqual } notEqual => this.VisitEqual(notEqual, negated: true),

            BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso => this.VisitAndAlso(andAlso),
            BinaryExpression { NodeType: ExpressionType.OrElse } orElse => this.VisitOrElse(orElse),

            // MemberExpression member => this.VisitMember(member),

            // null => null, // TODO: Not sure

            _ => throw new NotSupportedException("Unsupported expression type: " + node.GetType().Name)
        };

    private Filter VisitEqual(BinaryExpression equal, bool negated)
    {
        return TryProcessEqual(equal.Left, equal.Right, out var result)
            ? result
            : TryProcessEqual(equal.Right, equal.Left, out result)
                ? result
                : throw new NotSupportedException("Equality expression not supported by Qdrant");

        bool TryProcessEqual(Expression first, Expression second, [NotNullWhen(true)] out Filter? result)
        {
            // TODO: Captured variable
            // TODO: Nullable
            if (first is MemberExpression memberExpression
                && memberExpression.Expression == this._recordParameter
                && second is ConstantExpression { Value: var constantValue })
            {
                if (!this._storagePropertyNames.TryGetValue(memberExpression.Member.Name, out var storagePropertyName))
                {
                    throw new InvalidOperationException($"Property name '{memberExpression.Member.Name}' provided as part of the filter clause is not a valid property name.");
                }

                var condition = constantValue is null
                    ? new Condition { IsNull = new() { Key = storagePropertyName } }
                    : new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = storagePropertyName,
                            Match = constantValue switch
                            {
                                string stringValue => new Match { Keyword = stringValue },
                                int intValue => new Match { Integer = intValue },
                                long longValue => new Match { Integer = longValue },
                                bool boolValue => new Match { Boolean = boolValue },

                                _ => throw new InvalidOperationException($"Unsupported filter value type '{constantValue.GetType().Name}'.")
                            }
                        }
                    };

                var filter = new Filter();
                if (negated)
                {
                    filter.MustNot.Add(condition);
                }
                else
                {
                    filter.Must.Add(condition);
                }

                result = filter;
                return true;
            }

            result = null;
            return false;
        }
    }

    private Filter VisitAndAlso(BinaryExpression andAlso)
    {
        var left = this.Visit(andAlso.Left);
        var right = this.Visit(andAlso.Right);

        // Qdrant doesn't allow arbitrary nesting of logical operators, only one MUST list (AND), one SHOULD list (OR), and one MUST_NOT list (AND NOT).
        // We can combine MUST and MUST_NOT; but we can only combine SHOULD if it's the *only* thing on the one side (no MUST/MUST_NOT), and there's no SHOULD on the other (only MUST/MUST_NOT).
        if (left.Should.Count > 0)
        {
            return ProcessWithShould(left, right);
        }

        if (right.Should.Count > 0)
        {
            return ProcessWithShould(right, left);
        }

        left.Must.AddRange(right.Must);
        left.MustNot.AddRange(right.MustNot);

        return left;

        static Filter ProcessWithShould(Filter filterWithShould, Filter otherFilter)
        {
            if (filterWithShould.Must.Count > 0 || filterWithShould.MustNot.Count > 0 || otherFilter.Should.Count > 0)
            {
                throw new NotSupportedException("Qdrant does not support the given logical operator combination");
            }

            otherFilter.Should.AddRange(filterWithShould.Should);
            return otherFilter;
        }
    }

    private Filter VisitOrElse(BinaryExpression orElse)
    {
        // Qdrant doesn't allow arbitrary nesting of logical operators, only one MUST list (AND), one SHOULD list (OR), and one MUST_NOT list (AND NOT).
        // As a result, we can only combine single conditions with OR - the moment there's a nested AND we can't.

        var left = this.Visit(orElse.Left);
        var right = this.Visit(orElse.Right);

        var result = new Filter();
        result.Should.AddRange(GetShouldConditions(left));
        result.Should.AddRange(GetShouldConditions(right));
        return result;

        static RepeatedField<Condition> GetShouldConditions(Filter filter)
            => filter switch
            {
                { Must.Count: 0, MustNot.Count: 0 } => filter.Should,
                { Must.Count: 1, MustNot.Count: 0, Should.Count: 0 } => [filter.Must[0]],
                { Must.Count: 0, MustNot.Count: 1, Should.Count: 0 } => [filter.MustNot[0]],

                _ => throw new NotSupportedException("Qdrant does not support the given logical operator combination")
            };
    }
}
