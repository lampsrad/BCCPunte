using System.ComponentModel.DataAnnotations.Schema;

namespace BCC.Models;

[Table("PClub")]
public class PClub
{
    public int ID { get; set; }
    public DateOnly? Date { get; set; }
    public string Name { get; set; }    
    public string SalonName { get; set; } 
}
