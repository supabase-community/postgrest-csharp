using System;
using Postgrest;
using Postgrest.Attributes;
using Postgrest.Models;

namespace PostgrestExample.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("username")]
        public string Username { get; set; } = null!;

        [Column("data")]
        public string Data { get; set; } = null!;

        [Column("age_range")]
        public IntRange AgeRange { get; set; } = null!;

        [Column("catchphrase")]
        public string Catchphrase { get; set; } = null!;

        [Column("status")]
        public string Status { get; set; } = null!;

        [Column("inserted_at")]
        public DateTime InsertedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
