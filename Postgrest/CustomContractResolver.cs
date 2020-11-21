using System;
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
    public class CustomContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            // Handle non-primitive conversions from a Postgres type to C#
            if (prop.PropertyType == typeof(Range))
            {
                prop.Converter = new RangeConverter();
            }

            // Dynamically set the name of the key we are serializing/deserializing from the model.
            if (member.CustomAttributes.Count() > 0)
            {
                ColumnAttribute columnAtt = member.GetCustomAttribute<ColumnAttribute>();

                if (columnAtt != null)
                {
                    prop.PropertyName = columnAtt.ColumnName;
                    return prop;
                }

                PrimaryKeyAttribute primaryKeyAtt = member.GetCustomAttribute<PrimaryKeyAttribute>();

                if (primaryKeyAtt != null)
                {
                    prop.PropertyName = primaryKeyAtt.ColumnName;
                    return prop;
                }
            }

            return prop;
        }
    }
}
