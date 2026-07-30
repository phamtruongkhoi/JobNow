using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    /// <summary>
    /// Model UserCV (Quản lý hồ sơ CV đính kèm của ứng viên), ánh xạ tới bảng jn_user_cvs trên Supabase.
    /// Lưu trữ thông tin metadata của CV đã tải lên Supabase Storage.
    /// </summary>
    [Table("jn_user_cvs")]
    public class UserCV : BaseModel
    {
        // Khóa chính tự động tăng trong bảng jn_user_cvs
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        // Khóa ngoại liên kết tới Id của ứng viên trong bảng jn_profiles
        [Column("profile_id")]
        public string ProfileId { get; set; } = string.Empty;

        // Tên file CV gốc (VD: "CV_NguyenVanA_Backend_2026.pdf")
        [Column("file_name")]
        public string? FileName { get; set; }

        // Đường dẫn truy cập công khai hoặc có chữ ký tới file CV trên Supabase Storage bucket
        [Column("file_url")]
        public string? FileUrl { get; set; }

        // Kích thước file (tính theo byte, dùng long? để xử lý file dung lượng lớn)
        [Column("size")]
        public long? Size { get; set; }

        // Thời điểm tải lên CV (Mặc định là thời gian UTC hiện tại)
        [Column("upload_date")]
        public DateTime? UploadDate { get; set; } = DateTime.UtcNow;

        // Đánh dấu đây có phải là CV mặc định để sử dụng nhanh khi nộp đơn ứng tuyển (true: Mặc định)
        [Column("is_default")]
        public bool IsDefault { get; set; }
    }
}
