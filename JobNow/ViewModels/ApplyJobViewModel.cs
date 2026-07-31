using System.Collections.Generic;
using JobNow.Models;

namespace JobNow.ViewModels
{
    public class ApplyJobViewModel
    {
        public Job Job { get; set; }
        public List<UserCV> AvailableCVs { get; set; } = new List<UserCV>();
        public int? SelectedCvId { get; set; }
        public string? CoverLetter { get; set; }
    }
}
