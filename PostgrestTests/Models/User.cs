using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace PostgrestTests.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("username")]
        public string Username { get; set; }

        [Column("data")]
        public string Data { get; set; }

        [Column("age_range")]
        public Range AgeRange { get; set; }

        [Column("catchphrase")]
        public string Catchphrase { get; set; }

        public override bool Equals(object obj)
        {
            return obj is User user &&
                   Status == user.Status &&
                   Username == user.Username &&
                   AgeRange.Equals(user.AgeRange) &&
                   Catchphrase == user.Catchphrase;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Status, Username, AgeRange, Catchphrase);
        }
    }
}
