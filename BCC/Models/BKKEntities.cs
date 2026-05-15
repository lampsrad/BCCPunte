using Microsoft.EntityFrameworkCore;

namespace BCC.Models;

public class BKKEntities : DbContext
{
    public BKKEntities(DbContextOptions<BKKEntities> options): base(options){}

    public virtual DbSet<Master> Masters { get; set; }
    public virtual DbSet<Monthly> Monthlies { get; set; }
    public virtual DbSet<Photo> Photos { get; set; }
    public virtual DbSet<Rating> Ratings { get; set; }
    public virtual DbSet<Salon> Salons { get; set; }
    public virtual DbSet<SalonMaster> SalonMasters { get; set; }
    public virtual DbSet<HitCounter> HitCounters { get; set; }
    public virtual DbSet<Datum> Datums { get; set; }
    public virtual DbSet<PClub> Clubs { get; set; } 
}
