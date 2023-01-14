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
	/// Helper class for parsing Select linq queries.
	/// </summary>
	internal class SelectExpressionVisitor : ExpressionVisitor
	{
		/// <summary>
		/// The columns that have been selected from this linq expression.
		/// </summary>
		public List<string> Columns { get; private set; } = new List<string>();

		/// <summary>
		/// The root call that will be looped through to populate <see cref="Columns"/>.
		/// 
		/// Called like: `Table<Movies>().Select(x => new[] { x.Id, x.Name, x.CreatedAt }).Get()`
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected override Expression VisitNewArray(NewArrayExpression node)
		{
			foreach (var expression in node.Expressions)
				Visit(expression);

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
				Columns.Add(column);

			return node;
		}

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
					Columns.Add(column);
			}

			return node;
		}

		/// <summary>
		/// Gets a column name from property based on it's supplied attributes.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private string? GetColumnFromMemberExpression(MemberExpression node)
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
