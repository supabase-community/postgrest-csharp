using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Postgrest;
using PostgrestTests.Models;

namespace PostgrestTests
{
    [TestClass]
    public class DateTimeReadTests
    {
        private const string WireValue = "2024-06-26T18:30:45.1234560+00:00";

        private static readonly DateTime ExpectedInstant =
            DateTimeOffset.Parse(WireValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).UtcDateTime;

        private static KitchenSink Deserialize(string json) =>
            JsonConvert.DeserializeObject<KitchenSink>(json, Client.SerializerSettings())!;

        [TestMethod("DateTime read: preserves the instant and sub-second precision of a timestamptz")]
        public void GivenTimestamptzWithSubSeconds_ShouldPreserveInstantAndPrecisionOnRead()
        {
            var model = Deserialize($"{{\"datetime_value\":\"{WireValue}\"}}");
            Assert.IsTrue(model.DateTimeValue.HasValue);
            Assert.AreEqual(ExpectedInstant, model.DateTimeValue!.Value.ToUniversalTime());
        }

        [TestMethod("DateTime read: does not strip the kind of a timestamptz to Unspecified")]
        public void GivenTimestamptz_ShouldNotReturnUnspecifiedKindOnRead()
        {
            var model = Deserialize($"{{\"datetime_value\":\"{WireValue}\"}}");
            Assert.AreNotEqual(DateTimeKind.Unspecified, model.DateTimeValue!.Value.Kind);
        }

        [TestMethod("DateTime read: preserves each instant of a timestamptz array")]
        public void GivenTimestamptzList_ShouldPreserveEachInstantOnRead()
        {
            var model = Deserialize($"{{\"list_of_datetimes\":[\"{WireValue}\"]}}");
            Assert.IsNotNull(model.ListOfDateTimes);
            Assert.AreEqual(1, model.ListOfDateTimes!.Count);
            Assert.AreEqual(ExpectedInstant, model.ListOfDateTimes[0].ToUniversalTime());
        }
    }
}
