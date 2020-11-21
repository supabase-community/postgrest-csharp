using System;
using System.Reflection;

namespace Postgrest.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets a typed Attribute attached to an enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T GetAttribute<T>(this Enum value) where T : Attribute
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    var attr = Attribute.GetCustomAttribute(field, typeof(T)) as T;
                    if (attr != null)
                    {
                        return attr;
                    }
                }
            }
            return null;
        }
    }
}
