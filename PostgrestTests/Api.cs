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

            Assert.AreEqual($"{baseUrl}/users?some-param=foo&other-param=bar", client.Builder<User>().GenerateUrl());
        }

        [TestMethod("will use TableAttribute")]
        public void TestTableAttribute()
        {
            var client = Client.Instance.Initialize(baseUrl, null);
            Assert.AreEqual($"{baseUrl}/users", client.Builder<User>().GenerateUrl());
        }

        [TestMethod("will default to Class.name in absence of TableAttribute")]
        public void TestTableAttributeDefault()
        {
            var client = Client.Instance.Initialize(baseUrl, null);
            Assert.AreEqual($"{baseUrl}/Stub", client.Builder<Stub>().GenerateUrl());
        }

        [TestMethod("will set Authorization header from token")]
        public void TestHeadersToken()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Token, "token"), null);
            var headers = client.Builder<User>().PrepareRequestHeaders();

            Assert.AreEqual("Bearer token", headers["Authorization"]);
        }

        [TestMethod("will set apikey query string")]
        public void TestQueryApiKey()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.ApiKey, "some-key"));
            Assert.AreEqual($"{baseUrl}/users?apikey=some-key", client.Builder<User>().GenerateUrl());
        }

        [TestMethod("will set Basic Authorization")]
        public void TestHeadersBasicAuth()
        {
            var user = "user";
            var pass = "pass";
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(user, pass), null);
            var headers = client.Builder<User>().PrepareRequestHeaders();
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
                var result = client.Builder<User>().PrepareFilter(filter);
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
                var result = client.Builder<User>().PrepareFilter(filter);
                Assert.AreEqual("foo", result.Key);
                Assert.AreEqual(pair.Value, result.Value);
            }
        }

        [TestMethod("filters: arrays with List<object> arguments")]
        public void TestFiltersArraysWithLists()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            // UrlEncoded {"bar","buzz"}
            string exp = "{\"bar\",\"buzz\"}";
            var dict = new Dictionary<Constants.Operator, string>
            {
                { Constants.Operator.In, $"in.{exp}" },
                { Constants.Operator.Contains, $"cs.{exp}" },
                { Constants.Operator.ContainedIn, $"cd.{exp}" },
                { Constants.Operator.Overlap, $"ov.{exp}" },
            };

            foreach (var pair in dict)
            {
                var list = new List<object> { "bar", "buzz" };
                var filter = new QueryFilter("foo", pair.Key, list);
                var result = client.Builder<User>().PrepareFilter(filter);
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
                var result = client.Builder<User>().PrepareFilter(filter);
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
                var result = client.Builder<User>().PrepareFilter(filter);
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
                var result = client.Builder<User>().PrepareFilter(filter);
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
            var result = client.Builder<User>().PrepareFilter(notFilter);

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
                var result = client.Builder<User>().PrepareFilter(filter);
                Assert.AreEqual(pair.Value, $"{result.Key}={result.Value}");
            }
        }

        [TestMethod("update: basic")]
        public async Task TestBasicUpdate()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Open, null));

            var user = await client.Builder<User>().Filter("username", Postgrest.Constants.Operator.Equals, "supabot").Single();

            if(user != null)
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
    }
}
