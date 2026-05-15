using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BCC.Models
{
    [Table("Rating")]
    public class Rating
    {
        public Rating()
        {
            this.Monthlies = new HashSet<Monthly>();
        }

        public int ID { get; set; }
        [MaxLength(50)]
        public string Rate { get; set; }
        public virtual ICollection<Monthly> Monthlies { get; set; }
    }
}
