using BCC.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BCC.Viewmodels
{
    public class SalonsVM
    {
        public IList<Salon> Salons { get; set; }
        public string Name { get; set; }
        public int Saldo { get; set; }
        public int Acctotal { get; set; }
        public int Comtotal { get; set; }
        public int Pointstotal { get; set; }
        public int Awardstotal { get; set; }
        public int PSSA { get; set; } = 0;
    }
}
