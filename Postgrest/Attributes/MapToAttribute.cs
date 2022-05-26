using System;

namespace Postgrest.Attributes
{
    /// <summary>
    /// Used internally to add a string value to a C# field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class MapToAttribute : Attribute
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
