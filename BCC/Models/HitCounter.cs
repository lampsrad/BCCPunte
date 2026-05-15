using System.ComponentModel.DataAnnotations.Schema;

namespace BCC.Models
{
    [Table("HitCounter")]
    public class HitCounter
    {
        public HitCounter() { }
        public int ID { get; set; }
        public int? Counter { get; set; }
    }
}
