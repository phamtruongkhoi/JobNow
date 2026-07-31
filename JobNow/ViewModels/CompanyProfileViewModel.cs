using System.ComponentModel.DataAnnotations;

namespace JobNow.ViewModels
{
    public class CompanyProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên công ty.")]
        [Display(Name = "Tên công ty")]
        public string Name { get; set; }

        [Display(Name = "Logo URL")]
        public string LogoUrl { get; set; }

        [Display(Name = "Website")]
        public string Website { get; set; }

        [Display(Name = "Email liên hệ")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Quy mô công ty")]
        public string CompanySize { get; set; }

        [Display(Name = "Mô tả công ty")]
        public string Description { get; set; }
    }
}
