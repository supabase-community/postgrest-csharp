using System;
using System.Runtime.CompilerServices;

namespace Postgrest.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FloatAttribute : Attribute
    {
        public Type CoerceInto { get => typeof(float); }
        public string PropertyName { get; set; }

        public FloatAttribute([CallerMemberName] string propertyName = null)
        {
            PropertyName = propertyName;
        }
    }
}
