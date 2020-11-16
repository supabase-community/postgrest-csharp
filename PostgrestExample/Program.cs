using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PostgrestExample.Models;
using static Postgrest.ClientAuthorization;

namespace PostgrestExample
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var url = "http://localhost:3000";
            var auth = new Postgrest.ClientAuthorization(AuthorizationType.Open, null);

            var client = Postgrest.Client.Instance.Initialize(url, auth);

            // Get all Users
            var users = await client.Builder<User>().Get();

            foreach (var user in users.Models)
            {
                Debug.WriteLine($"{user.Username} often says: {user.Catchphrase}");
            }

            // Get a single User
            var supabotUser = await client.Builder<User>().Filter("username", Postgrest.Constants.Operator.Equals, "supabot").Single();

            if (supabotUser != null)
            {
                Debug.WriteLine($"{supabotUser.Username} was born on: {supabotUser.InsertedAt}");

                // Use username to Query another table
                var supabotMessages = await client.Builder<Message>().Filter("username", Postgrest.Constants.Operator.Equals, supabotUser.Username).Get();

                if (supabotMessages != null)
                {
                    Debug.WriteLine("and has said...");

                    foreach (var message in supabotMessages.Models)
                    {
                        Debug.WriteLine(message.MessageData);
                    }
                }
            }

            var newUser = new User
            {
                Username = "Ash Ketchum",
                AgeRange = new Range(20, 25),
                Catchphrase = "Gotta catch them all",
                Status = "ONLINE"
            };

            var exists = await client.Builder<User>().Filter("username", Postgrest.Constants.Operator.Equals, "Ash Ketchum").Single();

            if (exists == null)
            {
                await client.Builder<User>().Insert(newUser);
            }
            else
            {
                await exists.Delete<User>();
            }

            return 0;
        }
    }
}
