using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Interfaces;
using static Supabase.Postgrest.Constants;

// ReSharper disable InvalidXmlDocComment

namespace Supabase.Postgrest.Linq

{
    /// <summary>
    /// Helper class for parsing Where linq queries.
    /// </summary>
    internal class WhereExpressionVisitor : ExpressionVisitor
    {
        /// <summary>
        /// The filter resulting from this Visitor, capable of producing nested filters.
        /// </summary>
        public QueryFilter? Filter { get; private set; }

        /// <summary>
        /// An entry point that will be used to populate <see cref="Filter"/>.
        /// This method handles comparisons, logical operations, and simple arithmetic expressions in a Where clause.
        /// 
        /// Examples:
        ///     <code>Table&lt;Movies&gt;().Where(x => x.Name == "Top Gun").Get();</code>
        ///     <code>Table&lt;Movies&gt;().Where(x => x.Rating > 5 &amp;&amp; x.Year == 1986).Get();</code>
        ///     <code>Table&lt;Movies&gt;().Where(x => x.Rating >= maxRating - 1).Get();</code>
        /// </summary>
        /// <param name="node">The binary expression to process, such as a comparison (e.g., x.Name == "Top Gun") or logical operation (e.g., x.Rating > 5 && x.Year == 1986).</param>
        /// <returns>The processed expression, typically the input <paramref name="node"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the left side of the expression does not correspond to a property with a <see cref="ColumnAttribute"/> or <see cref="PrimaryKeyAttribute"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown if the right side of the expression cannot be evaluated to a constant value.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="Filter"/> is not set after processing the expression.</exception>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var op = GetMappedOperator(node);

            // Handle logical operations (e.g., x.Rating > 5 && x.Year == 1986)
            if (IsLogicalOperation(node.NodeType))
            {
                var conditions = FlattenLogicalConditions(node, op);
                Filter = new QueryFilter(op, conditions);
                return node;
            }

            // Handle simple comparisons (e.g., x.Name == "Top Gun" or x.Rating >= maxRating - 1)
            var column = ExtractColumnName(node.Left);
            var rightValue = EvaluateRightExpression(node.Right);

            // Define the filter for a simple comparison
            Filter = new QueryFilter(column, op, rightValue);
            return node;
        }

        /// <summary>
        /// Flattens a tree of logical conditions (e.g., AND, OR) into a single list of conditions at the same level.
        /// </summary>
        /// <param name="node">The binary expression node representing a logical operation.</param>
        /// <param name="op">The operator (e.g., AND, OR) for the logical operation.</param>
        /// <returns>A list of filters representing all conditions at the same level.</returns>
        private List<IPostgrestQueryFilter> FlattenLogicalConditions(BinaryExpression node, Operator op)
        {
            var conditions = new List<IPostgrestQueryFilter>();

            // Recursively flatten the left and right sides
            FlattenLogicalConditionsRecursive(node, op, conditions);

            return conditions;
        }

        /// <summary>
        /// Recursively flattens a tree of logical conditions into a list of filters.
        /// </summary>
        /// <param name="node">The current binary expression node.</param>
        /// <param name="op">The operator (e.g., AND, OR) for the logical operation.</param>
        /// <param name="conditions">The list to accumulate the flattened conditions.</param>
        private void FlattenLogicalConditionsRecursive(BinaryExpression node, Operator op, List<IPostgrestQueryFilter> conditions)
        {
            // If the node is a logical operation with the same operator, recurse into its children
            if (IsLogicalOperation(node.NodeType) && GetMappedOperator(node) == op)
            {
                if (node.Left is BinaryExpression leftBinary)
                {
                    FlattenLogicalConditionsRecursive(leftBinary, op, conditions);
                }
                else
                {
                    conditions.Add(ProcessSubExpression(node.Left));
                }

                if (node.Right is BinaryExpression rightBinary)
                {
                    FlattenLogicalConditionsRecursive(rightBinary, op, conditions);
                }
                else
                {
                    conditions.Add(ProcessSubExpression(node.Right));
                }
            }
            else
            {
                // If the node is not a logical operation (or has a different operator), process it as a single condition
                conditions.Add(ProcessSubExpression(node));
            }
        }

        /// <summary>
        /// Determines if the node type represents a logical operation (AND, OR).
        /// </summary>
        /// <param name="nodeType">The type of the expression node.</param>
        /// <returns>True if the node type is a logical operation; otherwise, false.</returns>
        private static bool IsLogicalOperation(ExpressionType nodeType)
        {
            return nodeType == ExpressionType.And ||
                   nodeType == ExpressionType.Or ||
                   nodeType == ExpressionType.AndAlso ||
                   nodeType == ExpressionType.OrElse;
        }

        /// <summary>
        /// Processes a subexpression and returns the resulting filter.
        /// </summary>
        /// <param name="expression">The subexpression to process.</param>
        /// <returns>The filter generated by the subexpression.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the subexpression does not produce a valid filter.</exception>
        private IPostgrestQueryFilter ProcessSubExpression(Expression expression)
        {
            var visitor = new WhereExpressionVisitor();
            visitor.Visit(expression);
            return visitor.Filter ?? throw new InvalidOperationException($"Subexpression '{expression}' did not produce a valid filter.");
        }

        /// <summary>
        /// Extracts the column name from the left side of a binary expression.
        /// </summary>
        /// <param name="left">The left side expression, expected to be a property access.</param>
        /// <returns>The column name corresponding to the property.</returns>
        /// <exception cref="ArgumentException">Thrown if the left side does not correspond to a property with a <see cref="ColumnAttribute"/> or <see cref="PrimaryKeyAttribute"/>.</exception>
        private string ExtractColumnName(Expression left)
        {
            if (left is MemberExpression leftMember)
            {
                return GetColumnFromMemberExpression(leftMember);
            }
            if (left is UnaryExpression leftUnary && leftUnary.NodeType == ExpressionType.Convert &&
                leftUnary.Operand is MemberExpression leftOperandMember)
            {
                return GetColumnFromMemberExpression(leftOperandMember);
            }

            throw new ArgumentException(
                $"Left side of expression: '{left}' is expected to be a property with a ColumnAttribute or PrimaryKeyAttribute");
        }

        /// <summary>
        /// Evaluates the right side of a binary expression to produce a constant value, applying special handling for certain types.
        /// </summary>
        /// <param name="right">The right side expression to evaluate.</param>
        /// <returns>The evaluated value of the expression, formatted appropriately for use in a PostgREST query.</returns>
        /// <exception cref="NotSupportedException">Thrown if the right side cannot be evaluated to a constant value.</exception>
        private object EvaluateRightExpression(Expression right)
        {
            right = Visit(right); // Process the right expression

            object value = right switch
            {
                ConstantExpression constant => constant.Value,
                MemberExpression member => EvaluateExpression(member),
                NewExpression newExpr => EvaluateExpression(newExpr),
                UnaryExpression unary => EvaluateExpression(unary),
                BinaryExpression binary => EvaluateBinaryExpression(binary) ?? throw new NotSupportedException(
                    $"Binary expression '{binary}' on the right side is not supported. Only constant values or simple expressions are allowed."),
                _ => throw new NotSupportedException(
                    $"Right side of expression: '{right}' is not supported. Expected a constant, member, new, unary, or simple binary expression.")
            };

            return value switch
            {
                DateTime dateTime => dateTime,
                DateTimeOffset dateTimeOffset => dateTimeOffset,
                Guid guid => guid.ToString(),
                Enum enumValue => enumValue,
                _ => value
            };
        }

        /// <summary>
        /// Evaluates an expression to produce a constant value.
        /// </summary>
        /// <typeparam name="TExpression">The type of the expression to evaluate (e.g., MemberExpression, NewExpression, UnaryExpression).</typeparam>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The evaluated value of the expression.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the expression cannot be evaluated.</exception>
        private object EvaluateExpression<TExpression>(TExpression expression) where TExpression : Expression
        {
            try
            {
                var lambda = Expression.Lambda(expression);
                var compiled = lambda.Compile();
                return compiled.DynamicInvoke();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to evaluate {typeof(TExpression).Name.ToLower()}: '{expression}'.", ex);
            }
        }

        /// <summary>
        /// Evaluates a binary expression to compute its constant value, if possible.
        /// </summary>
        /// <param name="binaryExpression">The binary expression to evaluate (e.g., 'x - 5').</param>
        /// <returns>The computed value of the expression as an object, or null if the expression cannot be evaluated.</returns>
        /// <remarks>
        /// Returns null if the expression cannot be evaluated due to unresolved variables or invalid operations.
        /// The calling code should handle the null return value appropriately.
        /// </remarks>
        private object? EvaluateBinaryExpression(BinaryExpression binaryExpression)
        {
            try
            {
                var lambda = Expression.Lambda(binaryExpression);
                var compiled = lambda.Compile();
                return compiled.DynamicInvoke();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Called when evaluating a method 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var obj = node.Object as MemberExpression;

            if (obj == null)
                throw new ArgumentException(
                    $"Calling context '{node.Object}' is expected to be a member of or derived from `BaseModel`");

            var column = GetColumnFromMemberExpression(obj);

            if (column == null)
                throw new ArgumentException(
                    $"Left side of expression: '{node.ToString()}' is expected to be property with a ColumnAttribute or PrimaryKeyAttribute");

            switch (node.Method.Name)
            {
                // Includes String.Contains and IEnumerable.Contains
                case nameof(String.Contains):

                    if (typeof(ICollection).IsAssignableFrom(node.Method.DeclaringType))
                        Filter = new QueryFilter(column, Operator.Contains, GetArgumentValues(node));
                    else
                        Filter = new QueryFilter(column, Operator.Like, "*" + GetArgumentValues(node).First() + "*");

                    break;
                default:
                    throw new NotImplementedException("Unsupported method");
            }

            return node;
        }

        /// <summary>
        /// Gets a column name (postgrest) from a Member Expression (used on BaseModel)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string GetColumnFromMemberExpression(MemberExpression node)
        {
            var type = node.Member.ReflectedType;
            var prop = type?.GetProperty(node.Member.Name);
            if (prop == null)
            {
                return node.Member.Name;
            }

            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>(true);
            if (columnAttr != null)
            {
                return columnAttr.ColumnName;
            }

            var primaryKeyAttr = prop.GetCustomAttribute<PrimaryKeyAttribute>(true);
            if (primaryKeyAttr != null)
            {
                return primaryKeyAttr.ColumnName;
            }

            return node.Member.Name;
        }

        /// <summary>
        /// Get the value from a MemberExpression, which includes both fields and properties.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private object GetMemberExpressionValue(MemberExpression member)
        {
            if (member.Member is FieldInfo field)
            {
                var obj = Expression.Lambda(member.Expression).Compile().DynamicInvoke();
                return field.GetValue(obj);
            }

            var lambda = Expression.Lambda(member);
            var func = lambda.Compile();
            return func.DynamicInvoke();
        }

        /// <summary>
        /// Creates map between linq <see cref="ExpressionType"/> and <see cref="Constants.Operator"/>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Operator GetMappedOperator(Expression node)
        {
            return node.NodeType switch
            {
                ExpressionType.Not => Operator.Not,
                ExpressionType.And => Operator.And,
                ExpressionType.AndAlso => Operator.And,
                ExpressionType.OrElse => Operator.Or,
                ExpressionType.Or => Operator.Or,
                ExpressionType.Equal => Operator.Equals,
                ExpressionType.NotEqual => Operator.NotEqual,
                ExpressionType.LessThan => Operator.LessThan,
                ExpressionType.GreaterThan => Operator.GreaterThan,
                ExpressionType.LessThanOrEqual => Operator.LessThanOrEqual,
                ExpressionType.GreaterThanOrEqual => Operator.GreaterThanOrEqual,
                _ => Operator.Equals
            };
        }

        /// <summary>
        /// Gets arguments from a method call expression, (i.e. x => x.Name.Contains("Top")) &lt;- where `Top` is the argument on the called method `Contains`
        /// </summary>
        /// <param name="methodCall"></param>
        /// <returns></returns>
        List<object> GetArgumentValues(MethodCallExpression methodCall)
        {
            var argumentValues = new List<object>();

            foreach (var argument in methodCall.Arguments)
            {
                var lambda = Expression.Lambda(argument);
                var func = lambda.Compile();
                argumentValues.Add(func.DynamicInvoke());
            }

            return argumentValues;
        }
    }
}