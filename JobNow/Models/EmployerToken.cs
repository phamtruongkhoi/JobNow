using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_employer_tokens")]
    public class EmployerToken : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("employer_id")]
        public int EmployerId { get; set; }

        [Column("balance")]
        public int Balance { get; set; }

        [Column("last_daily_claim")]
        public DateTime? LastDailyClaim { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
