using System.Collections.Generic;
using JobNow.Models;

namespace JobNow.ViewModels
{
    public class HomeViewModel
    {
        public List<Employer> TopVietNamEmployers { get; set; } = new List<Employer>();
        public List<Employer> TopGlobalEmployers { get; set; } = new List<Employer>();
        public List<Industry> Industries { get; set; } = new List<Industry>();
        public List<JobLocation> Locations { get; set; } = new List<JobLocation>();
        public List<Article> Articles { get; set; } = new List<Article>();
    }
}