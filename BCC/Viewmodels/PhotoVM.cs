using System;
using System.Collections.Generic;
using System.Text;

namespace BCC.Viewmodels
{
    public class PhotoVM
    {
        public int ID { get; set; }
        public string Date { get; set; }
        public string Category { get; set; }
        public string CategoryStarGroup { get; set; }
        public int? StarGroup { get; set; }
        public string ClubWinner { get; set; }
        public string Filename { get; set; }
        public string Fullname { get; set; }
        public string Photoname { get; set; }
        public string IdJava { get; set; }
        public int? Score { get; set; }
        public string Award { get; set; }
        public string Winner { get; set; }
        public bool flagShow { get; set; }
     

    }
}
