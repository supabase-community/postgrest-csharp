using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest;
using PostgrestTests.Models;
using static Postgrest.Constants;

namespace PostgrestTests
{
    [TestClass]
    public class LinqTests
    {
        private static string baseUrl = "http://localhost:3000";

		[TestMethod("Linq: Select")]
		public async Task TestLinqSelect()
		{
			var client = new Client(baseUrl);

			var query1 = await client.Table<Movie>()
				.Select(x => new object[] { x.Id })
				.Get();

			var first1 = query1.Models.First();

			Assert.IsNull(first1.Name);

			var query2 = await client.Table<Movie>()
				.Select(x => new object[] { x.Id, x.Name })
				.Get();

			var first2 = query2.Models.First();

			Assert.IsNotNull(first2.Id);
			Assert.IsNotNull(first2.Name);

			Assert.ThrowsException<ArgumentException>(() =>
			{
				return client.Table<KitchenSink>().Select(x => new object[] { "stringValue" });
			});
		}

		[TestMethod("Linq: Where")]
		public async Task TestLinqWhere()
		{
			var client = new Client(baseUrl);

			var testValue = 2;
			// Test boolean equality
			var query1 = await client.Table<Movie>()
				.Where(x => x.Id == testValue)
				.Get();

			Assert.IsTrue(query1.Models.Count == 1);

			// Test string.contains
			var query2 = await client.Table<Movie>()
				.Where(x => x.Name.Contains("Gun"))
				.Get();

			Assert.IsTrue(query2.Models.Count == 2);

			// Test multiple conditions
			var query3 = await client.Table<Movie>()
				.Where(x => x.Name.Contains("Gun") && x.CreatedAt <= new DateTime(2022, 08, 23))
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
				.Where(x => x.ListOfStrings.Contains("set"))
				.Get();

			foreach (var q in query5.Models)
				Assert.IsTrue(q.ListOfStrings.Contains("set"));

			var query6 = await client.Table<KitchenSink>()
				.Where(x => x.ListOfFloats.Contains(10))
				.Get();

			foreach (var q in query6.Models)
				Assert.IsTrue(q.ListOfFloats.Contains(10));
		}

		[TestMethod("Linq: OnConflict")]
		public async Task TestLinqOnConflict()
		{
			var client = new Client(baseUrl);

			var supaUpdated = new User
			{
				Username = "supabot",
				AgeRange = new IntRange(3, 8),
				Status = "OFFLINE",
				Catchphrase = "fat cat"
			};

			var response = await client.Table<User>().Insert(supaUpdated, new QueryOptions { Upsert = true });

			// Upserting a model.
			var kitchenSink1 = new KitchenSink { UniqueValue = "Testing" };

			var ks1 = await client.Table<KitchenSink>().OnConflict(x => x.UniqueValue).Upsert(kitchenSink1);
			var uks1 = ks1.Models.First();
			uks1.StringValue = "Testing 1";
			var ks3 = await client.Table<KitchenSink>().OnConflict(x => x.UniqueValue).Upsert(uks1);

			var updatedUser = response.Models.First();

			Assert.AreEqual(1, response.Models.Count);
			Assert.AreEqual(supaUpdated.Username, updatedUser.Username);
			Assert.AreEqual(supaUpdated.AgeRange, updatedUser.AgeRange);
			Assert.AreEqual(supaUpdated.Status, updatedUser.Status);

			Assert.ThrowsException<ArgumentException>(() =>
			{
				return client.Table<KitchenSink>().OnConflict(x => new object[] { x.StringValue, x.IntValue }).Upsert(kitchenSink1);
			});

			Assert.ThrowsException<ArgumentException>(() =>
			{
				return client.Table<KitchenSink>().OnConflict(x => "something").Upsert(kitchenSink1);
			});
		}


		[TestMethod("Linq: Order")]
		public async Task TestLinqOrderBy()
		{
			var client = new Client(baseUrl);

			var orderedResponse = await client.Table<User>().Order(x => x.Username, Ordering.Descending).Get();
			var unorderedResponse = await client.Table<User>().Get();

			var supaOrderedUsers = orderedResponse.Models;
			var linqOrderedUsers = unorderedResponse.Models.OrderByDescending(u => u.Username).ToList();

			CollectionAssert.AreEqual(linqOrderedUsers, supaOrderedUsers);

			Assert.ThrowsException<ArgumentException>(() =>
			{
				return client.Table<KitchenSink>().Order(x => new object[] { x.StringValue, x.IntValue }, Ordering.Descending).Get();
			});

			Assert.ThrowsException<ArgumentException>(() =>
			{
				return client.Table<KitchenSink>().Order(x => "something", Ordering.Descending).Get();
			});
		}

		[TestMethod("Linq: Columns")]
		public async Task TestLinqColumns()
		{
			var client = new Client(baseUrl);

			var movies = await client.Table<Movie>().Get();
			var first = movies.Models.First();
			var originalName = first.Name;
			var originalDate = first.CreatedAt;
			var newName = $"{first.Name} (Changed, {DateTime.Now})";

			first.Name = newName;
			first.CreatedAt = DateTime.UtcNow;

			var result = await client.Table<Movie>().Columns(x => new[] { x.Name }).Update(first);

			Assert.AreEqual(originalDate, result.Models.First().CreatedAt);
			Assert.AreNotEqual(originalName, result.Models.First().Name);

			Assert.ThrowsException<ArgumentException>(() =>
			{
				return client.Table<Movie>().Columns(x => new object[] { "something", DateTime.Now }).Update(first);
			});
		}

		[TestMethod("Linq: Update")]
		public async Task TestLinqUpdate()
		{
			var client = new Client(baseUrl);

			var newName = $"Top Gun (Updated By Linq) at {DateTime.Now}";
			var movie = await client.Table<Movie>()
				.Set(x => new KeyValuePair<object, object>(x.Name, newName))
				.Where(x => x.Name.Contains("Top Gun"))
				.Update();

			var exists = await client.Table<Movie>()
				.Where(x => x.Name == newName)
				.Single();

			var count = await client.Table<Movie>()
				.Where(x => x.Name == newName)
				.Count(CountType.Exact);

			Assert.IsNotNull(exists);
			Assert.IsTrue(count == 1);

			Assert.ThrowsException<ArgumentException>(() =>
			{
				return client.Table<Movie>().Set(x => new KeyValuePair<object, object>(x.Name, DateTime.Now)).Update();
			});

			Assert.ThrowsException<ArgumentException>(() =>
			{
				return client.Table<Movie>().Set(x => new KeyValuePair<object, object>(DateTime.Now, newName)).Update();
			});
		}

		[TestMethod("Linq: Delete")]
		public async Task TestLinqDelete()
		{
			var client = new Client(baseUrl);

			var newMovie = new Movie
			{
				Id = 18,
				Name = "Pride and Prejudice",
				CreatedAt = DateTime.Now
			};

			await client.Table<Movie>().Insert(newMovie);

			await client.Table<Movie>().Where(x => x.Name == newMovie.Name).Delete();

			var exists = await client.Table<Movie>().Where(x => x.Name == newMovie.Name).Single();

			Assert.IsNull(exists);
		}
	}
}
