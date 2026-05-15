using System;

namespace BCC.Models
{
    public struct Csv
    {
        public string idVault { get; set; }
        public string Pssa { get; set; }
        public string Lastname { get; set; }
        public string Firstname { get; set; }
        public int ClubRating { get; set; }
        public string Honours { get; set; }
        public string Email { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Filename { get; set; }
        public Int32 Score { get; set; }
        public string Award { get; set; }
        public bool Winner { get; set; }
        public bool Club_Winner { get; set; }
    }
}
