using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Postgrest.Converters;

namespace Postgrest.Attributes
{
    /// <summary>
    /// A custom resolver that handles mapping column names and property names as well
    /// as handling the conversion of Postgrest Ranges to a C# `Range`.
    /// </summary>
    public class PostgrestContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            // Handle non-primitive conversions from a Postgres type to C#
            if (prop.PropertyType == typeof(IntRange))
            {
                prop.Converter = new RangeConverter();
            }
            else if (prop.PropertyType != null && (prop.PropertyType == typeof(DateTime) ||
                                                   Nullable.GetUnderlyingType(prop.PropertyType) == typeof(DateTime)))
            {
                prop.Converter = new DateTimeConverter();
            }
            else if (prop.PropertyType == typeof(List<int>))
            {
                prop.Converter = new IntArrayConverter();
            }
            else if (prop.PropertyType != null && (prop.PropertyType == typeof(List<DateTime>) ||
                                                   Nullable.GetUnderlyingType(prop.PropertyType) ==
                                                   typeof(List<DateTime>)))
            {
                prop.Converter = new DateTimeConverter();
            }

            // Dynamically set the name of the key we are serializing/deserializing from the model.
            if (!member.CustomAttributes.Any())
            {
                return prop;
            }

            var columnAttribute = member.GetCustomAttribute<ColumnAttribute>();

            if (columnAttribute != null)
            {
                prop.PropertyName = columnAttribute.ColumnName;
                prop.NullValueHandling = columnAttribute.NullValueHandling;
                return prop;
            }

            var primaryKeyAttribute = member.GetCustomAttribute<PrimaryKeyAttribute>();

            if (primaryKeyAttribute == null)
            {
                return prop;
            }

            prop.PropertyName = primaryKeyAttribute.ColumnName;
            prop.ShouldSerialize = instance => primaryKeyAttribute.ShouldInsert;
            return prop;
        }
    }
}