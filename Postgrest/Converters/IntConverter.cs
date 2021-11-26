using System;
using System.Collections.Generic;
using System.Linq;
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
            List<int> list = value as List<int>;
            writer.WriteValue($"{{{String.Join(",", list)}}}");
        }
    }
}
