using JobNow.Models;
using System.Collections.Generic;

namespace JobNow.ViewModels
{
    public class EmployerDashboardViewModel
    {
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int ExpiredJobs { get; set; }
        public int TotalApplicants { get; set; }
        public bool NoEmployerProfile { get; set; }
        
        public int TokenBalance { get; set; }
        public int DailyFreeToken { get; set; }
        public int PostJobCost { get; set; }
        public string NextDailyClaim { get; set; }

        // Application Tracking
        public int ApplicationsReceived { get; set; }
        public int ApplicationsReviewing { get; set; }
        public int ApplicationsInterview { get; set; }
        public int ApplicationsAccepted { get; set; }
        public int ApplicationsRejected { get; set; }
    }
}
