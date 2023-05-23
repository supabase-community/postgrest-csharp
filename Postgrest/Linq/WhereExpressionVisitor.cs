using Postgrest.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static Postgrest.Constants;
// ReSharper disable InvalidXmlDocComment

namespace Postgrest.Linq;

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
	/// 
	/// Invoked like: 
	///		`Table<Movies>().Where(x => x.Name == "Top Gun").Get();`
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	protected override Expression VisitBinary(BinaryExpression node)
	{
		Operator op = GetMappedOperator(node);

		// In the event this is a nested expression (n.Name == "Example" || n.Id = 3)
		switch (node.NodeType)
		{
			case ExpressionType.And:
			case ExpressionType.Or:
			case ExpressionType.AndAlso:
			case ExpressionType.OrElse:
				var leftVisitor = new WhereExpressionVisitor();
				leftVisitor.Visit(node.Left);

				var rightVisitor = new WhereExpressionVisitor();
				rightVisitor.Visit(node.Right);

				Filter = new QueryFilter(op, new List<QueryFilter>() { leftVisitor.Filter!, rightVisitor.Filter! });

				return node;
		}

		// Otherwise, the base case.

		var left = Visit(node.Left);
		var right = Visit(node.Right);

		var column = left is MemberExpression leftMember ? GetColumnFromMemberExpression(leftMember) : null;

		if (column == null)
			throw new ArgumentException(
				$"Left side of expression: '{node}' is expected to be property with a ColumnAttribute or PrimaryKeyAttribute");

		if (right is ConstantExpression rightConstant)
		{
			HandleConstantExpression(column, op, rightConstant);
		}
		else if (right is MemberExpression memberExpression)
		{
			HandleMemberExpression(column, op, memberExpression);
		}
		else if (right is NewExpression newExpression)
		{
			HandleNewExpression(column, op, newExpression);
		}
		else if (right is UnaryExpression unaryExpression)
		{
			HandleUnaryExpression(column, op, unaryExpression);
		}

		return node;
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
	/// A constant expression parser (i.e. x => x.Id == 5 <- where '5' is the constant)
	/// </summary>
	/// <param name="column"></param>
	/// <param name="op"></param>
	/// <param name="constantExpression"></param>
	private void HandleConstantExpression(string column, Operator op, ConstantExpression constantExpression)
	{
		Filter = new QueryFilter(column, op, constantExpression.Value);
	}

	/// <summary>
	/// A member expression parser (i.e. => x.Id == Example.Id <- where both `x.Id` and `Example.Id` are parsed as 'members')
	/// </summary>
	/// <param name="column"></param>
	/// <param name="op"></param>
	/// <param name="memberExpression"></param>
	private void HandleMemberExpression(string column, Operator op, MemberExpression memberExpression)
	{
		Filter = new QueryFilter(column, op, GetMemberExpressionValue(memberExpression));
	}

	/// <summary>
	/// A unary expression parser (i.e. => x.Id == 1 <- where both `1` is considered unary)
	/// </summary>
	/// <param name="column"></param>
	/// <param name="op"></param>
	/// <param name="memberExpression"></param>
	private void HandleUnaryExpression(string column, Operator op, UnaryExpression unaryExpression)
	{
		if (unaryExpression.Operand is ConstantExpression constantExpression)
		{
			HandleConstantExpression(column, op, constantExpression);
		}
		else if (unaryExpression.Operand is MemberExpression memberExpression)
		{
			HandleMemberExpression(column, op, memberExpression);
		}
		else if (unaryExpression.Operand is NewExpression newExpression)
		{
			HandleNewExpression(column, op, newExpression);
		}
	}

	/// <summary>
	/// An instantiated class parser (i.e. x => x.CreatedAt <= new DateTime(2022, 08, 20) <- where `new DateTime(...)` is an instantiated expression.
	/// </summary>
	/// <param name="column"></param>
	/// <param name="op"></param>
	/// <param name="newExpression"></param>
	private void HandleNewExpression(string column, Operator op, NewExpression newExpression)
	{
		var argumentValues = new List<object>();
		foreach (var argument in newExpression.Arguments)
		{
			var lambda = Expression.Lambda(argument);
			var func = lambda.Compile();
			argumentValues.Add(func.DynamicInvoke());
		}

		var constructor = newExpression.Constructor;
		var instance = constructor.Invoke(argumentValues.ToArray());

		if (instance is DateTime dateTime)
		{
			Filter = new QueryFilter(column, op, dateTime.ToUniversalTime());
		}
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
		var attrs = prop?.GetCustomAttributes(true);

		if (attrs == null) return node.Member.Name;
			
		foreach (var attr in attrs)
		{
			switch (attr)
			{
				case ColumnAttribute columnAttr:
					return columnAttr.ColumnName;
				case PrimaryKeyAttribute primaryKeyAttr:
					return primaryKeyAttr.ColumnName;
			}
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
	/// Creates map between linq <see cref="ExpressionType"/> and <see cref="Operator"/>
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	private Operator GetMappedOperator(Expression node)
	{
		var op = Operator.Equals;

		switch (node.NodeType)
		{
			case ExpressionType.Not:
				op = Operator.Not;
				break;
			case ExpressionType.And:
			case ExpressionType.AndAlso:
				op = Operator.And;
				break;
			case ExpressionType.OrElse:
			case ExpressionType.Or:
				op = Operator.Or;
				break;
			case ExpressionType.Equal:
				op = Operator.Equals;
				break;
			case ExpressionType.NotEqual:
				op = Operator.NotEqual;
				break;
			case ExpressionType.LessThan:
				op = Operator.LessThan;
				break;
			case ExpressionType.GreaterThan:
				op = Operator.GreaterThan;
				break;
			case ExpressionType.LessThanOrEqual:
				op = Operator.LessThanOrEqual;
				break;
			case ExpressionType.GreaterThanOrEqual:
				op = Operator.GreaterThanOrEqual;
				break;
		}

		return op;
	}

	/// <summary>
	/// Gets arguments from a method call expression, (i.e. x => x.Name.Contains("Top")) <- where `Top` is the argument on the called method `Contains`
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