using Postgrest.Attributes;
using Postgrest.Models;

namespace JobNow.Models
{
    [Table("jn_notifications")]
    public class Notification : BaseModel
    {
        [PrimaryKey("id", false)] public int Id { get; set; }
        [Column("title")] public string Title { get; set; }
        [Column("message")] public string Message { get; set; }
        [Column("created_at")] public string CreatedAt { get; set; }
        [Column("is_read")] public bool IsRead { get; set; }
        [Column("type")] public string Type { get; set; }
        [Column("action_link")] public string ActionLink { get; set; }
    }
}