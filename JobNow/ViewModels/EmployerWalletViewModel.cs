using JobNow.Models;
using System.Collections.Generic;

namespace JobNow.ViewModels
{
    public class EmployerWalletViewModel
    {
        public int TokenBalance { get; set; }
        public int DailyFreeToken { get; set; }
        public string NextDailyClaim { get; set; }
        public bool CanClaimDaily { get; set; }
        public List<TokenTransaction> Transactions { get; set; }
        public List<TokenPackage> Packages { get; set; }
    }
}
