using Newtonsoft.Json.Linq;
using Postgrest.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static Postgrest.Constants;

namespace Postgrest.Linq
{
	/// <summary>
	/// Helper class for parsing Set linq queries.
	/// </summary>
	internal class SetExpressionVisitor : ExpressionVisitor
	{
		/// <summary>
		/// The column that have been selected from this linq expression.
		/// </summary>
		public string? Column { get; private set; }

		/// <summary>
		/// The Column's type that value should be checked against.
		/// </summary>
		public Type? ExpectedType { get; private set; }

		/// <summary>
		/// Value to be updated.
		/// </summary>
		public object? Value { get; private set; }

		/// <summary>
		/// A Unary Node, delved into to represent a property on a BaseModel.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected override Expression VisitUnary(UnaryExpression node)
		{
			if (node.Operand is MemberExpression memberExpression)
			{
				var column = GetColumnFromMemberExpression(memberExpression);

				if (column != null)
				{
					Column = column;
					ExpectedType = memberExpression.Type;
				}
			}

			return node;
		}

		/// <summary>
		/// A Member Node, representing a property on a BaseModel.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected override Expression VisitMember(MemberExpression node)
		{
			var column = GetColumnFromMemberExpression(node);

			if (column != null)
			{
				Column = column;
				ExpectedType = node.Type;
			}

			return node;
		}

		/// <summary>
		/// Called when visiting a the expected new KeyValuePair().
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		protected override Expression VisitNew(NewExpression node)
		{
			if (typeof(KeyValuePair<object, object>).IsAssignableFrom(node.Type))
			{
				HandleKeyValuePair(node);
			}

			return node;
		}

		private void HandleKeyValuePair(NewExpression node)
		{
			if (node.Arguments.Count != 2)
				throw new ArgumentException("Unknown expression, should be a `KeyValuePair<object, object>`");

			var left = node.Arguments[0];
			var right = node.Arguments[1];

			if (left is NewExpression)
			{
				Visit(left);
			}
			else if (left is MemberExpression member)
			{
				Column = GetColumnFromMemberExpression(member);
				ExpectedType = member.Type;
			}
			else if (left is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression unaryMemberExpression)
			{
				Column = GetColumnFromMemberExpression(unaryMemberExpression);
				ExpectedType = unaryMemberExpression.Type;
			}
			else
			{
				throw new ArgumentException("Key should reference a Model Property.");
			}

			var valueArgument = Expression.Lambda(right).Compile().DynamicInvoke();
			Value = valueArgument;

			if (!ExpectedType!.IsAssignableFrom(Value.GetType()))
				throw new ArgumentException(string.Format("Expected Value to be of Type: {0}, instead received: {1}.", ExpectedType.Name, Value.GetType().Name));
		}

		/// <summary>
		/// Gets a column name from property based on it's supplied attributes.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private string GetColumnFromMemberExpression(MemberExpression node)
		{
			var type = node.Member.ReflectedType;
			var prop = type.GetProperty(node.Member.Name);
			var attrs = prop.GetCustomAttributes(true);

			foreach (var attr in attrs)
			{
				if (attr is ColumnAttribute columnAttr)
					return columnAttr.ColumnName;
				else if (attr is PrimaryKeyAttribute primaryKeyAttr)
					return primaryKeyAttr.ColumnName;
			}

			throw new ArgumentException(string.Format("Unknown argument '{0}' provided, does it have a Column or PrimaryKey attribute?", node.Member.Name));
		}
	}
}
