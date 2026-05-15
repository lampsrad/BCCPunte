using System.ComponentModel.DataAnnotations.Schema;

namespace BCC.Models;

[Table("SalonMaster")]
public class SalonMaster
{
    public SalonMaster() { Salons = new HashSet<Salon>(); }
    public int ID { get; set; }
    public string Club { get; set; }    
    public string SalonName { get; set; }
    public string Alias { get; set; }   
    public DateOnly Date { get; set; }
    public bool International { get; set; }
    public bool Imported { get; set; }
    public virtual ICollection<Salon> Salons { get; set; }
}