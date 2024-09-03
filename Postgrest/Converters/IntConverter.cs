using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Supabase.Postgrest.Converters
{

	/// <inheritdoc />
	public class IntArrayConverter : JsonConverter<List<int>>
	{
		/// <inheritdoc />
		public override bool CanConvert(Type typeToConvert)
		{
			return typeToConvert == typeof(List<int>);
		}

		/// <inheritdoc />
		public override List<int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
			{
				throw new JsonException("Expected start of array.");
			}

			var list = new List<int>();

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndArray)
				{
					return list;
				}

				if (reader.TokenType == JsonTokenType.Number)
				{
					list.Add(reader.GetInt32());
				}
				else
				{
					throw new JsonException($"Unexpected token type: {reader.TokenType}");
				}
			}

			throw new JsonException("Unexpected end of JSON.");
		}

		/// <inheritdoc />
		public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
		{
			writer.WriteStringValue($"{{{string.Join(",", value)}}}");
		}
	}
}
