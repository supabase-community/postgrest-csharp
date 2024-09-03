using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Supabase.Postgrest.Converters
{

	/// <inheritdoc />
	public class DateTimeConverter : JsonConverter<object>
	{
		/// <inheritdoc />
		public override bool CanConvert(Type typeToConvert)
		{
			return typeToConvert == typeof(DateTime) || typeToConvert == typeof(DateTime?) ||
					 typeToConvert == typeof(List<DateTime>) || typeToConvert == typeof(List<DateTime?>);
		}

		/// <inheritdoc />
		public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null)
			{
				return null;
			}

			if (reader.TokenType == JsonTokenType.String)
			{
				var str = reader.GetString();
				if (string.IsNullOrEmpty(str))
				{
					return null;
				}

				var infinity = ParseInfinity(str);
				return infinity ?? DateTime.Parse(str);
			}

			if (reader.TokenType == JsonTokenType.StartArray)
			{
				var result = new List<DateTime?>();

				while (reader.Read())
				{
					if (reader.TokenType == JsonTokenType.EndArray)
					{
						break;
					}

					if (reader.TokenType == JsonTokenType.Null)
					{
						result.Add(null);
					}
					else if (reader.TokenType == JsonTokenType.String)
					{
						var inner = reader.GetString();
						if (string.IsNullOrEmpty(inner))
						{
							result.Add(null);
						}
						else
						{
							var infinity = ParseInfinity(inner);
							result.Add(infinity ?? DateTime.Parse(inner));
						}
					}
				}

				return result;
			}

			return null;
		}

		private static DateTime? ParseInfinity(string input)
		{
			if (input.Contains("infinity"))
			{
				return input.Contains("-") ? DateTime.MinValue : DateTime.MaxValue;
			}

			return null;
		}

		/// <inheritdoc />
		public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
		{
			if (value is DateTime dateTime)
			{
				if (dateTime == DateTime.MinValue)
				{
					writer.WriteStringValue("-infinity");
				}
				else if (dateTime == DateTime.MaxValue)
				{
					writer.WriteStringValue("infinity");
				}
				else
				{
					writer.WriteStringValue(dateTime.ToString("O"));
				}
			}
			else if (value is List<DateTime> dateTimeList)
			{
				writer.WriteStartArray();
				foreach (var dt in dateTimeList)
				{
					if (dt == DateTime.MinValue)
					{
						writer.WriteStringValue("-infinity");
					}
					else if (dt == DateTime.MaxValue)
					{
						writer.WriteStringValue("infinity");
					}
					else
					{
						writer.WriteStringValue(dt.ToString("O"));
					}
				}
				writer.WriteEndArray();
			}
			else
			{
				throw new JsonException("Unsupported value type for DateTimeConverter.");
			}
		}
	}
}
