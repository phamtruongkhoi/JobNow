using JobNow.Models;
using System.Collections.Generic;

namespace JobNow.ViewModels
{
    public class AdminTokenTransactionsViewModel
    {
        public List<TokenTransactionItem> Transactions { get; set; }
        public string Filter { get; set; } // Pending, Completed, Rejected
    }

    public class TokenTransactionItem
    {
        public TokenTransaction Transaction { get; set; }
        public Employer Employer { get; set; }
        public TokenPackage Package { get; set; }
    }
}
