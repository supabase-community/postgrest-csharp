using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postgrest;
using PostgrestTests.Models;
using static Postgrest.ClientAuthorization;

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
            Assert.AreEqual(client.BaseUrl, baseUrl);
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

            Assert.AreEqual(client.Builder<User>().GenerateUrl(), $"{baseUrl}/users?some-param=foo&other-param=bar");
        }

        [TestMethod("will use TableAttribute")]
        public void TestTableAttribute()
        {
            var client = Client.Instance.Initialize(baseUrl, null);
            Assert.AreEqual(client.Builder<User>().GenerateUrl(), $"{baseUrl}/users");
        }

        [TestMethod("will default to Class.name in absence of TableAttribute")]
        public void TestTableAttributeDefault()
        {
            var client = Client.Instance.Initialize(baseUrl, null);
            Assert.AreEqual(client.Builder<Stub>().GenerateUrl(), $"{baseUrl}/Stub");
        }

        [TestMethod("will set Authorization header from token")]
        public void TestHeadersToken()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.Token, "token"), null);
            var headers = client.Builder<User>().PrepareRequestHeaders();

            Assert.AreEqual(headers["Authorization"], "Bearer token");
        }

        [TestMethod("will set apikey query string")]
        public void TestQueryApiKey()
        {
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(AuthorizationType.ApiKey, "some-key"));
            Assert.AreEqual(client.Builder<User>().GenerateUrl(), $"{baseUrl}/users?apikey=some-key");
        }

        [TestMethod("will set Basic Authorization")]
        public void TestHeadersBasicAuth()
        {
            var user = "user";
            var pass = "pass";
            var client = Client.Instance.Initialize(baseUrl, new ClientAuthorization(user, pass), null);
            var headers = client.Builder<User>().PrepareRequestHeaders();
            var expected = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{pass}"));

            Assert.AreEqual(headers["Authorization"], $"Basic {expected}");
        }

        // TODO: Flesh out remaining tests
    }
}
