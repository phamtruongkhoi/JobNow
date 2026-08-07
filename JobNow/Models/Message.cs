using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_messages")]
    public class Message : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("conversation_id")]
        public int ConversationId { get; set; }

        [Column("sender_profile_id")]
        public string SenderProfileId { get; set; }

        [Column("message")]
        public string Content { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
