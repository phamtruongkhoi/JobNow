using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_jobs")]
    public class Job : BaseModel
    {
        [PrimaryKey("id", false)] public int Id { get; set; }
        [Column("title")] public string Title { get; set; }
        [Column("company_name")] public string CompanyName { get; set; }
        [Column("company_logo")] public string CompanyLogo { get; set; }
        [Column("salary")] public string Salary { get; set; }
        [Column("location")] public string Location { get; set; }
        [Column("posted_at")] public string PostedAt { get; set; }
        [Column("deadline")] public DateTime Deadline { get; set; }
        [Column("tags")] public string Tags { get; set; }
        [Column("is_hot")] public bool IsHot { get; set; }
        [Column("is_new")] public bool IsNew { get; set; }
        [Column("experience")] public string Experience { get; set; }
        [Column("job_type")] public string JobType { get; set; }
        // ... (các thuộc tính cũ giữ nguyên)
        [Column("quantity")] public string Quantity { get; set; }
        [Column("applied_count")] public int AppliedCount { get; set; }
        [Column("interviewing_count")] public int InterviewingCount { get; set; }
        [Column("is_urgent")] public bool IsUrgent { get; set; }
        [Column("description")] public string Description { get; set; }
        [Column("requirements")] public string Requirements { get; set; }
        [Column("benefits")] public string Benefits { get; set; }
        [Column("company_description")] public string CompanyDescription { get; set; }
        [Column("company_size")] public string CompanySize { get; set; }
        [Column("company_website")] public string CompanyWebsite { get; set; }
        [Column("company_address")] public string CompanyAddress { get; set; }
        
        [Column("employer_id")] public int? EmployerId { get; set; }
        [Column("status")] public string Status { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}