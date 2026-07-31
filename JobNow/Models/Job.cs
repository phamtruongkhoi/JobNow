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
        [Column("posted_at")] public string PostedAt { get; set; } // Giữ lại dự phòng nếu cần
        [Column("deadline")] public DateTime Deadline { get; set; }
        [Column("tags")] public string Tags { get; set; }
        [Column("is_hot")] public bool IsHot { get; set; }
        [Column("is_new")] public bool IsNew { get; set; }
        [Column("experience")] public string Experience { get; set; }
        [Column("job_type")] public string JobType { get; set; }
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
        [Reference(typeof(Employer), ReferenceAttribute.JoinType.Left)] public Employer Employer { get; set; }
        [Column("status")] public string Status { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }

        // 1. THÊM CỘT NÀY ĐỂ HỨNG THỜI GIAN THỰC TỪ SUPABASE
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // 2. HÀM TÍNH TOÁN THỜI GIAN ĐỘNG
        public string GetTimeAgo()
        {
            // Nếu chưa có dữ liệu thời gian, trả về mặc định
            if (CreatedAt == DateTime.MinValue) return PostedAt ?? "Vừa xong";

            // Lấy giờ hiện tại trừ đi giờ đăng bài (Quy về chuẩn UTC để không bị lệch múi giờ)
            TimeSpan timeSince = DateTime.UtcNow - CreatedAt.ToUniversalTime();

            if (timeSince.TotalMinutes < 1) return "Vừa xong";
            if (timeSince.TotalHours < 1) return $"{(int)timeSince.TotalMinutes} phút trước";
            if (timeSince.TotalDays < 1) return $"{(int)timeSince.TotalHours} giờ trước";
            if (timeSince.TotalDays < 30) return $"{(int)timeSince.TotalDays} ngày trước";
            if (timeSince.TotalDays < 365) return $"{(int)(timeSince.TotalDays / 30)} tháng trước";

            return $"{(int)(timeSince.TotalDays / 365)} năm trước";
        }
    }
}