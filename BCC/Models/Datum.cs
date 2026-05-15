using System.ComponentModel.DataAnnotations.Schema;

namespace BCC.Models
{
    [Table("Date")]
    public class Datum
    {
        public string ID { get; set; }
        public DateOnly Date { get; set; }
    }
}
