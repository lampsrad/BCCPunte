using BCC.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
namespace BCC;

public class Repo
{
    private readonly IDbContextFactory dbFactory;

    public Repo(IDbContextFactory _dbFactory)
    {
        dbFactory = _dbFactory;
    }

    public RepoScope CreateScope() => new RepoScope(dbFactory.CreateDbContext());

    public async Task<T> AddSaveAsync<T>(T entity) where T : class
    {
        using var cx = dbFactory.CreateDbContext();
        await cx.AddAsync(entity);
        await cx.SaveChangesAsync();
        return entity;
    }
    public async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> filter) where T : class
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        bool found = await cx.Set<T>().AnyAsync(filter);
        return found;
    }
    public async Task<Datum> Datum(Expression<Func<Datum, bool>> filter)
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        var dat = await cx.Datums.SingleOrDefaultAsync(filter);
        return dat;
    }
    public async Task DeleteSave(object entity)
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        cx.Remove(entity);
        cx.SaveChanges();
    }
    public async Task<DateOnly> lastDateAsync(Expression<Func<Datum, bool>> filter)
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        var dats = await cx.Datums.SingleOrDefaultAsync(filter);
        return dats.Date;
    }
    public async Task<IList<int>> monthliesYearsAsync()
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        IList<int> years;
        years = await cx.Monthlies.AsNoTracking().Select(x => x.Date.Year).Distinct().ToListAsync();
        years = years.OrderBy(x => x).ToList();
        return years;
    }
    public async Task<IList<Monthly>> monthliesLastAsyncNT(Expression<Func<Monthly, bool>> filter, DateOnly comparisonDate)
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        DateOnly clubdate = comparisonDate.toClubDate();
        var query = cx.Monthlies
        .AsNoTracking()
        .Include(m => m.Master)
        .Where(filter);
        var latestMonthlies = await query
            .Where(m => m.Date ==
                query.Where(x => x.MasterID == m.MasterID)
                     .Select(x => x.Date)
                     .Max())
            .ToListAsync();
        foreach (var m in latestMonthlies)
        {
            if (m.Date < comparisonDate)
            {
                m.VOm = m.Mm = m.Gm = m.Sm = m.Bm = m.Pm = m.Salm = null;
                if (m.Date < clubdate)
                    m.VOy = m.Saly = m.GMy = m.Py = null;
            }
        }
        return latestMonthlies;
    }
    public async Task<Monthly> MonthlyAsyncExcelLastAsyncNT(Expression<Func<Monthly, bool>> filter, DateOnly date)
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        DateOnly clubdate = date.toClubDate();
        var mons = await GetEntitiesNTAsync(filter, null);
        Monthly m = mons.LastOrDefault();
        if (m is null)
            return null;
        if (m.Date < date)
        {
            m.VOm = null;
            m.Mm = null;
            m.Gm = null;
            m.Sm = null;
            m.Bm = null;
            m.Pm = null;
            m.Salm = null;
            if (m.Date < clubdate)
            {
                m.VOy = null;
                m.Saly = null;
                m.GMy = null;
                m.Py = null;
            }
        }
        return m;
    }
    public async Task<Monthly> monthlyLastNTAsync(Expression<Func<Monthly, bool>> filter)
    {
        using var cx = dbFactory.CreateDbContext();
       var mon = await cx.Monthlies
        .AsNoTracking()
        .Include(x => x.Master)
        .Include(x => x.Rating)
        .Where(filter)
        .OrderByDescending(m => m.Date)
        .FirstOrDefaultAsync();
        return mon;
    }
    public async Task<DateOnly> monthlyLastDateAsync()
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        var lastDate = await cx.Monthlies
        .OrderByDescending(m => m.Date)
        .Select(m => m.Date)
        .FirstOrDefaultAsync();
        return lastDate;
    }
    public async Task<Monthly> monthlyPrevNTAsync(Monthly mon)
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        var mons = await cx.Monthlies.AsNoTracking().Include(x => x.Master).Include(x => x.Rating).Include(x => x.Photos).Include(x => x.Salons).Where(x => x.MasterID == mon.MasterID).OrderBy(x => x.Date).ToListAsync();
        var cur = mons.SingleOrDefault(x => x.ID == mon.ID);
        int idx = mons.IndexOf(cur);
        var prev = mons[idx - 1];
        return prev;
    }
    public async Task<int> UpdateSaveDetachAsync<T>(T entity) where T : class
    {
        await using var cx = dbFactory.CreateDbContext();
        cx.Set<T>().Update(entity);
        var cc = await cx.SaveChangesAsync();
        cx.ChangeTracker.Clear();
        return cc;
    }
    public async Task<T> GetEntityNTAsync<T>(Expression<Func<T, bool>> filter = null) where T : class
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        IQueryable<T> query = cx.Set<T>().AsNoTracking();
        query = BuildQuery(query);
        var ent = await query.SingleOrDefaultAsync(filter);
        return ent;
    }
    public async Task<IList<T>> GetEntitiesNTAsync<T>(Expression<Func<T, bool>> filter = null, Expression<Func<T, object>> sort = null) where T : class
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        IQueryable<T> query = cx.Set<T>().AsNoTracking();
        query = BuildQuery(query);
        if (filter != null)
        {
            query = query.Where(filter);
        }
        if (sort != null)
        {
            query = query.OrderBy(sort);
        }
        var list = await query.ToListAsync();
        return list;
    }
    public async Task<IEnumerable<T>> GetEntitiesIEnumNTAsync<T>(Expression<Func<T, bool>> filter = null, Expression<Func<T, object>> sort = null) where T : class
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        IQueryable<T> query = cx.Set<T>().AsNoTracking();
        query = BuildQuery(query);
        if (filter != null)
        {
            query = query.Where(filter);
        }
        if (sort != null)
        {
            query = query.OrderBy(sort);
        }
        var list = await query.ToListAsync();
        return list;
    }
    public async Task SqlBackupAsync(string fn)
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        Task T = Task.Run(() =>
        {
            cx.Database.ExecuteSqlInterpolated($"Use BCC Backup Database BCC to Disk = {fn} with init");
        });
        await T;
    }
    public async Task SqlRestoreAsync(string fn)
    {
        using var cx = await dbFactory.CreateDbContextAsync();
        string sql = $"USE master; ALTER DATABASE [{gData.dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [{gData.dbName}] FROM DISK = @filePath WITH REPLACE;";
        cx.Database.ExecuteSqlRaw(sql, new SqlParameter("@filePath", fn));
        var sqlMultiUser = $"USE master; ALTER DATABASE [{gData.dbName}] SET MULTI_USER;";
        cx.Database.ExecuteSqlRaw(sqlMultiUser);
    }
    internal static IQueryable<T> BuildQuery<T>(IQueryable<T> query) where T : class
    {
        if (typeof(T) == typeof(Master))
        {
            return (query as IQueryable<Master>)
                .Include(x => x.Rating)
                .OrderBy(x => x.Name)
                .Cast<T>();
        }
        if (typeof(T) == typeof(Monthly))
        {
            return (query as IQueryable<Monthly>)
                .Include(x => x.Master)
                .Include(x => x.Rating)
                .Include(x => x.Photos)
                .Include(x => x.Salons)
                .ThenInclude(x => x.SalonMaster)
                .OrderBy(x => x.Date)
                .Cast<T>();
        }
        if (typeof(T) == typeof(Photo))
        {
            return (query as IQueryable<Photo>)
                .Include(x => x.Monthly)
                .Include(x => x.Monthly.Master)
                .Cast<T>();
        }
        if (typeof(T) == typeof(Salon))
        {
            return (query as IQueryable<Salon>)
                .Include(x => x.Monthly)
                .Include(x => x.Monthly.Master)
                .Include(x => x.SalonMaster)
                .Cast<T>();
        }
        if (typeof(T) == typeof(SalonMaster))
        {
            return (query as IQueryable<SalonMaster>)
                .Include(x => x.Salons)
                .OrderBy(x => x.SalonName)
                .Cast<T>();
        }
        if (typeof(T) == typeof(HitCounter))
        {
            return (query as IQueryable<HitCounter>)
                .Cast<T>();
        }
        return query;
    }
    public async Task TESTUpdateAsync(IEnumerable<Photo> photos, bool? archived = null, bool? edited = null)
    {
        //using var cx = dbFactory.CreateDbContext();
        //var query = cx.Photos
        //    .Where(p => photos.Select(ph => ph.ID).Contains(p.ID));
        //await query.ExecuteUpdateAsync(setters =>
        //{
        //    var s = setters; // start with empty setter
        //    if (archived.HasValue)
        //        s = s.SetProperty(p => p.Winner, archived.Value);
        //    if (edited.HasValue)
        //        s = s.SetProperty(p => p.Club_Winner, edited.Value);
        //});
    }
}

public class RepoScope : IAsyncDisposable
{
    private readonly BKKEntities cx;
    internal RepoScope(BKKEntities cx) => this.cx = cx;
    public async Task AddAsync(object entity) => await cx.AddAsync(entity);
    public void AddRange(IList<Photo> range) => cx.AddRange(range);
    public void Delete(object entity) => cx.Remove(entity);
    public async Task<int> SaveChangesAsync() => await cx.SaveChangesAsync();
    public async Task<int> SaveChangesDetachAsync()
    {
        int n = await cx.SaveChangesAsync();
        cx.ChangeTracker.Clear();
        return n;
    }
    public async Task<IDbContextTransaction> BeginTransactionAsync()
        => await cx.Database.BeginTransactionAsync();
    public async Task<T> GetEntityAsync<T>(Expression<Func<T, bool>> filter = null) where T : class
    {
        IQueryable<T> query = cx.Set<T>();
        query = Repo.BuildQuery(query);
        return await query.SingleOrDefaultAsync(filter);
    }
    public async Task<IList<T>> GetEntitiesAsync<T>(Expression<Func<T, bool>> filter = null, Expression<Func<T, object>> sort = null) where T : class
    {
        IQueryable<T> query = cx.Set<T>();
        query = Repo.BuildQuery(query);
        if (filter != null) query = query.Where(filter);
        if (sort != null) query = query.OrderBy(sort);
        return await query.ToListAsync();
    }
    public async ValueTask DisposeAsync() => await cx.DisposeAsync();
}
