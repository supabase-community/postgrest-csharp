using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Postgrest.Converters
{
    public class DateTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value != null)
            {
                var str = reader.Value.ToString();
                var date =  DateTime.Parse(str);
                return date;
            }
            else
            {
                List<DateTime> result = new List<DateTime>();
                JArray jo = JArray.Load(reader);

                foreach (var item in jo.ToArray())
                {
                    var date = DateTime.Parse(item.ToString());
                    result.Add(date);
                }

                return result;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
