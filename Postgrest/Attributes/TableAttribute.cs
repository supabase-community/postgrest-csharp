using System;
namespace Postgrest.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public string Name { get; set; }

        public TableAttribute(string tableName)
        {
            Name = tableName;
        }
    }
}
