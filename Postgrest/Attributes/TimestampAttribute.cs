using System;
using System.Runtime.CompilerServices;

namespace Postgrest.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TimestampAttribute : Attribute
    {
        public Type CoerceInto { get => typeof(DateTime); }
        public string PropertyName { get; set; }

        public TimestampAttribute([CallerMemberName] string propertyName = null)
        {
            PropertyName = propertyName;
        }
    }
}
