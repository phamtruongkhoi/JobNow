using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_token_settings")]
    public class TokenSettings : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("daily_free_token")]
        public int DailyFreeToken { get; set; }

        [Column("post_job_cost")]
        public int PostJobCost { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
