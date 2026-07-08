using System;
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

        [TestMethod("Linq: Where")]
        public async Task TestLinqWhere()
        {
            var client = new Client(BaseUrl);

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
            var client = new Client(BaseUrl);

            var requestModel = new UserRequestModel();

            var table = client.Table<User>()
                .Where(x => requestModel.FilterPredicate == null || requestModel.FilterPredicate(x));

            Assert.AreEqual($"{BaseUrl}/users", table.GenerateUrl());
        }

        [TestMethod("Linq: Where with a null-check on a present delegate throws a descriptive exception (supabase-csharp#192)")]
        public void GivenNonNullDelegate_ShouldThrowArgumentException()
        {
            var client = new Client(BaseUrl);

            var requestModel = new UserRequestModel { FilterPredicate = u => u.Username == "supabot" };

            var exception = Assert.ThrowsException<ArgumentException>(() => client.Table<User>()
                .Where(x => requestModel.FilterPredicate == null || requestModel.FilterPredicate(x)));

            StringAssert.Contains(exception.Message, "Unable to translate expression");
        }

        [TestMethod("Linq: Where with an always-false predicate throws a descriptive exception (supabase-csharp#192)")]
        public void GivenAlwaysFalsePredicate_ShouldThrowArgumentException()
        {
            var client = new Client(BaseUrl);

            var requestModel = new UserRequestModel();

            var exception = Assert.ThrowsException<ArgumentException>(() => client.Table<User>()
                .Where(x => requestModel.FilterPredicate != null && requestModel.FilterPredicate(x)));

            StringAssert.Contains(exception.Message, "always evaluates to false");
        }

        [TestMethod("Linq: Where translates a nested null-check to `is.null` (supabase-csharp#192)")]
        public void GivenNullCheckInsideOrPredicate_WhereTranslateToIsNullFilter()
        {
            var client = new Client(BaseUrl);

            var table = client.Table<User>()
                .Where(x => x.Catchphrase == null || x.Catchphrase == "fat cat");

            var urlEncodedIsNullFilter = "or=(catchphrase.is.null%2ccatchphrase.eq.fat+cat)";
            Assert.AreEqual($"{BaseUrl}/users?{urlEncodedIsNullFilter}", table.GenerateUrl());
        }

        private class UserRequestModel
        {
            public Func<User, bool>? FilterPredicate { get; set; }
        }
    }
}
