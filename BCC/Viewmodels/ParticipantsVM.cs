using System;
using System.Collections.Generic;
using System.Text;

namespace BCC.Viewmodels
{
   public  class ParticipantsVM
    {
        public int ID { get; set; }
        public string CategoryStarGroup { get; set; }
        public string Fullname { get; set; }
        public int? Score { get; set; }
        public string Award { get; set; }
        public string Winner { get; set; }
        public string ClubWinner { get; set; }
        public string Choose { get; set; }
        public bool flagSelect { get; set; }
        public bool flagFullname { get; set; }
        public bool flagCategory { get; set; }
        public bool flagScore { get; set; }
        public bool flagAward { get; set; }
        public bool flagCatWin { get; set; }
        public bool flagClubWin { get; set; }
    }
}
