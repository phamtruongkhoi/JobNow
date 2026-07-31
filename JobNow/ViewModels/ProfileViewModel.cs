using System;
using System.Collections.Generic;
using JobNow.Models;

namespace JobNow.ViewModels
{
    /// <summary>
    /// ViewModel dùng cho Tab "Hồ sơ của tôi" & "Quản lý CV" (Trang cá nhân ứng viên).
    /// Tổng hợp dữ liệu từ bảng Profile, WorkHistory và UserCV theo chuẩn Clean Architecture / MVVM.
    /// </summary>
    public class ProfileViewModel
    {
        // Thông tin hồ sơ chính của ứng viên
        public Profile Profile { get; set; } = new Profile();

        // Danh sách lịch sử làm việc của ứng viên
        public List<WorkHistory> WorkHistories { get; set; } = new List<WorkHistory>();

        // Danh sách CV đã tải lên của ứng viên
        public List<UserCV> CVs { get; set; } = new List<UserCV>();

        // Danh sách việc làm đã lưu của ứng viên (kết hợp với bảng jn_jobs)
        public List<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();

        // Danh sách việc làm đã ứng tuyển
        public List<Application> Applications { get; set; } = new List<Application>();

        /// <summary>
        /// Kiểm tra hồ sơ có đang ở trạng thái trống (Empty State) hay không.
        /// </summary>
        public bool IsEmptyProfile =>
            string.IsNullOrWhiteSpace(Profile.Title) &&
            string.IsNullOrWhiteSpace(Profile.DesiredPosition) &&
            string.IsNullOrWhiteSpace(Profile.Skills) &&
            string.IsNullOrWhiteSpace(Profile.Introduction) &&
            WorkHistories.Count == 0 &&
            CVs.Count == 0;

        /// <summary>
        /// Tỷ lệ hoàn thiện hồ sơ (%) giúp ứng viên biết mức độ đầy đủ của hồ sơ.
        /// </summary>
        public int CompletionPercentage
        {
            get
            {
                int score = 0;
                if (!string.IsNullOrWhiteSpace(Profile.FullName)) score += 15;
                if (!string.IsNullOrWhiteSpace(Profile.Title)) score += 15;
                if (!string.IsNullOrWhiteSpace(Profile.Email) || !string.IsNullOrWhiteSpace(Profile.Phone)) score += 15;
                if (!string.IsNullOrWhiteSpace(Profile.AvatarUrl)) score += 15;
                if (!string.IsNullOrWhiteSpace(Profile.Skills)) score += 10;
                if (!string.IsNullOrWhiteSpace(Profile.Introduction)) score += 10;
                if (WorkHistories.Count > 0) score += 10;
                if (CVs.Count > 0) score += 10;
                return Math.Min(score, 100);
            }
        }
    }
}

