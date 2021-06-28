using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankAccountAPI.Models
{
    public class Card
    {
        public string Type { get; set; }
        public decimal PesosAmount { get; set; }
        public decimal DolarAmount { get; set; }
        public DateTime DueDate { get; set; }
    }
}
