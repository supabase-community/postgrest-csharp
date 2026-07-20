using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PostgrestTests.Models;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;

namespace PostgrestTests
{
    [TestClass]
    public class AttachEndToEndTests
    {
        private const string BaseUrl = "http://localhost:54321/rest/v1";

        [TestMethod(DisplayName = "Update() on an Attach()ed model does not throw the 'BaseUrl should be set' exception")]
        public async Task GivenAttachedModel_UpdateShouldNotThrowBaseUrlException()
        {
            var client = new Client(BaseUrl, new ClientOptions());
            var model = client.Attach(new Movie { Id = "11111111-1111-1111-1111-111111111111", Name = "Test" });

            try
            {
                await model.Update<Movie>();
            }
            catch (Exception ex)
            {
                Assert.IsFalse(ex is PostgrestException { Message: var m } && m.Contains("should be set in the model"),
                    $"Unexpectedly got the BaseUrl exception: {ex}");
            }
        }

        [TestMethod(DisplayName = "Delete() on an Attach()ed model does not throw the 'BaseUrl should be set' exception")]
        public async Task GivenAttachedModel_DeleteShouldNotThrowBaseUrlException()
        {
            var client = new Client(BaseUrl, new ClientOptions());
            var model = client.Attach(new Movie { Id = "11111111-1111-1111-1111-111111111111", Name = "Test" });

            try
            {
                await model.Delete<Movie>();
            }
            catch (Exception ex)
            {
                Assert.IsFalse(ex is PostgrestException { Message: var m } && m.Contains("should be set in the model"),
                    $"Unexpectedly got the BaseUrl exception: {ex}");
            }
        }
    }
}
