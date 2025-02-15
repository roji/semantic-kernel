// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL;

internal class AzureCosmosDBNoSqlFilterTranslator
{
    private IReadOnlyDictionary<string, string> _storagePropertyNames = null!;
    private ParameterExpression _recordParameter = null!;

    private readonly Dictionary<string, object?> _parameters = new();
    private readonly StringBuilder _sql = new();

    internal (string WhereClause, Dictionary<string, object?> Parameters) Translate(LambdaExpression lambdaExpression, IReadOnlyDictionary<string, string> storagePropertyNames)
    {
        Debug.Assert(this._sql.Length == 0);

        this._storagePropertyNames = storagePropertyNames;

        Debug.Assert(lambdaExpression.Parameters.Count == 1);
        this._recordParameter = lambdaExpression.Parameters[0];

        this.Translate(lambdaExpression.Body);
        return (this._sql.ToString(), this._parameters);
    }

    private void Translate(Expression? node)
    {
        switch (node)
        {
            case BinaryExpression binary:
                this.TranslateBinary(binary);
                return;

            case ConstantExpression constant:
                this.TranslateConstant(constant);
                return;

            case MemberExpression member:
                this.TranslateMember(member);
                return;

            case NewArrayExpression newArray:
                this.TranslateNewArray(newArray);
                return;

            case MethodCallExpression methodCall:
                this.TranslateMethodCall(methodCall);
                return;

            case NewExpression @new:
                this.TranslateNew(@new);
                return;

            case UnaryExpression unary:
                this.TranslateUnary(unary);
                return;

            default:
                throw new NotSupportedException("Unsupported NodeType in filter: " + node?.NodeType);
        }
    }

    private void TranslateBinary(BinaryExpression binary)
    {
        this._sql.Append('(');
        this.Translate(binary.Left);

        this._sql.Append(binary.NodeType switch
        {
            ExpressionType.Equal => " = ",
            ExpressionType.NotEqual => " <> ",

            ExpressionType.GreaterThan => " > ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            ExpressionType.LessThan => " < ",
            ExpressionType.LessThanOrEqual => " <= ",

            ExpressionType.AndAlso => " AND ",
            ExpressionType.OrElse => " OR ",

            _ => throw new NotSupportedException("Unsupported binary expression node type: " + binary.NodeType)
        });

        this.Translate(binary.Right);
        this._sql.Append(')');
    }

    private void TranslateConstant(ConstantExpression constant)
        => this.TranslateConstant(constant.Value);

    private void TranslateConstant(object? constant)
    {
        switch (constant)
        {
            case byte b:
                this._sql.Append(b);
                return;
            case short s:
                this._sql.Append(s);
                return;
            case int i:
                this._sql.Append(i);
                return;
            case long l:
                this._sql.Append(l);
                return;

            case float f:
                this._sql.Append(f);
                return;
            case double d:
                this._sql.Append(d);
                return;

            case string s:
                this._sql.Append('"').Append(s.Replace(@"\", @"\\").Replace("\"", "\\\"")).Append('"');
                return;
            case bool b:
                this._sql.Append(b ? "true" : "false");
                return;
            case Guid g:
                this._sql.Append('"').Append(g.ToString()).Append('"');
                return;

            case DateTimeOffset d:
                this._sql
                    .Append('\'')
                    .Append(d.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFzzz", CultureInfo.InvariantCulture))
                    .Append('\'');
                return;

            case null:
                this._sql.Append("null");
                return;

            default:
                throw new NotSupportedException("Unsupported constant type: " + constant.GetType().Name);
        }
    }

    private void TranslateMember(MemberExpression memberExpression)
    {
        switch (memberExpression)
        {
            case var _ when this.TryGetPropertyAccess(memberExpression, out var column):
                this._sql.Append(AzureCosmosDBNoSQLConstants.ContainerAlias).Append("[\"").Append(column).Append("\"]");
                return;

            // Identify captured lambda variables, translate to Cosmos parameters (@foo, @bar...)
            case var _ when TryGetCapturedValue(memberExpression, out var name, out var value):
                // Duplicate parameter name, create a new parameter with a different name
                // TODO: Share the same parameter when it references the same captured value
                if (this._parameters.ContainsKey(name))
                {
                    var baseName = name;
                    var i = 0;
                    do
                    {
                        name = baseName + (i++);
                    } while (this._parameters.ContainsKey(name));
                }

                name = '@' + name;
                this._parameters.Add(name, value);
                this._sql.Append(name);
                return;

            default:
                throw new NotSupportedException($"Member access for '{memberExpression.Member.Name}' is unsupported - only member access over the filter parameter are supported");
        }
    }

    private void TranslateNewArray(NewArrayExpression newArray)
    {
        this._sql.Append('[');

        for (var i = 0; i < newArray.Expressions.Count; i++)
        {
            if (i > 0)
            {
                this._sql.Append(", ");
            }

            this.Translate(newArray.Expressions[i]);
        }

        this._sql.Append(']');
    }

    private void TranslateMethodCall(MethodCallExpression methodCall)
    {
        switch (methodCall)
        {
            // Enumerable.Contains()
            case { Method.Name: nameof(Enumerable.Contains), Arguments: [var source, var item] } contains
                when contains.Method.DeclaringType == typeof(Enumerable):
                this.TranslateContains(source, item);
                return;

            // List.Contains()
            case
            {
                Method:
                {
                    Name: nameof(Enumerable.Contains),
                    DeclaringType: { IsGenericType: true } declaringType
                },
                Object: Expression source,
                Arguments: [var item]
            } when declaringType.GetGenericTypeDefinition() == typeof(List<>):
                this.TranslateContains(source, item);
                return;

            default:
                throw new NotSupportedException($"Unsupported method call: {methodCall.Method.DeclaringType?.Name}.{methodCall.Method.Name}");
        }
    }

    private void TranslateNew(NewExpression @new)
    {
        if (TryEvaluate(@new, out var constant))
        {
            this.TranslateConstant((ConstantExpression)constant);
            return;
        }

        throw new NotSupportedException($"Unsupported new expression for type: {@new.Constructor?.DeclaringType?.Name}");

#pragma warning disable CA1031 // Catch a more specific exception
        static bool TryEvaluate(Expression expression, out object? value)
        {
            try
            {
                value = Expression.Lambda<Func<object>>(
                        Expression.Convert(expression, typeof(object)))
                    .Compile(preferInterpretation: true)
                    .Invoke();
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }
#pragma warning restore CA1031
    }

    private void TranslateContains(Expression source, Expression item)
    {
        this._sql.Append("ARRAY_CONTAINS(");
        this.Translate(source);
        this._sql.Append(", ");
        this.Translate(item);
        this._sql.Append(')');
    }

    private void TranslateUnary(UnaryExpression unary)
    {
        switch (unary)
        {
            // Special handling for !(a == b) and !(a != b)
            case { NodeType: ExpressionType.Not }:
                if (unary.Operand is BinaryExpression { NodeType: ExpressionType.Equal or ExpressionType.NotEqual } binary)
                {
                    this.TranslateBinary(
                        Expression.MakeBinary(
                            binary.NodeType is ExpressionType.Equal ? ExpressionType.NotEqual : ExpressionType.Equal,
                            binary.Left,
                            binary.Right));
                    return;
                }

                this._sql.Append("(NOT ");
                this.Translate(unary.Operand);
                this._sql.Append(')');
                return;

            // Ignore casts up to object - these get introduced e.g. when a generic property is compared to null
            case { NodeType: ExpressionType.Convert } when unary.Type == typeof(object):
                this.Translate(unary.Operand);
                return;

            default:
                throw new NotSupportedException("Unsupported unary expression node type: " + unary.NodeType);
        }
    }

    private bool TryGetPropertyAccess(Expression expression, [NotNullWhen(true)] out string? column)
    {
        if (expression is MemberExpression member && member.Expression == this._recordParameter)
        {
            if (!this._storagePropertyNames.TryGetValue(member.Member.Name, out column))
            {
                throw new InvalidOperationException($"Property name '{member.Member.Name}' provided as part of the filter clause is not a valid property name.");
            }

            return true;
        }

        column = null;
        return false;
    }

    private static bool TryGetCapturedValue(Expression expression, [NotNullWhen(true)] out string? name, out object? value)
    {
        if (expression is MemberExpression { Expression: ConstantExpression constant, Member: FieldInfo fieldInfo }
            && constant.Type.Attributes.HasFlag(TypeAttributes.NestedPrivate)
            && Attribute.IsDefined(constant.Type, typeof(CompilerGeneratedAttribute), inherit: true))
        {
            name = fieldInfo.Name;
            value = fieldInfo.GetValue(constant.Value);
            return true;
        }

        name = null;
        value = null;
        return false;
    }
}
