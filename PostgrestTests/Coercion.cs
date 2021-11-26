using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest;
using PostgrestTests.Models;

namespace PostgrestTests
{
    [TestClass]
    public class Coercion
    {
        private static string baseUrl = "http://localhost:3000";

        [TestMethod]
        public async Task CanCoerceData()
        {
            var options = new StatelessClientOptions(baseUrl);

            var stringValue = "test";
            var intValue = 1;
            var floatValue = 1.1f;
            var doubleValue = 1.1d;
            var dateTimeValue = new DateTime(2021, 12, 12);
            var listOfStrings = new List<string> { "test", "1", "2", "3" };
            var listOfDateTime = new List<DateTime> { new DateTime(2021, 12, 10), new DateTime(2021, 12, 11), new DateTime(2021, 12, 12) };
            var listOfInts = new List<int> { 1, 2, 3 };
            var listOfFloats = new List<float> { 1.1f, 1.2f, 1.3f };
            var intRange = new IntRange(0, 1);


            var model = new KitchenSink
            {
                StringValue = stringValue,
                IntValue = intValue,
                FloatValue = floatValue,
                DoubleValue = doubleValue,
                DateTimeValue = dateTimeValue,
                ListOfStrings = listOfStrings,
                ListOfDateTimes = listOfDateTime,
                ListOfInts = listOfInts,
                ListOfFloats = listOfFloats,
                IntRange = intRange
            };


            var insertedModel = await StatelessClient.Table<KitchenSink>(options).Insert(model);
            var actual = insertedModel.Models.First();

            Assert.AreEqual(model.StringValue, actual.StringValue);
            Assert.AreEqual(model.IntValue, actual.IntValue);
            Assert.AreEqual(model.FloatValue, actual.FloatValue);
            Assert.AreEqual(model.DoubleValue, actual.DoubleValue);
            Assert.AreEqual(model.DateTimeValue, actual.DateTimeValue);
            CollectionAssert.AreEquivalent(model.ListOfStrings, actual.ListOfStrings);
            CollectionAssert.AreEquivalent(model.ListOfDateTimes, actual.ListOfDateTimes);
            CollectionAssert.AreEquivalent(model.ListOfInts, actual.ListOfInts);
            CollectionAssert.AreEquivalent(model.ListOfFloats, actual.ListOfFloats);
            Assert.AreEqual(model.IntRange.Start, actual.IntRange.Start);
            Assert.AreEqual(model.IntRange.End, actual.IntRange.End);
            
        }
    }
}
