using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PostgrestTests.Models;
using Supabase.Postgrest;

namespace PostgrestTests
{
    [TestClass]
    public class AttachTests
    {
        private const string BaseUrl = "http://localhost:54321/rest/v1";

        [TestMethod(DisplayName = "Attached properties should have null properties by default")]
        public void GivenBareModel_Attach_PopulatesClientContext()
        {
            var model = new Movie { Name = "Realtime-hydrated Movie" };
            Assert.IsNull(model.BaseUrl);
            Assert.IsNull(model.RequestClientOptions);
            Assert.IsNull(model.GetHeaders);
        }

        [TestMethod(DisplayName = "Attach populates BaseUrl, RequestClientOptions, and GetHeaders")]
        public void Attach_ShouldPopulatesClientContext()
        {
            var options = new ClientOptions { Schema = "public" };
            var client = new Client(BaseUrl, options)
            {
                GetHeaders = () => new Dictionary<string, string> { { "Authorization", "Bearer test" } }
            };

            var result = client.Attach(new Movie { Name = "Realtime-hydrated Movie" });
            Assert.AreEqual(BaseUrl, result.BaseUrl);
            Assert.AreSame(options, result.RequestClientOptions);
            Assert.AreSame(client.GetHeaders, result.GetHeaders);
        }

        [TestMethod(DisplayName = "Attach should return the same instance for chaining")]
        public void Attach_ShouldReturnSameInstance()
        {
            var options = new ClientOptions { Schema = "public" };
            var client = new Client(BaseUrl, options)
            {
                GetHeaders = () => new Dictionary<string, string> { { "Authorization", "Bearer test" } }
            };

            var model = new Movie { Name = "Realtime-hydrated Movie" };
            var result = client.Attach(model);
            Assert.AreSame(model, result, "Attach should return the same instance for chaining.");
        }
    }
}
