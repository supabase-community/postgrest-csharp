using System.Diagnostics;
using System.Threading.Tasks;
using Postgrest;
using PostgrestExample.Models;

namespace PostgrestExample
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var url = "http://localhost:3000";
            var client = Postgrest.Client.Initialize(url);

            // Get all Users
            var users = await client.Table<User>().Get();

            foreach (var user in users.Models)
            {
                Debug.WriteLine($"{user.Username} often says: {user.Catchphrase}");
            }

            // Get a single User
            var supabotUser = await client.Table<User>().Filter("username", Postgrest.Constants.Operator.Equals, "supabot").Single();

            if (supabotUser != null)
            {
                Debug.WriteLine($"{supabotUser.Username} was born on: {supabotUser.InsertedAt}");

                // Use username to Query another table
                var supabotMessages = await client.Table<Message>().Filter("username", Postgrest.Constants.Operator.Equals, supabotUser.Username).Get();

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
                AgeRange = new IntRange(20, 25),
                Catchphrase = "Gotta catch them all",
                Status = "ONLINE"
            };

            var exists = await client.Table<User>().Filter("username", Postgrest.Constants.Operator.Equals, "Ash Ketchum").Single();

            if (exists == null)
            {
                await client.Table<User>().Insert(newUser);
            }
            else
            {
                await exists.Delete<User>();
            }

            return 0;
        }
    }
}
