using System;
using System.Runtime.CompilerServices;

namespace Postgrest.Attributes
{
    /// <summary>
    /// Used to map a C# property to a Postgrest Column.
    /// </summary>
    /// <example>
    /// <code>
    /// class User : BaseModel {
    ///     [ColumnName("firstName")]
    ///     public string FirstName {get; set;}
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string ColumnName { get; set; }

        public ColumnAttribute([CallerMemberName] string columnName = null)
        {
            ColumnName = columnName;
        }
    }
}
