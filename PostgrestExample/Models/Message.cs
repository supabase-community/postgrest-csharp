using Postgrest.Attributes;
using Postgrest.Models;

namespace PostgrestExample.Models
{
    [Table("messages")]
    public class Message : BaseModel
    {
        [Column("channel_id")]
        public int ChannelId { get; set; }

        [Column("message")]
        public string MessageData { get; set; }

        [Column("data")]
        public string Data { get; set; }

        [Column("username")]
        public string Username { get; set; }
    }
}
