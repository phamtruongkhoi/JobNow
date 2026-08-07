using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_token_transactions")]
    public class TokenTransaction : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("employer_id")]
        public int EmployerId { get; set; }

        [Column("package_id")]
        public int? PackageId { get; set; }

        [Column("transaction_type")]
        public string TransactionType { get; set; } // DailyReward, Purchase, Consume

        [Column("token_amount")]
        public int TokenAmount { get; set; }

        [Column("money_amount")]
        public decimal? MoneyAmount { get; set; }

        [Column("status")]
        public string Status { get; set; } // Pending, Completed, Rejected

        [Column("description")]
        public string Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
