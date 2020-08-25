using System;
using System.Runtime.CompilerServices;

namespace Postgrest.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
        public string PropertyName { get; set; }

        public PrimaryKeyAttribute([CallerMemberName] string propertyName = null)
        {
            PropertyName = propertyName;
        }
    }
}
