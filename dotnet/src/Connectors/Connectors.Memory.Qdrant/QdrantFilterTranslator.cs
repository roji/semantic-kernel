// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        return this.Translate(lambdaExpression.Body);
    }

    private Filter Translate(Expression? node)
        => node switch
        {
            BinaryExpression { NodeType: ExpressionType.Equal } equal => this.TranslateEqual(equal.Left, equal.Right),
            BinaryExpression { NodeType: ExpressionType.NotEqual } notEqual => this.TranslateEqual(notEqual.Left, notEqual.Right, negated: true),

            BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso => this.TranslateAndAlso(andAlso.Left, andAlso.Right),
            BinaryExpression { NodeType: ExpressionType.OrElse } orElse => this.TranslateOrElse(orElse.Left, orElse.Right),

            // TODO: Other Contains variants (e.g. List.Contains)
            MethodCallExpression
                {
                    Method.Name: nameof(Enumerable.Contains),
                    Arguments: [var source, var item]
                } contains when contains.Method.DeclaringType == typeof(Enumerable)
                => this.TranslateContains(source, item),

            _ => throw new NotSupportedException("Qdrant does not support the following expression type in filters: " + node?.GetType().Name)
        };

    private Filter TranslateEqual(Expression left, Expression right, bool negated = false)
    {
        return TryProcessEqual(left, right, out var result)
            ? result
            : TryProcessEqual(right, left, out result)
                ? result
                : throw new NotSupportedException("Equality expression not supported by Qdrant");

        bool TryProcessEqual(Expression first, Expression second, [NotNullWhen(true)] out Filter? result)
        {
            // TODO: Captured variable
            // TODO: Nullable
            if (this.TryTranslateFieldAccess(first, out var storagePropertyName) && second is ConstantExpression { Value: var constantValue })
            {
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

    #region Logical operators

    private Filter TranslateAndAlso(Expression left, Expression right)
    {
        var leftFilter = this.Translate(left);
        var rightFilter = this.Translate(right);

        // Qdrant doesn't allow arbitrary nesting of logical operators, only one MUST list (AND), one SHOULD list (OR), and one MUST_NOT list (AND NOT).
        // We can combine MUST and MUST_NOT; but we can only combine SHOULD if it's the *only* thing on the one side (no MUST/MUST_NOT), and there's no SHOULD on the other (only MUST/MUST_NOT).
        if (leftFilter.Should.Count > 0)
        {
            return ProcessWithShould(leftFilter, rightFilter);
        }

        if (rightFilter.Should.Count > 0)
        {
            return ProcessWithShould(rightFilter, leftFilter);
        }

        leftFilter.Must.AddRange(rightFilter.Must);
        leftFilter.MustNot.AddRange(rightFilter.MustNot);

        return leftFilter;

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

    private Filter TranslateOrElse(Expression left, Expression right)
    {
        // Qdrant doesn't allow arbitrary nesting of logical operators, only one MUST list (AND), one SHOULD list (OR), and one MUST_NOT list (AND NOT).
        // As a result, we can only combine single conditions with OR - the moment there's a nested AND we can't.

        var leftFilter = this.Translate(left);
        var rightFilter = this.Translate(right);

        var result = new Filter();
        result.Should.AddRange(GetShouldConditions(leftFilter));
        result.Should.AddRange(GetShouldConditions(rightFilter));
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

    #endregion Logical operators

    private Filter TranslateContains(Expression source, Expression item)
    {
        // TODO: Inline/parameterized array?
        if (this.TryTranslateFieldAccess(source, out _))
        {
            // Oddly, in Qdrant, tag list contains is handled using a Match condition, just like equality.
            return this.TranslateEqual(source, item);
        }

        throw new NotSupportedException("Contains only supported over Qdrant list fields");
    }

    private bool TryTranslateFieldAccess(Expression expression, [NotNullWhen(true)] out string? storagePropertyName)
    {
        if (expression is MemberExpression memberExpression && memberExpression.Expression == this._recordParameter)
        {
            if (!this._storagePropertyNames.TryGetValue(memberExpression.Member.Name, out storagePropertyName))
            {
                throw new InvalidOperationException($"Property name '{memberExpression.Member.Name}' provided as part of the filter clause is not a valid property name.");
            }

            return true;
        }

        storagePropertyName = null;
        return false;
    }
}
