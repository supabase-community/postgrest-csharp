using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Postgrest.Converters
{
    public class IntArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is List<int> list)
            {
                writer.WriteValue($"{{{string.Join(",", list)}}}");
            }
        }
    }
}
