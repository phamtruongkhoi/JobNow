using System;
using System.ComponentModel.DataAnnotations;

namespace JobNow.ViewModels
{
    public class CreateCourseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề khóa học không được để trống")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Tên trung tâm/trường không được để trống")]
        public string ProviderName { get; set; }

        public string ProviderLogo { get; set; }

        [Required(ErrorMessage = "Học phí không được để trống")]
        public string Price { get; set; }

        [Required(ErrorMessage = "Địa điểm không được để trống")]
        public string Location { get; set; }

        public DateTime Deadline { get; set; }

        public string Tags { get; set; }

        [Required(ErrorMessage = "Thời lượng không được để trống")]
        public string Duration { get; set; }

        [Required(ErrorMessage = "Hình thức không được để trống")]
        public string Format { get; set; }
    }
}
