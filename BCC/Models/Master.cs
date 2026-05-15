using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BCC.Models;

[Table("Master")]
public class Master
{
    public Master()
    {
        this.Monthlies = new HashSet<Monthly>();
    }
    public int ID { get; set; }
    [MaxLength(50)]
    public string IdVault { get; set; }
    [MaxLength(50)]
    public string PSSA { get; set; }
    [MaxLength(50)]
    public string Lastname { get; set; }
    [MaxLength(50)]
    public string Firstname { get; set; }
    [MaxLength(50)]
    public string Name { get; set; }
    public DateOnly? Bday { get; set; }    
    public int RatingID { get; set; }
    public virtual Rating Rating { get; set; }
    [MaxLength(50)]
    public string Title { get; set; }
    [MaxLength(50)]
    public string Email { get; set; }
    [MaxLength(50)]
    public string Mobile { get; set; }
    [MaxLength(50)]
    public bool Active { get; set; }
    public bool Paid { get; set; }  
    public int? SalSaldo { get; set; }
    public virtual ICollection<Monthly> Monthlies { get; set; }
}
