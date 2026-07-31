using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_applications")]
    public class Application : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("job_id")]
        public int JobId { get; set; }

        [Column("profile_id")]
        public string ProfileId { get; set; }

        [Column("cv_id")]
        public int? CvId { get; set; }

        [Column("cover_letter")]
        public string? CoverLetter { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        [Column("applied_at")]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Newtonsoft.Json.JsonIgnore]
        public Job? Job { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public UserCV? CV { get; set; }
        
        [Newtonsoft.Json.JsonIgnore]
        public Profile? Profile { get; set; }
    }
}
