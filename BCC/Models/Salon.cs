using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace BCC.Models
{
    [Table("Salon")]
    public class Salon
    {
        public int ID { get; set; }
        public int? MonthlyID { get; set; }
        public virtual Monthly Monthly { get; set; }
        public int? SalonMasterID { get; set; }
        public virtual SalonMaster SalonMaster { get; set; }
        public int? Acceptance { get; set; }
        public int? Com { get; set; }
        public int? Points { get; set; }
        public int? Judge { get; set; }
        public string Award { get; set; }
        //public virtual ICollection<SalonPhoto> SalonPhotos { get; set; }
        [NotMapped]
        public int? MasterID { get; set; }
        [NotMapped] 
        public string Lastname { get; set; }
        [NotMapped]
        public string Firstname { get; set; }
    }
}
