using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankAccountAPI.Models
{
    public class BankAccountResponse
    {
        public string Bank { get; set ; }
        public List<BankAccount> BankAccounts { get; set; }
        public List<Card> Cards { get; set; }
        public decimal CallDuration { get; set; }
    }
}
