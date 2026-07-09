using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using PostgrestTests.Models;
using static Supabase.Postgrest.Constants;

namespace PostgrestTests
{
    [TestClass]
    public class LinqWhereTests
    {
        private const string BaseUrl = "http://localhost:54321/rest/v1";
        private Client client = new Client(BaseUrl);

        [TestMethod("Linq: Where")]
        public async Task TestLinqWhere()
        {
            // Test boolean equality
            var query1 = await client.Table<Movie>()
                .Where(x => x.Id == "ea07bd86-a507-4c68-9545-b848bfe74c90")
                .Get();

            Assert.IsTrue(query1.Models.Count == 1);

            // Test string.contains
            var query2 = await client.Table<Movie>()
                .Where(x => x.Name!.Contains("Gun"))
                .Get();

            Assert.IsTrue(query2.Models.Count == 2);

            // Test multiple conditions
            var query3 = await client.Table<Movie>()
                .Where(x => x.Name!.Contains("Gun") && x.CreatedAt <= new DateTimeOffset(new DateTime(2022, 8, 23)))
                .Get();

            Assert.IsTrue(query3.Models.Count == 1);

            // Test null value checking
            var query4 = await client.Table<KitchenSink>()
                .Where(x => x.StringValue != null)
                .Get();

            foreach (var q in query4.Models)
                Assert.IsNotNull(q.StringValue);

            // Test Collection Contains
            var query5 = await client.Table<KitchenSink>()
                .Where(x => x.ListOfStrings!.Contains("set"))
                .Get();

            foreach (var q in query5.Models)
                Assert.IsTrue(q.ListOfStrings!.Contains("set"));

            var query6 = await client.Table<KitchenSink>()
                .Where(x => x.ListOfFloats!.Contains(10))
                .Get();

            foreach (var q in query6.Models)
                Assert.IsTrue(q.ListOfFloats!.Contains(10));

            var query7 = await client.Table<KitchenSink>()
                .Filter<string>(x => x.DateTimeValue!, Operator.NotEqual, null)
                .Get();

            foreach (var q in query7.Models)
                Assert.IsNotNull(q.DateTimeValue);

            //Testing where condition with Enum as constant
            var query8 = await client.Table<Movie>()
                .Where(x => x.Status == MovieStatus.OnDisplay)
                .Get();
            foreach (var q in query8.Models)
                Assert.IsTrue(q.Status == MovieStatus.OnDisplay);

            //Test where condition with Enum as Memeber expression
            var testMovie = new Movie { Status = MovieStatus.OnDisplay };
            var query9 = await client.Table<Movie>()
                .Where(x => x.Status == testMovie.Status)
                .Get();
            foreach (var q in query9.Models)
                Assert.IsTrue(q.Status == MovieStatus.OnDisplay);

            await client.Table<KitchenSink>()
                .Where(x => x.DateTimeValue == DateTime.Now)
                .Get();

            await client.Table<KitchenSink>()
                .Where(x => x.DateTimeValue == null)
                .Get();

            await client.Table<KitchenSink>()
                .Where(x => x.DateTimeValue == null)
                .Set(x => x.BooleanValue!, true)
                .Update();

            await client.Table<KitchenSink>()
                .Where(x => x.DateTimeValue == null)
                .Set(x => x.BooleanValue, true)
                .Update();

            await client.Table<KitchenSink>()
                .Where(x => x.DateTimeValue == null)
                .Set(x => x.StringValue!, null)
                .Update();
        }

        [TestMethod("Linq: Where with a null-check on an absent delegate applies no filter (supabase-csharp#192)")]
        public void GivenNullDelegate_ShouldApplyNoFilter()
        {
            var requestModel = new UserRequestModel();
            var table = client.Table<User>().Where(x => requestModel.FilterPredicate == null || requestModel.FilterPredicate(x));
            Assert.AreEqual($"{BaseUrl}/users", table.GenerateUrl());
        }

        [TestMethod("Linq: Where with a null-check on a present delegate throws a descriptive exception (supabase-csharp#192)")]
        public void GivenNonNullDelegate_ShouldThrowArgumentException()
        {
            var requestModel = new UserRequestModel { FilterPredicate = u => u.Username == "supabot" };
            var exception = Assert.ThrowsException<ArgumentException>(() => client.Table<User>().Where(x => requestModel.FilterPredicate == null || requestModel.FilterPredicate(x)));
            StringAssert.Contains(exception.Message, "Unable to translate expression");
        }

        [TestMethod("Linq: Where with an always-false predicate throws a descriptive exception (supabase-csharp#192)")]
        public void GivenAlwaysFalsePredicate_ShouldThrowArgumentException()
        {
            var requestModel = new UserRequestModel();
            var exception = Assert.ThrowsException<ArgumentException>(() => client.Table<User>().Where(x => requestModel.FilterPredicate != null && requestModel.FilterPredicate(x)));
            StringAssert.Contains(exception.Message, "always evaluates to false");
        }

        [TestMethod("Linq: Where translates a nested null-check to `is.null` (supabase-csharp#192)")]
        public void GivenNullCheckInsideOrPredicate_WhereTranslateToIsNullFilter()
        {
            var table = client.Table<User>().Where(x => x.Catchphrase == null || x.Catchphrase == "fat cat");
            var urlEncodedIsNullFilter = "or=(catchphrase.is.null%2ccatchphrase.eq.fat+cat)";
            Assert.AreEqual($"{BaseUrl}/users?{urlEncodedIsNullFilter}", table.GenerateUrl());
        }

        [TestMethod("Linq: Where negates an equality predicate into a `not.eq` filter")]
        public void GivenNegatedEqualityPredicate_ShouldGenerateNotEqFilter()
        {
            var table = client.Table<User>().Where(x => !(x.Username == "supabot"));
            Assert.AreEqual($"{BaseUrl}/users?username=not.eq.supabot", table.GenerateUrl());
        }

        [TestMethod("Linq: Where negates a null-check into a `not.is.null` filter")]
        public void GivenNegatedNullCheckPredicate_ShouldGenerateNotIsNullFilter()
        {
            var table = client.Table<User>().Where(x => !(x.Catchphrase == null));
            Assert.AreEqual($"{BaseUrl}/users?catchphrase=not.is.null", table.GenerateUrl());
        }

        [TestMethod("Linq: Where negates a grouped predicate into a `not.`-wrapped logical filter")]
        public void GivenNegatedGroupedPredicate_ShouldGenerateNotWrappedLogicalFilter()
        {
            var table = client.Table<User>().Where(x => !(x.Catchphrase == "fat cat" || x.Username == "supabot"));
            var urlEncodedNotOrFilter = "not.or=(catchphrase.eq.fat+cat%2cusername.eq.supabot)";
            Assert.AreEqual($"{BaseUrl}/users?{urlEncodedNotOrFilter}", table.GenerateUrl());
        }

        [TestMethod("Linq: Where negates a string `Contains` into a `not.like` filter")]
        public void GivenNegatedStringContainsPredicate_ShouldGenerateNotLikeFilter()
        {
            var table = client.Table<User>().Where(x => !x.Username!.Contains("supa"));
            Assert.AreEqual($"{BaseUrl}/users?username=not.like.*supa*", table.GenerateUrl());
        }

        [TestMethod("Linq: Where translates a captured list `Contains(column)` into an `in` filter")]
        public void GivenCapturedListContainsColumn_ShouldGenerateInFilter()
        {
            var values = new List<string> { "a", "b" };
            var table = client.Table<KitchenSink>().Where(x => values.Contains(x.StringValue!));
            Assert.AreEqual($"{BaseUrl}/kitchen_sink?string_value=in.(\"a\"%2c\"b\")", table.GenerateUrl());
        }

        [TestMethod("Linq: Where translates a captured array `Contains(column)` into an `in` filter")]
        public void GivenCapturedArrayContainsColumn_ShouldGenerateInFilter()
        {
            var values = new[] { 1, 2 };
            var table = client.Table<KitchenSink>().Where(x => values.Contains(x.IntValue!.Value));
            Assert.AreEqual($"{BaseUrl}/kitchen_sink?int_value=in.(\"1\"%2c\"2\")", table.GenerateUrl());
        }

        [TestMethod("Linq: Where still translates a column list `Contains(constant)` into a `cs` filter")]
        public void GivenColumnListContainsConstant_ShouldStillGenerateContainsFilter()
        {
            var table = client.Table<KitchenSink>().Where(x => x.ListOfStrings!.Contains("set"));
            Assert.AreEqual($"{BaseUrl}/kitchen_sink?list_of_strings=cs.{{set}}", table.GenerateUrl());
        }

        [TestMethod("Linq: Where still translates a column string `Contains(constant)` into a `like` filter")]
        public void GivenColumnStringContainsConstant_ShouldStillGenerateLikeFilter()
        {
            var table = client.Table<KitchenSink>().Where(x => x.StringValue!.Contains("foo"));
            Assert.AreEqual($"{BaseUrl}/kitchen_sink?string_value=like.*foo*", table.GenerateUrl());
        }

        [TestMethod("Linq: Where translates a bare boolean member into an `eq` filter")]
        public void GivenBareBooleanMemberPredicate_ShouldGenerateEqTrueFilter()
        {
            var table = client.Table<KitchenSink>().Where(x => x.BooleanValue);
            Assert.AreEqual($"{BaseUrl}/kitchen_sink?bool_value=eq.True", table.GenerateUrl());
        }

        [TestMethod("Linq: Where translates a negated boolean member into a `not.eq` filter")]
        public void GivenNegatedBooleanMemberPredicate_ShouldGenerateNotEqTrueFilter()
        {
            var table = client.Table<KitchenSink>().Where(x => !x.BooleanValue);
            Assert.AreEqual($"{BaseUrl}/kitchen_sink?bool_value=not.eq.True", table.GenerateUrl());
        }

        [TestMethod("Linq: Where translates a boolean member inside an AND into a nested filter")]
        public void GivenBooleanMemberInsideAndPredicate_ShouldGenerateNestedFilter()
        {
            var table = client.Table<KitchenSink>().Where(x => x.BooleanValue && x.IntValue > 3);
            Assert.AreEqual($"{BaseUrl}/kitchen_sink?and=(bool_value.eq.True%2cint_value.gt.3)", table.GenerateUrl());
        }

        private class UserRequestModel
        {
            public Func<User, bool>? FilterPredicate { get; set; }
        }
    }
}
