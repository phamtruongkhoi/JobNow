using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    /// <summary>
    /// Model WorkHistory (Lịch sử kinh nghiệm làm việc của ứng viên), ánh xạ tới bảng jn_work_histories trên Supabase.
    /// Theo chuẩn Clean Architecture, Model chỉ chịu trách nhiệm định nghĩa cấu trúc dữ liệu và ánh xạ ORM.
    /// </summary>
    [Table("jn_work_histories")]
    public class WorkHistory : BaseModel
    {
        // Khóa chính tự động tăng trong bảng jn_work_histories
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        // Khóa ngoại liên kết tới Id của ứng viên trong bảng jn_profiles
        [Column("profile_id")]
        public string ProfileId { get; set; } = string.Empty;

        // Tên công ty / tổ chức nơi ứng viên từng làm việc
        [Column("company_name")]
        public string? CompanyName { get; set; }

        // Vị trí / Chức danh công việc tại công ty đó (VD: "Backend Developer")
        [Column("position")]
        public string? Position { get; set; }

        // Ngày bắt đầu làm việc
        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        // Ngày kết thúc làm việc (null nếu hiện tại vẫn đang làm việc tại đây)
        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        // Mô tả chi tiết công việc, trách nhiệm chính và các thành tựu đạt được
        [Column("description")]
        public string? Description { get; set; }

        // Thời điểm tạo bản ghi
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
