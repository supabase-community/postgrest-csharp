using System;
using Postgrest.Attributes;

namespace PostgrestExample
{
    [Table("public.users")]
    public class UserModel
    {
        [PrimaryKey]
        [String]
        public string Username { get; set; }

        [Timestamp]
        public DateTime InsertedAt { get; set; }

        [Timestamp]
        public DateTime UpdatedAt { get; set; }

        [String]
        public string Data { get; set; }

        [String]
        public string Status { get; set; }

        [String]
        public string Catchphrase { get; set; }
    }
}
