using System;
using System.Runtime.CompilerServices;

namespace Postgrest.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class BooleanAttribute : Attribute
    {
        public Type CoerceInto { get => typeof(bool); }
        public string PropertyName { get; set; }

        public BooleanAttribute([CallerMemberName] string propertyName = null)
        {
            PropertyName = propertyName;
        }
    }
}
