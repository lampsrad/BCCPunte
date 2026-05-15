using System;
using System.Collections.Generic;
using System.Text;

namespace BCC.Viewmodels
{
   public class wallVM
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }
        public int Star { get; set; }
        public int? Py { get; set; }
        public int?VOy { get; set; }
        public int? Saly { get; set; }
        public int? grandMaster { get; set; }//py+voy+saly
        public int? clubMaster { get; set; }//py+voy
    }
}
