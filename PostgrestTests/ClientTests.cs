using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Responses;
using PostgrestTests.Models;
using static Supabase.Postgrest.Constants;

namespace PostgrestTests
{
    [TestClass]
    public class ClientTests
    {
        private const string BaseUrl = "http://localhost:3000";

        [TestMethod("Initializes")]
        public void TestInitialization()
        {
            var client = new Client(BaseUrl);
            Assert.AreEqual(BaseUrl, client.BaseUrl);
        }

        [TestMethod("with optional query params")]
        public void TestQueryParams()
        {
            var client = new Client(BaseUrl, options: new ClientOptions
            {
                QueryParams = new Dictionary<string, string>
                {
                    { "some-param", "foo" },
                    { "other-param", "bar" }
                }
            });

            Assert.AreEqual($"{BaseUrl}/users?some-param=foo&other-param=bar", client.Table<User>().GenerateUrl());
        }

        [TestMethod("will use TableAttribute")]
        public void TestTableAttribute()
        {
            var client = new Client(BaseUrl);
            Assert.AreEqual($"{BaseUrl}/users", client.Table<User>().GenerateUrl());
        }

        [TestMethod("will default to Class.name in absence of TableAttribute")]
        public void TestTableAttributeDefault()
        {
            var client = new Client(BaseUrl);
            Assert.AreEqual($"{BaseUrl}/Stub", client.Table<Stub>().GenerateUrl());
        }

        [TestMethod("will set header from options")]
        public void TestHeadersToken()
        {
            var headers = Supabase.Postgrest.Helpers.PrepareRequestHeaders(HttpMethod.Get,
                new Dictionary<string, string> { { "Authorization", "Bearer token" } });

            Assert.AreEqual("Bearer token", headers["Authorization"]);
        }

        [TestMethod("will set apikey as query string")]
        public void TestQueryApiKey()
        {
            var client = new Client(BaseUrl, new ClientOptions
            {
                Headers =
                {
                    { "apikey", "some-key" }
                }
            });
            Assert.AreEqual($"{BaseUrl}/users?apikey=some-key", client.Table<User>().GenerateUrl());
        }

        [TestMethod("filters: simple")]
        public void TestFiltersSimple()
        {
            var client = new Client(BaseUrl);
            var dict = new Dictionary<Operator, string>
            {
                { Operator.Equals, "eq.bar" },
                { Operator.GreaterThan, "gt.bar" },
                { Operator.GreaterThanOrEqual, "gte.bar" },
                { Operator.LessThan, "lt.bar" },
                { Operator.LessThanOrEqual, "lte.bar" },
                { Operator.NotEqual, "neq.bar" },
                { Operator.Is, "is.bar" },
            };

            foreach (var pair in dict)
            {
                var filter = new QueryFilter("foo", pair.Key, "bar");
                var result = ((Table<User>)client.Table<User>()).PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: like & ilike")]
        public void TestFiltersLike()
        {
            var client = new Client(BaseUrl);
            var dict = new Dictionary<Operator, string>
            {
                { Operator.Like, "like.*bar*" },
                { Operator.ILike, "ilike.*bar*" },
            };

            foreach (var pair in dict)
            {
                var filter = new QueryFilter("foo", pair.Key, "%bar%");
                var result = ((Table<User>)client.Table<User>()).PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        /// <summary>
        /// See: http://postgrest.org/en/v7.0.0/api.html#operators
        /// </summary>
        [TestMethod("filters: `In` with List<object> arguments")]
        public void TestFiltersArraysWithLists()
        {
            var client = new Client(BaseUrl);

            // UrlEncoded {"bar","buzz"}
            string exp = "(\"bar\",\"buzz\")";
            var dict = new Dictionary<Operator, string>
            {
                { Operator.In, $"in.{exp}" },
            };

            foreach (var pair in dict)
            {
                var list = new List<object> { "bar", "buzz" };
                var filter = new QueryFilter("foo", pair.Key, list);
                var result = ((Table<User>)client.Table<User>()).PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        /// <summary>
        /// See: http://postgrest.org/en/v7.0.0/api.html#operators
        /// </summary>
        [TestMethod("filters: `Contains`, `ContainedIn`, `Overlap` with List<object> arguments")]
        public void TestFiltersContainsArraysWithLists()
        {
            var client = new Client(BaseUrl);

            // UrlEncoded {bar,buzz} - according to documentation, does not accept quoted strings
            string exp = "{bar,buzz}";
            var dict = new Dictionary<Operator, string>
            {
                { Operator.Contains, $"cs.{exp}" },
                { Operator.ContainedIn, $"cd.{exp}" },
                { Operator.Overlap, $"ov.{exp}" },
            };

            foreach (var pair in dict)
            {
                var list = new List<object> { "bar", "buzz" };
                var filter = new QueryFilter("foo", pair.Key, list);
                var result = ((Table<User>)client.Table<User>()).PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: arrays with Dictionary<string,object> arguments")]
        public void TestFiltersArraysWithDictionaries()
        {
            var client = new Client(BaseUrl);

            string exp = "{\"bar\":100,\"buzz\":\"zap\"}";
            var dict = new Dictionary<Operator, string>
            {
                { Operator.In, $"in.{exp}" },
                { Operator.Contains, $"cs.{exp}" },
                { Operator.ContainedIn, $"cd.{exp}" },
                { Operator.Overlap, $"ov.{exp}" },
            };

            foreach (var pair in dict)
            {
                var value = new Dictionary<string, object> { { "bar", 100 }, { "buzz", "zap" } };
                var filter = new QueryFilter("foo", pair.Key, value);
                var result = ((Table<User>)client.Table<User>()).PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: full text search")]
        public void TestFiltersFullTextSearch()
        {
            var client = new Client(BaseUrl);

            // UrlEncoded [2,3]
            var exp = "(english).bar";
            var dict = new Dictionary<Operator, string>
            {
                { Operator.FTS, $"fts{exp}" },
                { Operator.PHFTS, $"phfts{exp}" },
                { Operator.PLFTS, $"plfts{exp}" },
                { Operator.WFTS, $"wfts{exp}" },
            };

            foreach (var pair in dict)
            {
                var config = new FullTextSearchConfig("bar", "english");
                var filter = new QueryFilter("foo", pair.Key, config);
                var result = ((Table<User>)client.Table<User>()).PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: ranges")]
        public void TestFiltersRanges()
        {
            var client = new Client(BaseUrl);

            var exp = "[2,3]";
            var dict = new Dictionary<Operator, string>
            {
                { Operator.StrictlyLeft, $"sl.{exp}" },
                { Operator.StrictlyRight, $"sr.{exp}" },
                { Operator.NotRightOf, $"nxr.{exp}" },
                { Operator.NotLeftOf, $"nxl.{exp}" },
                { Operator.Adjacent, $"adj.{exp}" },
            };

            foreach (var pair in dict)
            {
                var config = new IntRange(2, 3);
                var filter = new QueryFilter("foo", pair.Key, config);
                var result = ((Table<User>)client.Table<User>()).PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: not")]
        public void TestFiltersNot()
        {
            var client = new Client(BaseUrl);
            var filter = new QueryFilter("foo", Operator.Equals, "bar");
            var notFilter = new QueryFilter(Operator.Not, filter);
            var result = ((Table<User>)client.Table<User>()).PrepareFilter(notFilter);

            Assert.AreEqual("foo", result.Key);
            Assert.AreEqual("not.eq.bar", result.Value);
        }

        [TestMethod("filters: and & or")]
        public void TestFiltersAndOr()
        {
            var client = new Client(BaseUrl);
            var exp = "(a.gte.0,a.lte.100)";

            var dict = new Dictionary<Operator, string>
            {
                { Operator.And, $"and={exp}" },
                { Operator.Or, $"or={exp}" },
            };

            var filters = new List<IPostgrestQueryFilter>
            {
                new QueryFilter("a", Operator.GreaterThanOrEqual, "0"),
                new QueryFilter("a", Operator.LessThanOrEqual, "100")
            };

            foreach (var pair in dict)
            {
                var filter = new QueryFilter(pair.Key, filters);
                var result = ((Table<User>)client.Table<User>()).PrepareFilter(filter);
                Assert.AreEqual(pair.Value, $"{result.Key}={result.Value}");
            }
        }

        [TestMethod("update: basic")]
        public async Task TestBasicUpdate()
        {
            var client = new Client(BaseUrl);

            var user = await client.Table<User>().Filter("username", Operator.Equals, "supabot").Single();

            Assert.IsNotNull(user);

            // Update user status
            user.Status = "OFFLINE";
            var response = await user.Update<User>();

            var updatedUser = response.Models.FirstOrDefault();

            if (updatedUser == null)
                Assert.Fail();

            Assert.AreEqual(1, response.Models.Count);
            Assert.AreEqual(user.Username, updatedUser.Username);
            Assert.AreEqual(user.Status, updatedUser.Status);
        }


        [TestMethod("insert: basic")]
        public async Task TestBasicInsert()
        {
            var client = new Client(BaseUrl);

            var newUser = new User
            {
                Username = Guid.NewGuid().ToString(),
                AgeRange = new IntRange(18, 22),
                Catchphrase = "what a shot",
                Status = "ONLINE"
            };

            var response = await client.Table<User>().Insert(newUser);
            var insertedUser = response.Models.First();

            Assert.AreEqual(1, response.Models.Count);
            Assert.AreEqual(newUser.Username, insertedUser.Username);
            Assert.AreEqual(newUser.AgeRange, insertedUser.AgeRange);
            Assert.AreEqual(newUser.Status, insertedUser.Status);

            await client.Table<User>().Delete(newUser);

            var response2 = await client.Table<User>()
                .Insert(newUser, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal });
            Assert.AreEqual("", response2.Content);

            await client.Table<User>().Delete(newUser);
        }

        [TestMethod("insert: headers generated")]
        public void TestInsertHeaderGeneration()
        {
            var option = new QueryOptions();
            Assert.AreEqual("return=representation", option.ToHeaders()["Prefer"]);

            option.Returning = QueryOptions.ReturnType.Minimal;
            Assert.AreEqual("return=minimal", option.ToHeaders()["Prefer"]);

            option.Upsert = true;
            Assert.AreEqual("resolution=merge-duplicates,return=minimal", option.ToHeaders()["Prefer"]);

            option.DuplicateResolution = QueryOptions.DuplicateResolutionType.IgnoreDuplicates;
            Assert.AreEqual("resolution=ignore-duplicates,return=minimal", option.ToHeaders()["Prefer"]);

            option.Upsert = false;
            option.Count = QueryOptions.CountType.Exact;
            Assert.AreEqual("return=minimal,count=exact", option.ToHeaders()["Prefer"]);
        }

        [TestMethod(
            "Exceptions: Throws when inserting a user with same primary key value as an existing one without upsert option")]
        public async Task TestThrowsRequestExceptionInsertPkConflict()
        {
            var client = new Client(BaseUrl);

            await Assert.ThrowsExceptionAsync<PostgrestException>(async () =>
            {
                var newUser = new User
                {
                    Username = "supabot"
                };
                await client.Table<User>().Insert(newUser);
            });
        }

        [TestMethod("insert: upsert")]
        public async Task TestInsertWithUpsert()
        {
            var client = new Client(BaseUrl);

            var model = new User
            {
                Username = "supabot",
                AgeRange = new IntRange(3, 8),
                Status = "OFFLINE",
                Catchphrase = "fat cat"
            };

            var options = new QueryOptions
            {
                Upsert = true
            };

            var response = await client.Table<User>().Insert(model, options);

            var kitchenSink1 = new KitchenSink
            {
                Id = Guid.NewGuid(),
                UniqueValue = "Testing"
            };

            var ks1 = await client.Table<KitchenSink>().OnConflict("unique_value").Upsert(kitchenSink1);
            var uks1 = ks1.Models.First();

            await client.Table<KitchenSink>()
                .OnConflict(x => x.UniqueValue!)
                .Set(x => x.StringValue!, "Testing 1")
                .Upsert(uks1);

            var updatedUser = response.Models.First();

            Assert.AreEqual(1, response.Models.Count);
            Assert.AreEqual(model.Username, updatedUser.Username);
            Assert.AreEqual(model.AgeRange, updatedUser.AgeRange);
            Assert.AreEqual(model.Status, updatedUser.Status);

            await client.Table<Message>().Get();
        }

        [TestMethod("order: basic")]
        public async Task TestOrderBy()
        {
            var client = new Client(BaseUrl);

            // Test with a single orderer specified
            var orderedResponse = await client.Table<User>().Order("username", Ordering.Descending).Get();
            var unorderedResponse = await client.Table<User>().Get();

            var linqOrderedUsers = unorderedResponse.Models.OrderByDescending(u => u.Username).ToList();

            CollectionAssert.AreEqual(linqOrderedUsers, orderedResponse.Models);

            // Test with multiple orderers specified
            var multipleOrderedResponse = await client.Table<User>()
                .Order(u => u.Username!, Ordering.Descending)
                .Order(u => u.Status!, Ordering.Descending)
                .Get();

            linqOrderedUsers = unorderedResponse.Models
                .OrderByDescending(u => u.Username)
                .ThenByDescending(u => u.Status)
                .ToList();

            CollectionAssert.AreEqual(linqOrderedUsers, multipleOrderedResponse.Models);
        }

        [TestMethod("limit: basic")]
        public async Task TestLimit()
        {
            var client = new Client(BaseUrl);

            var limitedUsersResponse = await client.Table<User>().Limit(2).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaLimitUsers = limitedUsersResponse.Models;
            var linqLimitUsers = usersResponse.Models.Take(2).ToList();

            CollectionAssert.AreEqual(linqLimitUsers, supaLimitUsers);
        }

        [TestMethod("offset: basic")]
        public async Task TestOffset()
        {
            var client = new Client(BaseUrl);

            var offsetUsersResponse = await client.Table<User>().Offset(2).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaOffsetUsers = offsetUsersResponse.Models;
            var linqSkipUsers = usersResponse.Models.Skip(2).ToList();

            CollectionAssert.AreEqual(linqSkipUsers, supaOffsetUsers);
        }

        [TestMethod("range: from")]
        public async Task TestRangeFrom()
        {
            var client = new Client(BaseUrl);

            var rangeUsersResponse = await client.Table<User>().Range(2).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaRangeUsers = rangeUsersResponse.Models;
            var linqSkipUsers = usersResponse.Models.Skip(2).ToList();

            CollectionAssert.AreEqual(linqSkipUsers, supaRangeUsers);
        }

        [TestMethod("range: from and to")]
        public async Task TestRangeFromAndTo()
        {
            var client = new Client(BaseUrl);

            var rangeUsersResponse = await client.Table<User>().Range(1, 3).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaRangeUsers = rangeUsersResponse.Models;
            var linqRangeUsers = usersResponse.Models.Skip(1).Take(3).ToList();

            CollectionAssert.AreEqual(linqRangeUsers, supaRangeUsers);
        }

        [TestMethod("range: limit and offset")]
        public async Task TestRangeWithLimitAndOffset()
        {
            var client = new Client(BaseUrl);

            var rangeUsersResponse = await client.Table<User>().Limit(1).Offset(3).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaRangeUsers = rangeUsersResponse.Models;
            var linqRangeUsers = usersResponse.Models.Skip(3).Take(1).ToList();

            CollectionAssert.AreEqual(linqRangeUsers, supaRangeUsers);
        }

        [TestMethod("filters: not")]
        public async Task TestNotFilter()
        {
            var client = new Client(BaseUrl);
            var filter = new QueryFilter("username", Operator.Equals, "supabot");

            var filteredResponse = await client.Table<User>().Not(filter).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Username != "supabot").ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: `not` shorthand")]
        public async Task TestNotShorthandFilter()
        {
            var client = new Client(BaseUrl);

            // Standard NOT Equal Op.
            var filteredResponse = await client.Table<User>().Not("username", Operator.Equals, "supabot").Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Username != "supabot").ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);

            // NOT `In` Shorthand Op.
            var notInFilterResponse = await client.Table<User>()
                .Not("username", Operator.In, new List<string> { "supabot", "kiwicopple" }).Get();
            var supaNotInList = notInFilterResponse.Models;
            var linqNotInList = usersResponse.Models.Where(u => u.Username != "supabot")
                .Where(u => u.Username != "kiwicopple").ToList();

            CollectionAssert.AreEqual(supaNotInList, linqNotInList);
        }

        [TestMethod("filters: null operation `Equals`")]
        public async Task TestEqualsNullFilterEquals()
        {
            var client = new Client(BaseUrl);

            await client.Table<User>()
                .Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
                    new QueryOptions { Upsert = true });

            var filteredResponse =
                await client.Table<User>().Filter<string>("catchphrase", Operator.Equals, null).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase == null).ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: null operation `Is`")]
        public async Task TestEqualsNullFilterIs()
        {
            var client = new Client(BaseUrl);

            await client.Table<User>()
                .Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
                    new QueryOptions { Upsert = true });

            var filteredResponse = await client.Table<User>().Filter<string>("catchphrase", Operator.Is, null).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase == null).ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: null operation `NotEquals`")]
        public async Task TestEqualsNullFilterNotEquals()
        {
            var client = new Client(BaseUrl);

            await client.Table<User>()
                .Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
                    new QueryOptions { Upsert = true });

            var filteredResponse =
                await client.Table<User>().Filter<string>("catchphrase", Operator.NotEqual, null).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase != null).ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: null operation `Not`")]
        public async Task TestEqualsNullNot()
        {
            var client = new Client(BaseUrl);

            await client.Table<User>()
                .Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
                    new QueryOptions { Upsert = true });

            var filteredResponse = await client.Table<User>().Filter<string>("catchphrase", Operator.Not, null).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase != null).ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: in")]
        public async Task TestInFilter()
        {
            var client = new Client(BaseUrl);

            var criteria = new List<object> { "supabot", "kiwicopple" };

            var filteredResponse = await client.Table<User>().Filter("username", Operator.In, criteria)
                .Order("username", Ordering.Descending).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.OrderByDescending(u => u.Username)
                .Where(u => u.Username == "supabot" || u.Username == "kiwicopple").ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: eq")]
        public async Task TestEqualsFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<User>().Filter("username", Operator.Equals, "supabot").Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Username == "supabot").ToList();

            Assert.AreEqual(1, supaFilteredUsers.Count);
            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: gt")]
        public async Task TestGreaterThanFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.GreaterThan, "1").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id > 1).ToList();

            Assert.AreEqual(1, supaFilteredMessages.Count);
            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: gte")]
        public async Task TestGreaterThanOrEqualFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.GreaterThanOrEqual, "1").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id >= 1).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: lt")]
        public async Task TestLessThanFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.LessThan, "2").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id < 2).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: lte")]
        public async Task TestLessThanOrEqualFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.LessThanOrEqual, "2").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id <= 2).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: nqe")]
        public async Task TestNotEqualFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.NotEqual, "2").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id != 2).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: like")]
        public async Task TestLikeFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<Message>().Filter("username", Operator.Like, "s%").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.UserName!.StartsWith("s")).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: cs")]
        public async Task TestContainsFilter()
        {
            var client = new Client(BaseUrl);

            await client.Table<User>()
                .Insert(new User { Username = "skikra", Status = "ONLINE", AgeRange = new IntRange(1, 3) },
                    new QueryOptions { Upsert = true });
            var filteredResponse =
                await client.Table<User>().Filter("age_range", Operator.Contains, new IntRange(1, 2)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models
                .Where(m => m.AgeRange?.Start.Value <= 1 && m.AgeRange?.End.Value >= 2).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: cd")]
        public async Task TestContainedFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<User>()
                .Filter("age_range", Operator.ContainedIn, new IntRange(25, 35)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models
                .Where(m => m.AgeRange?.Start.Value >= 25 && m.AgeRange?.End.Value <= 35).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: sr")]
        public async Task TestStrictlyLeftFilter()
        {
            var client = new Client(BaseUrl);

            await client.Table<User>()
                .Insert(new User { Username = "minds3t", Status = "ONLINE", AgeRange = new IntRange(3, 6) },
                    new QueryOptions { Upsert = true });
            var filteredResponse = await client.Table<User>()
                .Filter("age_range", Operator.StrictlyLeft, new IntRange(7, 8)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value < 7 && m.AgeRange?.End.Value < 7)
                .ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: sl")]
        public async Task TestStrictlyRightFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<User>()
                .Filter("age_range", Operator.StrictlyRight, new IntRange(1, 2)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value > 2 && m.AgeRange?.End.Value > 2)
                .ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: nxl")]
        public async Task TestNotExtendToLeftFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse =
                await client.Table<User>().Filter("age_range", Operator.NotLeftOf, new IntRange(2, 4)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models
                .Where(m => m.AgeRange?.Start.Value >= 2 && m.AgeRange?.End.Value >= 2).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: nxr")]
        public async Task TestNotExtendToRightFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse =
                await client.Table<User>().Filter("age_range", Operator.NotRightOf, new IntRange(2, 4)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models
                .Where(m => m.AgeRange?.Start.Value <= 4 && m.AgeRange?.End.Value <= 4).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: adj")]
        public async Task TestAdjacentFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse =
                await client.Table<User>().Filter("age_range", Operator.Adjacent, new IntRange(1, 2)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models
                .Where(m => m.AgeRange?.End.Value == 0 || m.AgeRange?.Start.Value == 3).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: ov")]
        public async Task TestOverlapFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse =
                await client.Table<User>().Filter("age_range", Operator.Overlap, new IntRange(2, 4)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models
                .Where(m => m.AgeRange?.Start.Value <= 4 && m.AgeRange?.End.Value >= 2).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: ilike")]
        public async Task TestILikeFilter()
        {
            var client = new Client(BaseUrl);

            var filteredResponse = await client.Table<Message>().Filter("username", Operator.ILike, "%SUPA%").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models
                .Where(m => m.UserName!.Contains("SUPA", StringComparison.OrdinalIgnoreCase)).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: fts")]
        public async Task TestFullTextSearch()
        {
            var client = new Client(BaseUrl);
            var config = new FullTextSearchConfig("'fat' & 'cat'", "english");

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.FTS, config).Get();

            Assert.AreEqual(1, filteredResponse.Models.Count);
            Assert.AreEqual("supabot", filteredResponse.Models.FirstOrDefault()?.Username);
        }

        [TestMethod("filters: plfts")]
        public async Task TestPlainToFullTextSearch()
        {
            var client = new Client(BaseUrl);
            var config = new FullTextSearchConfig("'fat' & 'cat'", "english");

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.PLFTS, config).Get();

            Assert.AreEqual(1, filteredResponse.Models.Count);
            Assert.AreEqual("supabot", filteredResponse.Models.FirstOrDefault()?.Username);
        }

        [TestMethod("filters: phfts")]
        public async Task TestPhraseToFullTextSearch()
        {
            var client = new Client(BaseUrl);
            var config = new FullTextSearchConfig("'cat'", "english");

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.PHFTS, config).Get();
            var usersResponse = await client.Table<User>().Filter<string>("catchphrase", Operator.NotEqual, null).Get();

            var testAgainst = usersResponse.Models.Where(u => u.Catchphrase!.Contains("'cat'")).ToList();
            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: wfts")]
        public async Task TestWebFullTextSearch()
        {
            var client = new Client(BaseUrl);
            var config = new FullTextSearchConfig("'fat' & 'cat'", "english");

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.WFTS, config).Get();

            Assert.AreEqual(1, filteredResponse.Models.Count);
            Assert.AreEqual("supabot", filteredResponse.Models.FirstOrDefault()?.Username);
        }

        [TestMethod("filters: match")]
        public async Task TestMatchFilter()
        {
            //Arrange
            var client = new Client(BaseUrl);
            var usersResponse = await client.Table<User>().Get();
            var expected = usersResponse.Models.Where(u => u.Username == "kiwicopple" && u.Status == "OFFLINE")
                .ToList();

            //Act
            var filters = new Dictionary<string, string>
            {
                { "username", "kiwicopple" },
                { "status", "OFFLINE" }
            };
            var filteredResponse = await client.Table<User>().Match(filters).Get();

            //Assert
            CollectionAssert.AreEqual(expected, filteredResponse.Models);
        }

        [TestMethod("filters: dt")]
        public async Task TestDateTimeFilter()
        {
            var client = new Client(BaseUrl);
            var filteredResponse = await client.Table<Movie>().Filter("created_at", Operator.GreaterThan, new DateTime(2022, 08, 20))
                                                              .Filter("created_at", Operator.LessThan, new DateTime(2022, 08, 21))
                                                              .Get();
            Assert.AreEqual(1, filteredResponse.Models.Count);
            Assert.AreEqual("ea07bd86-a507-4c68-9545-b848bfe74c90", filteredResponse.Models[0].Id);
        }

        [TestMethod("filters: dto")]
        public async Task TestDateTimeOffsetFilter()
        {
            var client = new Client(BaseUrl);
            var filteredResponse = await client.Table<Movie>().Filter("created_at", Operator.GreaterThan, new DateTimeOffset(new DateTime(2022, 08, 20)))
                                                              .Filter("created_at", Operator.LessThan, new DateTimeOffset(new DateTime(2022, 08, 21)))
                                                              .Get();
            Assert.AreEqual(1, filteredResponse.Models.Count);
            Assert.AreEqual("ea07bd86-a507-4c68-9545-b848bfe74c90", filteredResponse.Models[0].Id);
        }

        [TestMethod("filters: long")]
        public async Task TestLongIntFilter()
        {
            var client = new Client(BaseUrl);
            var filteredResponse = await client.Table<KitchenSink>().Filter("long_value", Operator.Equals, 2147483648L)
                                                                    .Get();
            Assert.AreEqual(1, filteredResponse.Models.Count);
        }

        [TestMethod("select: basic")]
        public async Task TestSelect()
        {
            var client = new Client(BaseUrl);

            var response = await client.Table<User>().Select("username").Get();
            foreach (var user in response.Models)
            {
                Assert.IsNotNull(user.Username);
                Assert.IsNull(user.Catchphrase);
                Assert.IsNull(user.Status);
            }
        }

        [TestMethod("select: multiple columns")]
        public async Task TestSelectWithMultipleColumns()
        {
            var client = new Client(BaseUrl);

            var response = await client.Table<User>().Select("username,status").Get();
            foreach (var user in response.Models)
            {
                Assert.IsNotNull(user.Username);
                Assert.IsNotNull(user.Status);
                Assert.IsNull(user.Catchphrase);
            }
        }

        [TestMethod("insert: bulk")]
        public async Task TestInsertBulk()
        {
            var client = new Client(BaseUrl);
            var rocketUser = new User
            {
                Username = "rocket",
                AgeRange = new IntRange(35, 40),
                Status = "ONLINE"
            };

            var aceUser = new User
            {
                Username = "ace",
                AgeRange = new IntRange(21, 28),
                Status = "OFFLINE"
            };

            var users = new List<User>
            {
                rocketUser,
                aceUser
            };

            var response = await client.Table<User>().Insert(users);
            var insertedUsers = response.Models;


            CollectionAssert.AreEqual(users, insertedUsers);

            await client.Table<User>().Delete(rocketUser);
            await client.Table<User>().Delete(aceUser);
        }

        [TestMethod("count")]
        public async Task TestCount()
        {
            var client = new Client(BaseUrl);

            var resp = await client.Table<User>().Count(CountType.Exact);
            // Lame, I know. We should check an actual number. However, the tests are run asynchronously
            // so we get inconsistent counts depending on the order that the tests are actually executed.
            Assert.IsNotNull(resp);
        }

        [TestMethod("count: with filter")]
        public async Task TestCountWithFilter()
        {
            var client = new Client(BaseUrl);

            var resp = await client.Table<User>().Filter("status", Operator.Equals, "ONLINE").Count(CountType.Exact);
            Assert.IsNotNull(resp);
        }

        [TestMethod("response count")]
        public async Task TestCountInResponse()
        {
            var client = new Client(BaseUrl);

            var resp = await client.Table<User>().Get(default, CountType.Exact);
            Assert.IsTrue(resp.Count > -1);
        }
        
        [TestMethod("response count: with filter")]
        public async Task TestCountInResponseWithFilter()
        {
            var client = new Client(BaseUrl);

            var resp = await client.Table<User>().Filter("status", Operator.Equals, "ONLINE").Get(default, CountType.Exact);
            Assert.IsTrue(resp.Count > -1);
        }

        [TestMethod("support: int arrays")]
        public async Task TestSupportIntArraysAsLists()
        {
            var client = new Client(BaseUrl);

            var numbers = new List<int> { 1, 2, 3 };
            var result = await client.Table<User>()
                .Insert(
                    new User
                    {
                        Username = "WALRUS", Status = "ONLINE", Catchphrase = "I'm a walrus", FavoriteNumbers = numbers,
                        AgeRange = new IntRange(15, 25)
                    }, new QueryOptions { Upsert = true });

            CollectionAssert.AreEqual(numbers, result.Models.First().FavoriteNumbers);
        }

        [TestMethod("stored procedure")]
        public async Task TestStoredProcedure()
        {
            //Arrange 
            var client = new Client(BaseUrl);

            //Act 
            var parameters = new Dictionary<string, object>
            {
                { "name_param", "supabot" }
            };
            var response = await client.Rpc("get_status", parameters);

            //Assert 
            Assert.AreEqual(true, response.ResponseMessage?.IsSuccessStatusCode);
            Assert.AreEqual(true, response.Content?.Contains("OFFLINE"));
        }

        [TestMethod("stored procedure with row param")]
        public async Task TestStoredProcedureWithRowParam()
        {
            //Arrange 
            var client = new Client(BaseUrl);

            //Act 
            var parameters = new Dictionary<string, object>
            {
                {
                    "param",
                    new Dictionary<string, object>
                    {
                        { "username", "supabot" }
                    }
                }
            };
            var response = await client.Rpc("get_data", parameters);

            //Assert 
            Assert.AreEqual(true, response.ResponseMessage?.IsSuccessStatusCode);
            Assert.AreEqual("null", response.Content);
        }


        [TestMethod("switch schema")]
        public async Task TestSwitchSchema()
        {
            //Arrange
            var options = new ClientOptions
            {
                Schema = "personal"
            };
            var client = new Client(BaseUrl, options);

            //Act 
            var response = await client.Table<User>().Filter(x => x.Username!, Operator.Equals, "leroyjenkins").Get();

            //Assert 
            Assert.AreEqual(1, response.Models.Count);
            Assert.AreEqual("leroyjenkins", response.Models.FirstOrDefault()?.Username);
        }

        [TestMethod("Test cancellation token")]
        public async Task TestCancellationToken()
        {
            var client = new Client(BaseUrl);
            var now = DateTime.UtcNow;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromTicks(1));

            var model = new KitchenSink
            {
                DateTimeValue = now,
                DateTimeValue1 = now
            };

            ModeledResponse<KitchenSink>? insertResponse = null;

            try
            {
                insertResponse = await client.Table<KitchenSink>().Insert(model, cancellationToken: cts.Token);
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(TaskCanceledException));
                Assert.IsNull(insertResponse);
            }
        }

        [TestMethod("columns")]
        public async Task TestColumns()
        {
            var client = new Client(BaseUrl);

            var movies = await client.Table<Movie>().Get();
            var first = movies.Models.First();
            var originalName = first.Name;
            var originalDate = first.CreatedAt;
            var newName = $"{first.Name} (Changed)";

            first.Name = newName;
            first.CreatedAt = DateTime.UtcNow;

            var result = await client.Table<Movie>().Columns(new[] { "name" }).Update(first);

            Assert.AreEqual(originalDate, result.Models.First().CreatedAt);
            Assert.AreNotEqual(originalName, result.Models.First().Name);
        }

        [TestMethod("OnRequestPrepared is fired.")]
        public async Task TestOnRequestPreparedEvent()
        {
            var tsc = new TaskCompletionSource<bool>();
            var client = new Client(BaseUrl);

            var timer1 = new Stopwatch();
            var timer2 = new Stopwatch();

            timer1.Start();
            timer2.Start();

            client.AddRequestPreparedHandler((_, _, _, _, _, _, _) =>
            {
                timer1.Stop();
                tsc.TrySetResult(true);
            });

            var request = client.Table<Movie>();

            await request.Get();
            timer2.Stop();

            await tsc.Task;

            Assert.IsTrue(timer1.ElapsedTicks < timer2.ElapsedTicks);
        }
    }
}