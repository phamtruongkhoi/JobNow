using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_conversations")]
    public class Conversation : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("candidate_profile_id")]
        public string CandidateProfileId { get; set; }

        [Column("employer_profile_id")]
        public string EmployerProfileId { get; set; }

        [Column("job_id")]
        public int JobId { get; set; }

        [Column("job_title")]
        public string? JobTitle { get; set; }

        [Column("last_message")]
        public string? LastMessage { get; set; }

        [Column("last_message_at")]
        public DateTime? LastMessageAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
