using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest;
using Postgrest.Extensions;
using Postgrest.Attributes;
using PostgrestTests.Models;
using static Postgrest.ClientAuthorization;
using System.Threading.Tasks;
using System.Linq;
using static Postgrest.Constants;

namespace PostgrestTests
{
    [TestClass]
    public class Api
    {
        private static string baseUrl = "http://localhost:3000";

        [TestMethod("Initilizes")]
        public void TestInitilization()
        {
            var client = Client.Instance.Initialize(baseUrl, null, null);
            Assert.AreEqual(baseUrl, client.BaseUrl);
        }

        [TestMethod("with optional query params")]
        public void TestQueryParams()
        {
            var client = Client.Instance.Initialize(baseUrl, null, options: new ClientOptions
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
            var client = Client.Instance.Initialize(baseUrl, null);
            Assert.AreEqual($"{baseUrl}/users", client.Table<User>().GenerateUrl());
        }

        [TestMethod("will default to Class.name in absence of TableAttribute")]
        public void TestTableAttributeDefault()
        {
            var client = Client.Instance.Initialize(baseUrl, null);
            Assert.AreEqual($"{baseUrl}/Stub", client.Table<Stub>().GenerateUrl());
        }

        [TestMethod("will set Authorization header from token")]
        public void TestHeadersToken()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Token, "token"), null);
            var headers = client.Table<User>().PrepareRequestHeaders();

            Assert.AreEqual("Bearer token", headers["Authorization"]);
        }

        [TestMethod("will set apikey query string")]
        public void TestQueryApiKey()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.ApiKey, "some-key"));
            Assert.AreEqual($"{baseUrl}/users?apikey=some-key", client.Table<User>().GenerateUrl());
        }

        [TestMethod("will set Basic Authorization")]
        public void TestHeadersBasicAuth()
        {
            var user = "user";
            var pass = "pass";
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(user, pass), null);
            var headers = client.Table<User>().PrepareRequestHeaders();
            var expected = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{pass}"));

            Assert.AreEqual($"Basic {expected}", headers["Authorization"]);
        }

        [TestMethod("filters: simple")]
        public void TestFiltersSimple()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));
            var dict = new Dictionary<Constants.Operator, string>
            {
                { Constants.Operator.Equals, "eq.bar" },
                { Constants.Operator.GreaterThan, "gt.bar" },
                { Constants.Operator.GreaterThanOrEqual, "gte.bar" },
                { Constants.Operator.LessThan, "lt.bar" },
                { Constants.Operator.LessThanOrEqual, "lte.bar" },
                { Constants.Operator.NotEqual, "neq.bar" },
                { Constants.Operator.Is, "is.bar" },
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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));
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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
                var config = new Range(2, 3);
                var filter = new QueryFilter("foo", pair.Key, config);
                var result = client.Table<User>().PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: not")]
        public void TestFiltersNot()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));
            var filter = new QueryFilter("foo", Constants.Operator.Equals, "bar");
            var notFilter = new QueryFilter(Constants.Operator.Not, filter);
            var result = client.Table<User>().PrepareFilter(notFilter);

            Assert.AreEqual("foo", result.Key);
            Assert.AreEqual("not.eq.bar", result.Value);
        }

        [TestMethod("filters: and & or")]
        public void TestFiltersAndOr()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));
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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var newUser = new User
            {
                Username = Guid.NewGuid().ToString(),
                AgeRange = new Range(18, 22),
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

        [TestMethod("Exceptions: Throws when inserting a user with same primary key value as an existing one without upsert option")]
        public async Task TestThrowsRequestExceptionInsertPkConflict()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var supaUpdated = new User
            {
                Username = "supabot",
                AgeRange = new Range(3, 8),
                Status = "OFFLINE",
                Catchphrase = "fat cat"
            };

            var insertOptions = new InsertOptions
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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var orderedResponse = await client.Table<User>().Order("catchphrase", Constants.Ordering.Descending).Get();
            var unorderedResponse = await client.Table<User>().Get();

            var supaOrderedUsers = orderedResponse.Models;
            var linqOrderedUsers = unorderedResponse.Models.OrderByDescending(u => u.Catchphrase).ToList();

            CollectionAssert.AreEqual(linqOrderedUsers, supaOrderedUsers);
        }

        [TestMethod("limit: basic")]
        public async Task TestLimit()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var limitedUsersResponse = await client.Table<User>().Limit(2).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaLimitUsers = limitedUsersResponse.Models;
            var linqLimitUsers = usersResponse.Models.Take(2).ToList();

            CollectionAssert.AreEqual(linqLimitUsers, supaLimitUsers);
        }

        [TestMethod("offset: basic")]
        public async Task TestOffset()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var offsetUsersResponse = await client.Table<User>().Offset(2).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaOffsetUsers = offsetUsersResponse.Models;
            var linqSkipUsers = usersResponse.Models.Skip(2).ToList();

            CollectionAssert.AreEqual(linqSkipUsers, supaOffsetUsers);
        }

        [TestMethod("range: from")]
        public async Task TestRangeFrom()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var rangeUsersResponse = await client.Table<User>().Range(2).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaRangeUsers = rangeUsersResponse.Models;
            var linqSkipUsers = usersResponse.Models.Skip(2).ToList();

            CollectionAssert.AreEqual(linqSkipUsers, supaRangeUsers);
        }

        [TestMethod("range: from and to")]
        public async Task TestRangeFromAndTo()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var rangeUsersResponse = await client.Table<User>().Range(1, 3).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaRangeUsers = rangeUsersResponse.Models;
            var linqRangeUsers = usersResponse.Models.Skip(1).Take(3).ToList();

            CollectionAssert.AreEqual(linqRangeUsers, supaRangeUsers);
        }

        [TestMethod("range: limit and offset")]
        public async Task TestRangeWithLimitAndOffset()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var rangeUsersResponse = await client.Table<User>().Limit(1).Offset(3).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaRangeUsers = rangeUsersResponse.Models;
            var linqRangeUsers = usersResponse.Models.Skip(3).Take(1).ToList();

            CollectionAssert.AreEqual(linqRangeUsers, supaRangeUsers);
        }

        [TestMethod("filters: not")]
        public async Task TestNotFilter()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));
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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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

        [TestMethod("filters: in")]
        public async Task TestInFilter()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var criteria = new List<object> { "supabot", "kiwicopple" };

            var filteredResponse = await client.Table<User>().Filter("username", Operator.In, criteria).Get();
            var usersResponse = await client.Table<User>().Get();

            var supaFilteredUsers = filteredResponse.Models;
            var linqFilteredUsers = usersResponse.Models.Where(u => u.Username == "supabot" || u.Username == "kiwicopple").ToList();

            CollectionAssert.AreEqual(linqFilteredUsers, supaFilteredUsers);
        }

        [TestMethod("filters: eq")]
        public async Task TestEqualsFilter()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.GreaterThanOrEqual, "1").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id >= 1).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: lt")]
        public async Task TestlessThanFilter()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.LessThan, "2").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id < 2).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: lte")]
        public async Task TestLessThanOrEqualFilter()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.LessThanOrEqual, "2").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id <= 2).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: nqe")]
        public async Task TestNotEqualFilter()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var filteredResponse = await client.Table<Message>().Filter("id", Operator.NotEqual, "2").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id != 2).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: like")]
        public async Task TestLikeFilter()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var filteredResponse = await client.Table<Message>().Filter("username", Operator.Like, "s%").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.UserName.StartsWith('s')).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("filters: ilike")]
        public async Task TestILikeFilter()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var filteredResponse = await client.Table<Message>().Filter("username", Operator.ILike, "%SUPA%").Get();
            var messagesResponse = await client.Table<Message>().Get();

            var supaFilteredMessages = filteredResponse.Models;
            var linqFilteredMessages = messagesResponse.Models.Where(m => m.UserName.Contains("SUPA",StringComparison.OrdinalIgnoreCase)).ToList();

            CollectionAssert.AreEqual(linqFilteredMessages, supaFilteredMessages);
        }

        [TestMethod("select: basic")]
        public async Task TestSelect()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

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
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));
            var rocketUser = new User
            {
                Username = "rocket",
                AgeRange = new Range(35, 40),
                Status = "ONLINE"
            };

            var aceUser = new User
            {
                Username = "ace",
                AgeRange = new Range(21, 28),
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
    }
}
