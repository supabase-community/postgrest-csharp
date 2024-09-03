using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Extensions;
using Supabase.Postgrest.Exceptions;

namespace Supabase.Postgrest.Converters
{
	/// <summary>
	/// Used by System.Text.Json to convert a C# range into a Postgrest range.
	/// </summary>
	internal class RangeConverter : JsonConverter<IntRange>
	{
		public override bool CanConvert(Type typeToConvert)
		{
			return typeToConvert == typeof(IntRange);
		}

		public override IntRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				string? value = reader.GetString();
				return value != null ? ParseIntRange(value) : null;
			}
			throw new JsonException("Expected string value for IntRange.");
		}

		public override void Write(Utf8JsonWriter writer, IntRange value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToPostgresString());
		}

		public static IntRange ParseIntRange(string value)
		{
			//int4range (0,1] , [123,4123], etc. etc.
			const string pattern = @"^(\[|\()(\d+),(\d+)(\]|\))$";
			var matches = Regex.Matches(value, pattern);

			if (matches.Count <= 0)
				throw new PostgrestException("Unknown Range format.") { Reason = FailureHint.Reason.InvalidArgument };

			var groups = matches[0].Groups;
			var isInclusiveLower = groups[1].Value == "[";
			var isInclusiveUpper = groups[4].Value == "]";
			var value1 = int.Parse(groups[2].Value);
			var value2 = int.Parse(groups[3].Value);

			var start = isInclusiveLower ? value1 : value1 + 1;
			var count = isInclusiveUpper ? value2 : value2 - 1;

			// Edge-case, includes no points
			return count < start ? new IntRange(0, 0) : new IntRange(start, count);
		}
	}
}
