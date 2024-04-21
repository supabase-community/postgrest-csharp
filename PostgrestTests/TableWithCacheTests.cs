using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Supabase.Postgrest;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Requests;
using PostgrestTests.Models;

namespace PostgrestTests
{
    /// <summary>
    /// A sample (dumb) caching implementation that saves to a file on the filesystem.
    /// </summary>
    public class TestCacheImplementation : IPostgrestCacheProvider
    {
        private readonly string _cacheDir = Path.GetTempPath();
        private string CacheFilePath => Path.Join(_cacheDir, $".postgrest-csharp.cache");

        public Task<T?> GetItem<T>(string key)
        {
            if (!Path.Exists(CacheFilePath))
                return Task.FromResult<T?>(default);

            var text = File.ReadAllText(CacheFilePath);

            if (string.IsNullOrEmpty(text))
                return Task.FromResult<T?>(default);

            return Task.FromResult(JsonConvert.DeserializeObject<T>(text));
        }

        public async Task SetItem(string key, object value)
        {
            if (Path.Exists(CacheFilePath))
                File.Delete(CacheFilePath);

            await using var handle = new StreamWriter(CacheFilePath);

            var serialized = JsonConvert.SerializeObject(value);
            await handle.WriteAsync(serialized);
            handle.Close();
        }

        public Task ClearItem(string key)
        {
            if (Path.Exists(CacheFilePath))
                File.Delete(CacheFilePath);

            return Task.CompletedTask;
        }

        public Task Empty()
        {
            if (Path.Exists(CacheFilePath))
                File.Delete(CacheFilePath);

            return Task.CompletedTask;
        }
    }

    [TestClass]
    public class TableWithCacheTests
    {
        private const string BaseUrl = "http://localhost:3000";

        [TestMethod("Table: Can construct with Caching Provider and raise events.")]
        public async Task TestCacheWorksWithGetRequests()
        {
            var tsc1 = new TaskCompletionSource<bool>();
            var tsc2 = new TaskCompletionSource<bool>();

            var client = new Client(BaseUrl);
            var cachingProvider = new TestCacheImplementation();
            await cachingProvider.Empty();

            // Attempting a request that should _not_ hit a cache.
            var initialReq = await client.Table<Movie>(cachingProvider).Get();
            
            Assert.IsTrue(initialReq.Models.Count == 0);

            initialReq.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != nameof(CacheBackedRequest<Movie>.WasResponseCached)) return;

                Assert.IsTrue(initialReq.WasResponseCached);
                tsc1.SetResult(true);
            };

            initialReq.RemoteModelsPopulated += sender =>
            {
                Assert.IsTrue(sender.WasResponseCached);
                tsc2.SetResult(true);
            };

            await Task.WhenAll(new[] { tsc1.Task, tsc2.Task });

            // Attempting a request that should hit a cache.

            var tsc3 = new TaskCompletionSource<bool>();
            var tsc4 = new TaskCompletionSource<bool>();
            var cachedResourceReq = await client.Table<Movie>(cachingProvider).Get();

            cachedResourceReq.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(CacheBackedRequest<Movie>.WasResponseCached)) return;

                Assert.IsTrue(cachedResourceReq.WasCacheHit);
                Assert.IsTrue(cachedResourceReq.WasResponseCached);
                Assert.IsNotNull(cachedResourceReq.Response);
                Assert.IsTrue(cachedResourceReq.Models.Count > 0);

                tsc3.SetResult(true);
            };

            cachedResourceReq.RemoteModelsPopulated += sender =>
            {
                Assert.IsTrue(sender.WasResponseCached);
                tsc4.SetResult(true);
            };

            await Task.WhenAll(new[] { tsc3.Task, tsc4.Task });
        }
    }
}