using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Postgrest;
using PostgrestTests.Models;

namespace PostgrestTests
{
    [TestClass]
    public class DateTimeWriteTests
    {
        private static string Serialize(KitchenSink model) =>
            JsonConvert.SerializeObject(model, Client.SerializerSettings());

        [TestMethod("DateTime write: serializes an unspecified-kind list without shifting to UTC")]
        public void GivenUnspecifiedDateTimeList_ShouldSerializeWithoutUtcConversion()
        {
            var model = new KitchenSink
            {
                ListOfDateTimes = [new DateTime(2021, 12, 10), new DateTime(2021, 12, 11), new DateTime(2021, 12, 12)]
            };
            StringAssert.Contains(Serialize(model),
                "\"list_of_datetimes\":[\"2021-12-10T00:00:00\",\"2021-12-11T00:00:00\",\"2021-12-12T00:00:00\"]");
        }

        [TestMethod("DateTime write: serializes an unspecified-kind value without shifting to UTC")]
        public void GivenUnspecifiedDateTime_ShouldSerializeWithoutUtcConversion()
        {
            var model = new KitchenSink { DateTimeValue = new DateTime(2026, 7, 8, 12, 0, 0) };
            StringAssert.Contains(Serialize(model), "\"datetime_value\":\"2026-07-08T12:00:00\"");
        }
    }
}
