using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    /// <summary>
    /// Model SavedJob (Việc làm đã lưu của ứng viên), ánh xạ tới bảng jn_saved_jobs trên Supabase.
    /// Quản lý danh sách các công việc mà ứng viên đã thêm vào danh sách yêu thích / lưu lại để ứng tuyển sau.
    /// </summary>
    [Table("jn_saved_jobs")]
    public class SavedJob : BaseModel
    {
        // Khóa chính tự động tăng trong bảng jn_saved_jobs
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        // Khóa ngoại liên kết tới Id của ứng viên trong bảng jn_profiles
        [Column("profile_id")]
        public string ProfileId { get; set; } = string.Empty;

        // Khóa ngoại liên kết tới Id của công việc trong bảng jn_jobs
        [Column("job_id")]
        public int JobId { get; set; }

        // Thời điểm lưu công việc
        [Column("saved_at")]
        public DateTime? SavedAt { get; set; } = DateTime.UtcNow;

        // Thuộc tính tham chiếu (Reference) tới Model Job để tiện sử dụng khi truy vấn lồng / nối bảng (JOIN)
        [Reference(typeof(Job))]
        public Job? Job { get; set; }
    }
}
