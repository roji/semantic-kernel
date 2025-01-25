﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.SemanticKernel.Connectors.Redis;

internal class RedisFilterTranslator
{
    private IReadOnlyDictionary<string, string> _storagePropertyNames = null!;
    private ParameterExpression _recordParameter = null!;
    private readonly StringBuilder _filter = new();

    internal string Translate(LambdaExpression lambdaExpression, IReadOnlyDictionary<string, string> storagePropertyNames)
    {
        this._storagePropertyNames = storagePropertyNames;

        Debug.Assert(lambdaExpression.Parameters.Count == 1);
        this._recordParameter = lambdaExpression.Parameters[0];

        this._filter.Clear();
        this.Translate(lambdaExpression.Body);
        return this._filter.ToString();
    }

    private void Translate(Expression? node)
    {
        switch (node)
        {
            case BinaryExpression
            {
                NodeType: ExpressionType.Equal or ExpressionType.NotEqual
                or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
                or ExpressionType.LessThan or ExpressionType.LessThanOrEqual
            } binary:
                this.TranslateEqualityComparison(binary);
                return;

            case BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso:
                this.TranslateAndAlso(andAlso.Left, andAlso.Right);
                return;
            case BinaryExpression { NodeType: ExpressionType.OrElse } orElse:
                this.TranslateOrElse(orElse.Left, orElse.Right);
                return;
            case UnaryExpression { NodeType: ExpressionType.Not } not:
                this.TranslateNot(not.Operand);
                return;

            // TODO: Other Contains variants (e.g. List.Contains)
            case MethodCallExpression
                {
                    Method.Name: nameof(Enumerable.Contains),
                    Arguments: [var source, var item]
                } contains when contains.Method.DeclaringType == typeof(Enumerable):
                this.TranslateContains(source, item);
                return;

            default:
                throw new NotSupportedException("Redis does not support the following NodeType in filters: " + node?.NodeType);
        }
    }

    private void TranslateEqualityComparison(BinaryExpression binary)
    {
        if (!TryProcessEqual(binary.Left, binary.Right) && !TryProcessEqual(binary.Right, binary.Left))
        {
            throw new NotSupportedException("Binary expression not supported by Redis");
        }

        bool TryProcessEqual(Expression first, Expression second)
        {
            // TODO: Nullable
            if (this.TryTranslateFieldAccess(first, out var storagePropertyName)
                && TryGetConstant(second, out var constantValue))
            {
                // Numeric negation has a special syntax (!=), for the rest we nest in a NOT
                if (binary.NodeType is ExpressionType.NotEqual && constantValue is not int or long or float or double)
                {
                    this.TranslateNot(Expression.Equal(first, second));
                    return true;
                }

                // https://redis.io/docs/latest/develop/interact/search-and-query/query/exact-match
                this._filter.Append('@').Append(storagePropertyName);

                this._filter.Append(
                    binary.NodeType switch
                    {
                        ExpressionType.Equal when constantValue is int or long or float or double => $" == {constantValue}",
                        ExpressionType.Equal when constantValue is string stringValue
#if NETSTANDARD2_0
                            => $$""":{"{{stringValue.Replace("\"", "\"\"")}}"}""",
#else
                            => $$""":{"{{stringValue.Replace("\"", "\\\"", StringComparison.Ordinal)}}"}""",
#endif
                        ExpressionType.Equal when constantValue is null => throw new NotSupportedException("Null value type not supported"), // TODO

                        ExpressionType.NotEqual when constantValue is int or long or float or double => $" != {constantValue}",
                        ExpressionType.NotEqual => throw new InvalidOperationException("Unreachable"), // Handled above

                        ExpressionType.GreaterThan => $" > {constantValue}",
                        ExpressionType.GreaterThanOrEqual => $" >= {constantValue}",
                        ExpressionType.LessThan => $" < {constantValue}",
                        ExpressionType.LessThanOrEqual => $" <= {constantValue}",

                        _ => throw new InvalidOperationException("Unsupported equality/comparison")
                    });

                return true;
            }

            return false;
        }
    }

    #region Logical operators

    private void TranslateAndAlso(Expression left, Expression right)
    {
        // https://redis.io/docs/latest/develop/interact/search-and-query/query/combined/#and
        this._filter.Append('(');
        this.Translate(left);
        this._filter.Append(' ');
        this.Translate(right);
        this._filter.Append(')');
    }

    private void TranslateOrElse(Expression left, Expression right)
    {
        // https://redis.io/docs/latest/develop/interact/search-and-query/query/combined/#or
        this._filter.Append('(');
        this.Translate(left);
        this._filter.Append(" | ");
        this.Translate(right);
        this._filter.Append(')');
    }

    private void TranslateNot(Expression expression)
    {
        // https://redis.io/docs/latest/develop/interact/search-and-query/query/combined/#not
        this._filter.Append("(-");
        this.Translate(expression);
        this._filter.Append(')');
    }

    #endregion Logical operators

    private void TranslateContains(Expression source, Expression item)
    {
        // Contains over tag field
        if (this.TryTranslateFieldAccess(source, out var storagePropertyName)
            && TryGetConstant(item, out var itemConstant)
            && itemConstant is string stringConstant)
        {
            this._filter
                .Append('@')
                .Append(storagePropertyName)
                .Append(":{")
                .Append(stringConstant)
                .Append('}');
            return;
        }

        throw new NotSupportedException("Contains supported only over tag field");
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

    private static bool TryGetConstant(Expression expression, out object? constantValue)
    {
        switch (expression)
        {
            case ConstantExpression { Value: var v }:
                constantValue = v;
                return true;

            // This identifies compiler-generated closure types which contain captured variables.
            case MemberExpression { Expression: ConstantExpression constant, Member: FieldInfo fieldInfo }
                when constant.Type.Attributes.HasFlag(TypeAttributes.NestedPrivate)
                     && Attribute.IsDefined(constant.Type, typeof(CompilerGeneratedAttribute), inherit: true):
                constantValue = fieldInfo.GetValue(constant.Value);
                return true;

            default:
                constantValue = null;
                return false;
        }
    }
}
