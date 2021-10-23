using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest;
using PostgrestTests.Models;
using System.Threading.Tasks;
using System.Linq;
using static Postgrest.Constants;
using System.Net.Http;

namespace PostgrestTests
{
    [TestClass]
    public class Api
    {
        private static string baseUrl = "http://localhost:3000";

        [TestMethod("Initilizes")]
        public void TestInitilization()
        {
            var client = Client.Initialize(baseUrl, null);
            Assert.AreEqual(baseUrl, client.BaseUrl);
        }

        [TestMethod("with optional query params")]
        public void TestQueryParams()
        {
            var client = Client.Initialize(baseUrl, options: new ClientOptions
            {
                QueryParams = new Dictionary<string, string>
                {
                    { "some-param", "foo" },
                    { "other-param", "bar" }
                }
            });

            Assert.AreEqual($"{baseUrl}/users?some-param=foo&other-param=bar", client.Table<User>().GenerateUrl());
        }

        [TestMethod("will use TableAttribute")]
        public void TestTableAttribute()
        {
            var client = Client.Initialize(baseUrl, null);
            Assert.AreEqual($"{baseUrl}/users", client.Table<User>().GenerateUrl());
        }

        [TestMethod("will default to Class.name in absence of TableAttribute")]
        public void TestTableAttributeDefault()
        {
            var client = Client.Initialize(baseUrl, null);
            Assert.AreEqual($"{baseUrl}/Stub", client.Table<Stub>().GenerateUrl());
        }

        [TestMethod("will set header from options")]
        public void TestHeadersToken()
        {
            var headers = Helpers.PrepareRequestHeaders(HttpMethod.Get, new Dictionary<string, string> { { "Authorization", $"Bearer token" } });

            Assert.AreEqual("Bearer token", headers["Authorization"]);
        }

        [TestMethod("will set apikey as query string")]
        public void TestQueryApiKey()
        {
            var client = Client.Initialize(baseUrl, new ClientOptions
            {
                Headers =
                {
                    { "apikey", "some-key" }
                }
            });
            Assert.AreEqual($"{baseUrl}/users?apikey=some-key", client.Table<User>().GenerateUrl());
        }

        [TestMethod("filters: simple")]
        public void TestFiltersSimple()
        {
            var client = Client.Initialize(baseUrl);
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
                var result = client.Table<User>().PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: like & ilike")]
        public void TestFiltersLike()
        {
            var client = Client.Initialize(baseUrl);
            var dict = new Dictionary<Constants.Operator, string>
            {
                { Constants.Operator.Like, "like.*bar*" },
                { Constants.Operator.ILike, "ilike.*bar*" },
            };

            foreach (var pair in dict)
            {
                var filter = new QueryFilter("foo", pair.Key, "%bar%");
                var result = client.Table<User>().PrepareFilter(filter);
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
            var client = Client.Initialize(baseUrl);

            // UrlEncoded {"bar","buzz"}
            string exp = "(\"bar\",\"buzz\")";
            var dict = new Dictionary<Constants.Operator, string>
            {
                { Constants.Operator.In, $"in.{exp}" },
            };

            foreach (var pair in dict)
            {
                var list = new List<object> { "bar", "buzz" };
                var filter = new QueryFilter("foo", pair.Key, list);
                var result = client.Table<User>().PrepareFilter(filter);
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
            var client = Client.Initialize(baseUrl);

            // UrlEncoded {bar,buzz} - according to documentation, does not accept quoted strings
            string exp = "{bar,buzz}";
            var dict = new Dictionary<Constants.Operator, string>
            {
                { Constants.Operator.Contains, $"cs.{exp}" },
                { Constants.Operator.ContainedIn, $"cd.{exp}" },
                { Constants.Operator.Overlap, $"ov.{exp}" },
            };

            foreach (var pair in dict)
            {
                var list = new List<object> { "bar", "buzz" };
                var filter = new QueryFilter("foo", pair.Key, list);
                var result = client.Table<User>().PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: arrays with Dictionary<string,object> arguments")]
        public void TestFiltersArraysWithDictionaries()
        {
            var client = Client.Initialize(baseUrl);

            string exp = "{\"bar\":100,\"buzz\":\"zap\"}";
            var dict = new Dictionary<Constants.Operator, string>
            {
                { Constants.Operator.In, $"in.{exp}" },
                { Constants.Operator.Contains, $"cs.{exp}" },
                { Constants.Operator.ContainedIn, $"cd.{exp}" },
                { Constants.Operator.Overlap, $"ov.{exp}" },
            };

            foreach (var pair in dict)
            {
                var value = new Dictionary<string, object> { { "bar", 100 }, { "buzz", "zap" } };
                var filter = new QueryFilter("foo", pair.Key, value);
                var result = client.Table<User>().PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: full text search")]
        public void TestFiltersFullTextSearch()
        {
            var client = Client.Initialize(baseUrl);

            // UrlEncoded [2,3]
            var exp = "(english).bar";
            var dict = new Dictionary<Constants.Operator, string>
            {
                { Constants.Operator.FTS, $"fts{exp}" },
                { Constants.Operator.PHFTS, $"phfts{exp}" },
                { Constants.Operator.PLFTS, $"plfts{exp}" },
                { Constants.Operator.WFTS, $"wfts{exp}" },
            };

            foreach (var pair in dict)
            {
                var config = new FullTextSearchConfig("bar", "english");
                var filter = new QueryFilter("foo", pair.Key, config);
                var result = client.Table<User>().PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: ranges")]
        public void TestFiltersRanges()
        {
            var client = Client.Initialize(baseUrl);

            var exp = "[2,3]";
            var dict = new Dictionary<Constants.Operator, string>
            {
                { Constants.Operator.StrictlyLeft, $"sl.{exp}" },
                { Constants.Operator.StrictlyRight, $"sr.{exp}" },
                { Constants.Operator.NotRightOf, $"nxr.{exp}" },
                { Constants.Operator.NotLeftOf, $"nxl.{exp}" },
                { Constants.Operator.Adjacent, $"adj.{exp}" },
            };

            foreach (var pair in dict)
            {
                var config = new IntRange(2, 3);
                var filter = new QueryFilter("foo", pair.Key, config);
                var result = client.Table<User>().PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: not")]
        public void TestFiltersNot()
        {
            var client = Client.Initialize(baseUrl);
            var filter = new QueryFilter("foo", Constants.Operator.Equals, "bar");
            var notFilter = new QueryFilter(Constants.Operator.Not, filter);
            var result = client.Table<User>().PrepareFilter(notFilter);

            Assert.AreEqual("foo", result.Key);
            Assert.AreEqual("not.eq.bar", result.Value);
        }

        [TestMethod("filters: and & or")]
        public void TestFiltersAndOr()
        {
            var client = Client.Initialize(baseUrl);
            var exp = "(a.gte.0,a.lte.100)";

            var dict = new Dictionary<Constants.Operator, string>
            {
                { Constants.Operator.And, $"and={exp}" },
                { Constants.Operator.Or, $"or={exp}" },
            };

            var subfilters = new List<QueryFilter> {
                new QueryFilter("a", Constants.Operator.GreaterThanOrEqual, "0"),
                new QueryFilter("a", Constants.Operator.LessThanOrEqual, "100")
            };

            foreach (var pair in dict)
            {
                var filter = new QueryFilter(pair.Key, subfilters);
                var result = client.Table<User>().PrepareFilter(filter);
                Assert.AreEqual(pair.Value, $"{result.Key}={result.Value}");
            }
        }

        [TestMethod("update: basic")]
        public async Task TestBasicUpdate()
        {
            var client = Client.Initialize(baseUrl);

            var user = await client.Table<User>().Filter("username", Postgrest.Constants.Operator.Equals, "supabot").Single();

            if (user != null)
            {
                // Update user status
                user.Status = "OFFLINE";
                var response = await user.Update<User>();

                var updatedUser = response.Models.FirstOrDefault();

                Assert.AreEqual(1, response.Models.Count);
                Assert.AreEqual(user.Username, updatedUser.Username);
                Assert.AreEqual(user.Status, updatedUser.Status);

            }
        }

        [TestMethod("Exceptions: Throws when attempting to update a non-existent record")]
        public async Task TestThrowsRequestExceptionOnNonExistantUpdate()
        {
            var client = Client.Initialize(baseUrl);

            await Assert.ThrowsExceptionAsync<RequestException>(async () =>
            {
                var nonExistentRecord = new User
                {
                    Username = "Foo",
                    Status = "Bar"
                };
                await nonExistentRecord.Update<User>();

            });
        }

        [TestMethod("insert: basic")]
        public async Task TestBasicInsert()
        {
            var client = Client.Initialize(baseUrl);

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
        }

        [TestMethod("insert: headers generated")]
        public void TestInsertHeaderGeneration()
        {
            var option = new QueryOptions { };
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

        [TestMethod("Exceptions: Throws when inserting a user with same primary key value as an existing one without upsert option")]
        public async Task TestThrowsRequestExceptionInsertPkConflict()
        {
            var client = Client.Initialize(baseUrl);

            await Assert.ThrowsExceptionAsync<RequestException>(async () =>
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
            var client = Client.Initialize(baseUrl);

            var supaUpdated = new User
            {
                Username = "supabot",
                AgeRange = new IntRange(3, 8),
                Status = "OFFLINE",
                Catchphrase = "fat cat"
            };

            var insertOptions = new QueryOptions
            {
                Upsert = true
            };

            var response = await client.Table<User>().Insert(supaUpdated, insertOptions);
            var updatedUser = response.Models.First();

            Assert.AreEqual(1, response.Models.Count);
            Assert.AreEqual(supaUpdated.Username, updatedUser.Username);
            Assert.AreEqual(supaUpdated.AgeRange, updatedUser.AgeRange);
            Assert.AreEqual(supaUpdated.Status, updatedUser.Status);
        }

        [TestMethod("order: basic")]
        public async Task TestOrderBy()
        {
            var client = Client.Initialize(baseUrl);

            var orderedResponse = await client.Table<User>().Order("username", Constants.Ordering.Descending).Get();
            var unorderedResponse = await client.Table<User>().Get();

            var supaOrderedUsers = orderedResponse.Models;
            var linqOrderedUsers = unorderedResponse.Models.OrderByDescending(u => u.Username).ToList();

            CollectionAssert.AreEqual(linqOrderedUsers, supaOrderedUsers);
        }

        [TestMethod("limit: basic")]
        public async Task TestLimit()
        {
            var client = Client.Initialize(baseUrl);

            var limitedUsersResponse = await client.Table<User>().Limit(2).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaLimitUsers = limitedUsersResponse.Models;
            var linqLimitUsers = usersResponse.Models.Take(2).ToList();

            CollectionAssert.AreEqual(linqLimitUsers, supaLimitUsers);
        }

        [TestMethod("offset: basic")]
        public async Task TestOffset()
        {
            var client = Client.Initialize(baseUrl);

            var offsetUsersResponse = await client.Table<User>().Offset(2).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaOffsetUsers = offsetUsersResponse.Models;
            var linqSkipUsers = usersResponse.Models.Skip(2).ToList();

            CollectionAssert.AreEqual(linqSkipUsers, supaOffsetUsers);
        }

        [TestMethod("range: from")]
        public async Task TestRangeFrom()
        {
            var client = Client.Initialize(baseUrl);

            var rangeUsersResponse = await client.Table<User>().Range(2).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaRangeUsers = rangeUsersResponse.Models;
            var linqSkipUsers = usersResponse.Models.Skip(2).ToList();

            CollectionAssert.AreEqual(linqSkipUsers, supaRangeUsers);
        }

        [TestMethod("range: from and to")]
        public async Task TestRangeFromAndTo()
        {
            var client = Client.Initialize(baseUrl);

            var rangeUsersResponse = await client.Table<User>().Range(1, 3).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaRangeUsers = rangeUsersResponse.Models;
            var linqRangeUsers = usersResponse.Models.Skip(1).Take(3).ToList();

            CollectionAssert.AreEqual(linqRangeUsers, supaRangeUsers);
        }

        [TestMethod("range: limit and offset")]
        public async Task TestRangeWithLimitAndOffset()
        {
            var client = Client.Initialize(baseUrl);

            var rangeUsersResponse = await client.Table<User>().Limit(1).Offset(3).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaRangeUsers = rangeUsersResponse.Models;
            var linqRangeUsers = usersResponse.Models.Skip(3).Take(1).ToList();

            CollectionAssert.AreEqual(linqRangeUsers, supaRangeUsers);
        }

        [TestMethod("filters: not")]
        public async Task TestNotFilter()
        {
            var client = Client.Initialize(baseUrl);
            var filter = new QueryFilter("username", Constants.Operator.Equals, "supabot");

            var filteredResponse = await client.Table<User>().Not(filter).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Username != "supabot").ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: `not` shorthand")]
        public async Task TestNotShorthandFilter()
        {
            var client = Client.Initialize(baseUrl);

            // Standard NOT Equal Op.
            var filteredResponse = await client.Table<User>().Not("username", Operator.Equals, "supabot").Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Username != "supabot").ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);

            // NOT `In` Shorthand Op.
            var notInFilterResponse = await client.Table<User>().Not("username", Operator.In, new List<object> { "supabot", "kiwicopple" }).Get();
            var supaNotInList = notInFilterResponse.Models;
            var linqNotInList = usersResponse.Models.Where(u => u.Username != "supabot").Where(u => u.Username != "kiwicopple").ToList();

            CollectionAssert.AreEqual(supaNotInList, linqNotInList);
        }

        [TestMethod("filters: null operation `Equals`")]
        public async Task TestEqualsNullFilterEquals()
        {
            var client = Client.Initialize(baseUrl);

            await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null }, new QueryOptions { Upsert = true });

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.Equals, null).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase == null).ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: null operation `Is`")]
        public async Task TestEqualsNullFilterIs()
        {
            var client = Client.Initialize(baseUrl);

            await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null }, new QueryOptions { Upsert = true });

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.Is, null).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase == null).ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: null operation `NotEquals`")]
        public async Task TestEqualsNullFilterNotEquals()
        {
            var client = Client.Initialize(baseUrl);

            await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null }, new QueryOptions { Upsert = true });

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.NotEqual, null).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase != null).ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: null operation `Not`")]
        public async Task TestEqualsNullNot()
        {
            var client = Client.Initialize(baseUrl);

            await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null }, new QueryOptions { Upsert = true });

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.Not, null).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase != null).ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: in")]
        public async Task TestInFilter()
        {
            var client = Client.Initialize(baseUrl);

            var criteria = new List<object> { "supabot", "kiwicopple" };

            var filteredResponse = await client.Table<User>().Filter("username", Operator.In, criteria).Order("username", Ordering.Descending).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.OrderByDescending(u => u.Username).Where(u => u.Username == "supabot" || u.Username == "kiwicopple").ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: eq")]
        public async Task TestEqualsFilter()
        {
            var client = Client.Initialize(baseUrl);

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
            var client = Client.Initialize(baseUrl);

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
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.GreaterThanOrEqual, "1").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id >= 1).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: lt")]
        public async Task TestlessThanFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.LessThan, "2").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id < 2).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: lte")]
        public async Task TestLessThanOrEqualFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.LessThanOrEqual, "2").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id <= 2).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: nqe")]
        public async Task TestNotEqualFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.NotEqual, "2").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id != 2).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: like")]
        public async Task TestLikeFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<Message>().Filter("username", Operator.Like, "s%").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.UserName.StartsWith('s')).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: cs")]
        public async Task TestContainsFilter()
        {
            var client = Client.Initialize(baseUrl);

            await client.Table<User>().Insert(new User { Username = "skikra", Status = "ONLINE", AgeRange = new IntRange(1, 3) }, new QueryOptions { Upsert = true });
            var filteredResponse = await client.Table<User>().Filter("age_range", Operator.Contains, new IntRange(1, 2)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value <= 1 && m.AgeRange?.End.Value >= 2).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: cd")]
        public async Task TestContainedFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<User>().Filter("age_range", Operator.ContainedIn, new IntRange(25, 35)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value >= 25 && m.AgeRange?.End.Value <= 35).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: sr")]
        public async Task TestStrictlyLeftFilter()
        {
            var client = Client.Initialize(baseUrl);

            await client.Table<User>().Insert(new User { Username = "minds3t", Status = "ONLINE", AgeRange = new IntRange(3, 6) }, new QueryOptions { Upsert = true });
            var filteredResponse = await client.Table<User>().Filter("age_range", Operator.StrictlyLeft, new IntRange(7, 8)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value < 7 && m.AgeRange?.End.Value < 7).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: sl")]
        public async Task TestStrictlyRightFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<User>().Filter("age_range", Operator.StrictlyRight, new IntRange(1, 2)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value > 2 && m.AgeRange?.End.Value > 2).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: nxl")]
        public async Task TestNotExtendToLeftFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<User>().Filter("age_range", Operator.NotLeftOf, new IntRange(2, 4)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value >= 2 && m.AgeRange?.End.Value >= 2).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: nxr")]
        public async Task TestNotExtendToRightFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<User>().Filter("age_range", Operator.NotRightOf, new IntRange(2, 4)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value <= 4 && m.AgeRange?.End.Value <= 4).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: adj")]
        public async Task TestAdjacentFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<User>().Filter("age_range", Operator.Adjacent, new IntRange(1, 2)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.End.Value == 0 || m.AgeRange?.Start.Value == 3).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: ov")]
        public async Task TestOverlapFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<User>().Filter("age_range", Operator.Overlap, new IntRange(2, 4)).Get();
            var usersResponse = await client.Table<User>().Get();

            var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value <= 4 && m.AgeRange?.End.Value >= 2).ToList();

            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: ilike")]
        public async Task TestILikeFilter()
        {
            var client = Client.Initialize(baseUrl);

            var filteredResponse = await client.Table<Message>().Filter("username", Operator.ILike, "%SUPA%").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.UserName.Contains("SUPA", StringComparison.OrdinalIgnoreCase)).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: fts")]
        public async Task TestFullTextSearch()
        {
            var client = Client.Initialize(baseUrl);
            var config = new FullTextSearchConfig("'fat' & 'cat'", "english");

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.FTS, config).Get();

            Assert.AreEqual(1, filteredResponse.Models.Count);
            Assert.AreEqual("supabot", filteredResponse.Models.FirstOrDefault()?.Username);
        }

        [TestMethod("filters: plfts")]
        public async Task TestPlaintoFullTextSearch()
        {
            var client = Client.Initialize(baseUrl);
            var config = new FullTextSearchConfig("'fat' & 'cat'", "english");

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.PLFTS, config).Get();

            Assert.AreEqual(1, filteredResponse.Models.Count);
            Assert.AreEqual("supabot", filteredResponse.Models.FirstOrDefault()?.Username);
        }

        [TestMethod("filters: phfts")]
        public async Task TestPhrasetoFullTextSearch()
        {
            var client = Client.Initialize(baseUrl);
            var config = new FullTextSearchConfig("'cat'", "english");

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.PHFTS, config).Get();
            var usersResponse = await client.Table<User>().Filter("catchphrase", Operator.NotEqual, null).Get();

            var testAgainst = usersResponse.Models.Where(u => u.Catchphrase.Contains("'cat'")).ToList();
            CollectionAssert.AreEqual(testAgainst, filteredResponse.Models);
        }

        [TestMethod("filters: wfts")]
        public async Task TestWebFullTextSearch()
        {
            var client = Client.Initialize(baseUrl);
            var config = new FullTextSearchConfig("'fat' & 'cat'", "english");

            var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.WFTS, config).Get();

            Assert.AreEqual(1, filteredResponse.Models.Count);
            Assert.AreEqual("supabot", filteredResponse.Models.FirstOrDefault()?.Username);
        }

        [TestMethod("filters: match")]
        public async Task TestMatchFilter()
        {
            //Arrange
            var client = Client.Initialize(baseUrl);
            var usersResponse = await client.Table<User>().Get();
            var testAgaint = usersResponse.Models.Where(u => u.Username == "kiwicopple" && u.Status == "OFFLINE").ToList();

            //Act
            var filters = new Dictionary<string, string>()
            {
                { "username", "kiwicopple" },
                { "status", "OFFLINE" }
            };
            var filteredResponse = await client.Table<User>().Match(filters).Get();

            //Assert
            CollectionAssert.AreEqual(testAgaint, filteredResponse.Models);
        }

        [TestMethod("select: basic")]
        public async Task TestSelect()
        {
            var client = Client.Initialize(baseUrl);

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
            var client = Client.Initialize(baseUrl);

            var response = await client.Table<User>().Select("username, status").Get();
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
            var client = Client.Initialize(baseUrl);
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
            var client = Client.Initialize(baseUrl);

            var resp = await client.Table<User>().Count(CountType.Exact);
            // Lame, I know. We should check an actual number. However, the tests are run asynchronously
            // so we get inconsitent counts depending on the order that the tests are actually executed.
            Assert.IsNotNull(resp);
        }

        [TestMethod("count: with filter")]
        public async Task TestCountWithFilter()
        {
            var client = Client.Initialize(baseUrl);

            var resp = await client.Table<User>().Filter("status", Operator.Equals, "ONLINE").Count(CountType.Exact);
            Assert.IsNotNull(resp);
        }

        [TestMethod("support: int arrays")]
        public async Task TestSupportIntArraysAsLists()
        {
            var client = Client.Initialize(baseUrl);

            var numbers = new List<int> { 1, 2, 3 };
            var result = await client.Table<User>().Insert(new User { Username = "WALRUS", FavoriteNumbers = numbers, AgeRange = new IntRange(15, 25) }, new QueryOptions { Upsert = true });

            CollectionAssert.AreEqual(numbers, result.Models.First().FavoriteNumbers);
        }

        [TestMethod("stored procedure")]
        public async Task TestStoredProcedure()
        {
            //Arrange 
            var client = Client.Initialize(baseUrl);

            //Act 
            var parameters = new Dictionary<string, object>()
            {
                { "name_param", "supabot" }
            };
            var response = await client.Rpc("get_status", parameters);

            //Assert 
            Assert.AreEqual(true, response.ResponseMessage.IsSuccessStatusCode);
            Assert.AreEqual(true, response.Content.Contains("OFFLINE"));
        }

        [TestMethod("switch schema")]
        public async Task TestSwitchSchema()
        {
            //Arrange
            var options = new ClientOptions
            {
                Schema = "personal"
            };
            var client = Client.Initialize(baseUrl, options);

            //Act 
            var response = await client.Table<User>().Filter("username", Operator.Equals, "leroyjenkins").Get();

            //Assert 
            Assert.AreEqual(1, response.Models.Count);
            Assert.AreEqual("leroyjenkins", response.Models.FirstOrDefault()?.Username);
        }
    }
}
