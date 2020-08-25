using System;
using System.Reflection;

namespace Postgrest.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MapToAttribute : Attribute
    {
        public string Mapping { get; set; }
        public string Formatter { get; set; }

        public MapToAttribute(string mapping, string formatter = null)
        {
            Mapping = mapping;
            Formatter = formatter;
        }
    }
}
