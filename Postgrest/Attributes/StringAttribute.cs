using System;
using System.Runtime.CompilerServices;

namespace Postgrest.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class StringAttribute : Attribute
    {
        public Type CoerceInto { get => typeof(string); }
        public string PropertyName { get; set; }

        public StringAttribute([CallerMemberName] string propertyName = null)
        {
            PropertyName = propertyName;
        }
    }
}
