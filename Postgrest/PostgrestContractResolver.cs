using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;

namespace Supabase.Postgrest
{
    /// <summary>
    /// A custom resolver that handles mapping column names and property names as well
    /// as handling the conversion of Postgrest Ranges to a C# `Range`.
    /// </summary>
    public class PostgrestContractResolver : JsonConverter<object>
    {
        private bool IsUpdate { get; set; }
        private bool IsInsert { get; set; }
        private bool IsUpsert { get; set; }

        /// <summary>
        /// Sets the state of the contract resolver to either insert, update, or upsert.
        /// </summary>
        /// <param name="isInsert"></param>
        /// <param name="isUpdate"></param>
        /// <param name="isUpsert"></param>
        public void SetState(bool isInsert = false, bool isUpdate = false, bool isUpsert = false)
        {
            IsUpdate = isUpdate;
            IsInsert = isInsert;
            IsUpsert = isUpsert;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return true; // This converter can handle any type
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("JSON object expected.");
            }

            var instance = Activator.CreateInstance(typeToConvert);
            var properties = typeToConvert.GetProperties();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return instance;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Property name expected.");
                }

                string propertyName = reader.GetString()!;
                reader.Read();

                var property = FindProperty(properties, propertyName);
                if (property != null)
                {
                    object? value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                    property.SetValue(instance, value);
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("JSON object not properly closed.");
        }

        private PropertyInfo? FindProperty(PropertyInfo[] properties, string jsonPropertyName)
        {
            foreach (var prop in properties)
            {
                var columnAttribute = prop.GetCustomAttribute<ColumnAttribute>();
                var referenceAttr = prop.GetCustomAttribute<ReferenceAttribute>();
                var primaryKeyAttribute = prop.GetCustomAttribute<PrimaryKeyAttribute>();

                if (columnAttribute != null && columnAttribute.ColumnName == jsonPropertyName)
                {
                    return prop;
                }
                else if (referenceAttr != null)
                {
                    string? refPropertyName = string.IsNullOrEmpty(referenceAttr.ForeignKey)
                        ? referenceAttr.TableName
                        : referenceAttr.ColumnName;
                    if (refPropertyName == jsonPropertyName)
                    {
                        return prop;
                    }
                }
                else if (primaryKeyAttribute != null && primaryKeyAttribute.ColumnName == jsonPropertyName)
                {
                    return prop;
                }
                else if (prop.Name == jsonPropertyName)
                {
                    return prop;
                }
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            var properties = value.GetType().GetProperties();
            var customOptions = new JsonSerializerOptions(options);
            customOptions.Converters.Remove(this);

            foreach (var prop in properties)
            {
                var columnAttribute = prop.GetCustomAttribute<ColumnAttribute>();
                var referenceAttr = prop.GetCustomAttribute<ReferenceAttribute>();
                var primaryKeyAttribute = prop.GetCustomAttribute<PrimaryKeyAttribute>();
                var ignoreAttribute = prop.GetCustomAttribute<JsonIgnoreAttribute>();

                string? propertyName = prop.Name;
                bool shouldSerialize = ignoreAttribute == null;

                if (columnAttribute != null)
                {
                    propertyName = columnAttribute.ColumnName;
                    if ((IsInsert && columnAttribute.IgnoreOnInsert) ||
                        (IsUpdate && columnAttribute.IgnoreOnUpdate) ||
                        (IsUpsert && (columnAttribute.IgnoreOnUpdate || columnAttribute.IgnoreOnInsert)))
                    {
                        shouldSerialize = false;
                    }
                }
                else if (referenceAttr != null)
                {
                    propertyName = string.IsNullOrEmpty(referenceAttr.ForeignKey)
                        ? referenceAttr.TableName
                        : referenceAttr.ColumnName;
                    if (IsInsert || IsUpdate)
                    {
                        shouldSerialize = false;
                    }
                }
                else if (primaryKeyAttribute != null)
                {
                    propertyName = primaryKeyAttribute.ColumnName;
                    shouldSerialize = primaryKeyAttribute.ShouldInsert || (IsUpsert && value != null);
                }

                if (shouldSerialize)
                {
                    object? propValue = null;
                    try
                    {
                        propValue = prop.GetValue(value);
                    }
                    catch (TargetParameterCountException)
                    {
                        // Skip properties that require parameters
                        continue;
                    }
                    catch (Exception ex)
                    {
                        // Log or handle other exceptions as needed
                        Console.WriteLine($"Error getting value for property {prop.Name}: {ex.Message}");
                        continue;
                    }

                    if (propValue != null && propertyName != null)
                    {
                        // Check if the property is not the GetPrimaryKey method
                        if (prop.Name != "GetPrimaryKey" && !prop.Name.EndsWith("PrimaryKeyInternal"))
                        {
                            writer.WritePropertyName(propertyName);
                            JsonSerializer.Serialize(writer, propValue, prop.PropertyType, customOptions);
                        }
                    }
                }
            }

            writer.WriteEndObject();
        }
    }
}
