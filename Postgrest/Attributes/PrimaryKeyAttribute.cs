using System;
using System.Runtime.CompilerServices;

namespace Postgrest.Attributes
{
    /// <summary>
    /// Used to map a C# property to a Postgrest PrimaryKey.
    /// </summary>
    /// <example>
    /// <code>
    /// class User : BaseModel {
    ///     [PrimaryKey("id")]
    ///     public string Id {get; set;}
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
        public string ColumnName { get; set; }

        /// <summary>
        /// Would be set to false in the event that the database handles the generation of this property.
        /// </summary>
        public bool ShouldInsert { get; set; }

        public PrimaryKeyAttribute([CallerMemberName] string columnName = null, bool shouldInsert = true)
        {
            ColumnName = columnName;
            ShouldInsert = shouldInsert;
        }
    }
}
