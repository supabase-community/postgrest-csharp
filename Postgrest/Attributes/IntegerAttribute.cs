using System;
using System.Runtime.CompilerServices;

namespace Postgrest.Attributes
{
    public class IntegerAttribute
    {
        public Type CoerceInto { get => typeof(int); }
        public string PropertyName { get; set; }

        public IntegerAttribute([CallerMemberName] string propertyName = null)
        {
            PropertyName = propertyName;
        }
    }
}
