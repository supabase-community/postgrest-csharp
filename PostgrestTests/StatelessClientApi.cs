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
    public class StatelessClientApi
    {
        private static string baseUrl = "http://localhost:3000";

        [TestMethod("with optional query params")]
        public void TestQueryParams()
        {
            var options = new StatelessClientOptions(baseUrl)
            {
                QueryParams = new Dictionary<string, string>
                {
                    { "some-param", "foo" },
                    { "other-param", "bar" }
                }
            };

            Assert.AreEqual($"{baseUrl}/users?some-param=foo&other-param=bar", StatelessClient.Table<User>(options).GenerateUrl());
        }

        [TestMethod("will use TableAttribute")]
        public void TestTableAttribute()
        {
            var options = new StatelessClientOptions(baseUrl);
            Assert.AreEqual($"{baseUrl}/users", StatelessClient.Table<User>(options).GenerateUrl());
        }

        [TestMethod("will default to Class.name in absence of TableAttribute")]
        public void TestTableAttributeDefault()
        {
            var options = new StatelessClientOptions(baseUrl);
            Assert.AreEqual($"{baseUrl}/Stub", StatelessClient.Table<Stub>(options).GenerateUrl());
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
            var options = new StatelessClientOptions(baseUrl)
            {
                Headers =
                {
                    { "apikey", "some-key" }
                }
            };
            Assert.AreEqual($"{baseUrl}/users?apikey=some-key", StatelessClient.Table<User>(options).GenerateUrl());
        }

        [TestMethod("stored procedure")]
        public async Task TestStoredProcedure()
        {
            //Arrange 
            var options = new StatelessClientOptions(baseUrl);

            //Act 
            var parameters = new Dictionary<string, object>()
            {
                { "name_param", "supabot" }
            };
            var response = await StatelessClient.Rpc("get_status", parameters, options);

            //Assert 
            Assert.AreEqual(true, response.ResponseMessage.IsSuccessStatusCode);
            Assert.AreEqual(true, response.Content.Contains("OFFLINE"));
        }

        [TestMethod("switch schema")]
        public async Task TestSwitchSchema()
        {
            //Arrange
            var options = new StatelessClientOptions(baseUrl)
            {
                Schema = "personal"
            };

            //Act 
            var response = await StatelessClient.Table<User>(options).Filter("username", Operator.Equals, "leroyjenkins").Get();

            //Assert 
            Assert.AreEqual(1, response.Models.Count);
            Assert.AreEqual("leroyjenkins", response.Models.FirstOrDefault()?.Username);
        }
    }
}
