using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("id", true)]
        public string? Id { get; set; }

        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("role")]
        public string? Role { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("title")]
        public string? Title { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("dob")]
        public DateTime? Dob { get; set; }

        [Column("experience_years")]
        public int? YearsOfExperience { get; set; }

        [Column("location")]
        public string? Location { get; set; }

        [Column("desired_position")]
        public string? DesiredPosition { get; set; }

        // ĐÃ SỬA: Đổi từ string thành double? để giải quyết dứt điểm lỗi CS1061 ở Index.cshtml
        [Column("desired_salary")]
        public double? ExpectedSalary { get; set; }

        [Column("work_status")]
        public string? WorkStatus { get; set; }

        [Column("skills")]
        public string? Skills { get; set; }

        [Column("bio")]
        public string? Introduction { get; set; }

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}