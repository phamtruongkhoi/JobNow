using System;
using System.ComponentModel.DataAnnotations;

namespace JobNow.ViewModels
{
    public class CreateJobViewModel
    {
        public int Id { get; set; } // For Edit scenario

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề công việc.")]
        [Display(Name = "Tiêu đề công việc")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên công ty.")]
        [Display(Name = "Tên công ty")]
        public string CompanyName { get; set; }

        [Display(Name = "Ngành nghề")]
        public string Industry { get; set; }

        [Display(Name = "Loại hình công việc")]
        public string JobType { get; set; }

        [Display(Name = "Kinh nghiệm")]
        public string Experience { get; set; }

        [Display(Name = "Lương tối thiểu")]
        [RegularExpression(@"^\d*$", ErrorMessage = "Lương không được là số âm")]
        public string SalaryMin { get; set; }

        [Display(Name = "Lương tối đa")]
        [RegularExpression(@"^\d*$", ErrorMessage = "Lương không được là số âm")]
        public string SalaryMax { get; set; }

        [Display(Name = "Số lượng tuyển")]
        [RegularExpression(@"^\d*$", ErrorMessage = "Số lượng tuyển không hợp lệ")]
        public string Quantity { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hạn nộp hồ sơ.")]
        [Display(Name = "Hạn nộp hồ sơ")]
        public DateTime Deadline { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa điểm làm việc.")]
        [Display(Name = "Địa điểm làm việc")]
        public string Location { get; set; }

        [Display(Name = "Tags / Kỹ năng")]
        public string Tags { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả công việc.")]
        [Display(Name = "Mô tả công việc")]
        public string Description { get; set; }

        [Display(Name = "Yêu cầu ứng viên")]
        public string Requirements { get; set; }

        [Display(Name = "Quyền lợi")]
        public string Benefits { get; set; }

        public string Status { get; set; } // Draft or Published
    }
}
