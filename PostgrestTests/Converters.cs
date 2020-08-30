using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest.Extensions;

namespace PostgrestTests
{
    [TestClass]
    public class Converters
    {
        [TestMethod("`intRange` should parse according to postgres docs")]
        public void TestIntRangeParsing()
        {
            // Test cases from 8.17.5 https://www.postgresql.org/docs/9.3/rangetypes.html
            var test1 = "[3,7)";
            var result1 = Postgrest.Converters.RangeConverter.ParseIntRange(test1);
            Assert.AreEqual(result1.Start, 3);
            Assert.AreEqual(result1.End, 6);

            var test2 = "(3,7)";
            var result2 = Postgrest.Converters.RangeConverter.ParseIntRange(test2);
            Assert.AreEqual(result2.Start, 4);
            Assert.AreEqual(result2.End, 6);

            var test3 = "[4,4]";
            var result3 = Postgrest.Converters.RangeConverter.ParseIntRange(test3);
            Assert.AreEqual(result3.Start, 4);
            Assert.AreEqual(result3.End, 4);

            var test4 = "[4,4)";
            var result4 = Postgrest.Converters.RangeConverter.ParseIntRange(test4);
            Assert.AreEqual(result4.Start, 0);
            Assert.AreEqual(result4.End, 0);

        }

        [TestMethod("`intrange` should only accept integers for parsing")]
        [ExpectedException(typeof(Exception))]
        public void TestIntRangeParseInvalidFormat()
        {
            var test = "[1.2,3]";
            Postgrest.Converters.RangeConverter.ParseIntRange(test);
        }

        [TestMethod("`Range` should serialize into a string postgres understands")]
        [ExpectedException(typeof(Exception))]
        public void TestRangeToPostgresString()
        {
            var test1 = new Range(1, 7).ToPostgresString();
            Assert.AreEqual(test1, "[1,7]");

            var test2 = new Range(4, 6).ToPostgresString();
            Assert.AreEqual(test2, "[4,6]");

        }
    }
}
