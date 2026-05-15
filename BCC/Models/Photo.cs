using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCC.Models
{
    [Table("Photo")]
    public class Photo
    {
        public Photo()
        {
        }
        public int ID { get; set; }
        public int? MonthlyID { get; set; }
        public virtual Monthly Monthly { get; set; }
        public int Club_Rating { get; set; }
        public int? Star_Group { get; set; }
        [MaxLength(50)]
        public string Category { get; set; }
        [MaxLength(50)]
        public string Title { get; set; }
        public int? IntRef { get; set; }
        public int? Score { get; set; }
        [MaxLength(50)]
        public string Award { get; set; }
        public bool? Winner { get; set; }
        public bool? Club_Winner { get; set; }
        [NotMapped]
        public string Email { get; set; }
        [NotMapped]
        public string PVID { get; set; }  
        [NotMapped]
        public string Filename { get; set; }
        [NotMapped]
        public string Date { get; set; }
        [NotMapped]
        public string Name { get; set; }
        [NotMapped]
        public string Honours {  get; set; }
        [NotMapped]
        public bool Flag { get; set; }
        [NotMapped]
        public string IdJava { get; set; }
    }
}
