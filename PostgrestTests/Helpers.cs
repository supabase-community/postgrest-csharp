using System;
using System.Collections.Generic;
using Postgrest;

namespace PostgrestTests
{
	internal static class Helpers
	{
		internal static Client GetHostedClient()
		{
			var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
			var publicKey = Environment.GetEnvironmentVariable("SUPABASE_PUBLIC_KEY");

			var client = new Client($"{url}/rest/v1", new ClientOptions
			{
				Headers = new Dictionary<string, string>
				{
					{"apikey", publicKey! }
				}
			});

			return client;
		}
		
		internal static Client GetLocalClient()
		{
			var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
			var publicKey = Environment.GetEnvironmentVariable("SUPABASE_PUBLIC_KEY");

			var client = new Client($"{url}/rest/v1", new ClientOptions
			{
				Headers = new Dictionary<string, string>
				{
					{"apikey", publicKey! }
				}
			});

			return client;
		}
	}
}
