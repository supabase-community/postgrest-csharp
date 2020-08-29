using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Postgrest.Attributes
{
    public class AttributeContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

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
