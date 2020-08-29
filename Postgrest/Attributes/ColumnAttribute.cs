using System;
using System.Runtime.CompilerServices;

namespace Postgrest.Attributes
{
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
