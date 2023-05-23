using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest;
using Postgrest.Converters;
using Postgrest.Exceptions;
using Postgrest.Extensions;

namespace PostgrestTests
{
    [TestClass]
    public class ConverterTests
    {
        [TestMethod("`intRange` should parse according to postgres docs")]
        public void TestIntRangeParsing()
        {
            // Test cases from 8.17.5 https://www.postgresql.org/docs/9.3/rangetypes.html
            var test1 = "[3,7)";
            var result1 = RangeConverter.ParseIntRange(test1);
            Assert.AreEqual(3, result1.Start);
            Assert.AreEqual(6, result1.End);

            var test2 = "(3,7)";
            var result2 = RangeConverter.ParseIntRange(test2);
            Assert.AreEqual(4, result2.Start);
            Assert.AreEqual(6, result2.End);

            var test3 = "[4,4]";
            var result3 = RangeConverter.ParseIntRange(test3);
            Assert.AreEqual(4, result3.Start);
            Assert.AreEqual(4, result3.End);

            var test4 = "[4,4)";
            var result4 = RangeConverter.ParseIntRange(test4);
            Assert.AreEqual(0, result4.Start);
            Assert.AreEqual(0, result4.End);

        }

        [TestMethod("`intrange` should only accept integers for parsing")]
        [ExpectedException(typeof(PostgrestException))]
        public void TestIntRangeParseInvalidFormat()
        {
            var test = "[1.2,3]";
            RangeConverter.ParseIntRange(test);
        }

        [TestMethod("`Range` should serialize into a string postgres understands")]
        public void TestRangeToPostgresString()
        {
            var test1 = new IntRange(1, 7).ToPostgresString();
            Assert.AreEqual("[1,7]", test1);

            var test2 = new IntRange(4, 6).ToPostgresString();
            Assert.AreEqual("[4,6]", test2);
        }
    }
}
