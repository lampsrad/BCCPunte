using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCC.Models
{
    [Table("Monthly")]
    public class Monthly
    {
        public Monthly()
        {
            this.Photos = new HashSet<Photo>();
        }

        public int ID { get; set; }
        public int? MasterID { get; set; }
        public virtual Master Master { get; set; }
        public DateOnly Date { get; set; }
        public int? VOm { get; set; }
        public int? Mm { get; set; }
        public int? Gm { get; set; }
        public int? Sm { get; set; }
        public int? Bm { get; set; }
        public int? Pm { get; set; }
        public int? Salm { get; set; }
        public int? Mg { get; set; }
        public int? Gg { get; set; }
        public int? Sg { get; set; }
        public int? Bg { get; set; }
        public int? GMp { get; set; }
        public int? Pp { get; set; }
        public int? Salp { get; set; }
        public int? GMy { get; set; }
        public int? Py { get; set; }
        public int? VOy { get; set; }
        public int? Saly { get; set; }
        public bool? Promotion { get; set; }
        public int RatingID { get; set; }
        public virtual Rating Rating { get; set; }
        [MaxLength(50)]
        public string Title { get; set; }
        [NotMapped]
        public string PromoString { get; set; }
        [NotMapped]
        public int Position { get; set; }
        public virtual ICollection<Photo> Photos { get; set; }
        public virtual ICollection<Salon> Salons { get; set; }
    }
}
