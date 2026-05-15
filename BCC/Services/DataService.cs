using BCC.Models;
using Microsoft.AspNetCore.Authorization;
using System.IO.Compression;
using System.Text.RegularExpressions;


namespace BCC.Services;
[Authorize]
public class DataService
{
    private readonly Repo repo;

    public DataService(Repo _repo)
    {
        repo = _repo;
    }

    public async Task<string> CleanImportDirectory()
    {
        Task T = Task.Run(() =>
        {
            IList<string> files = Directory.GetFiles(gData.ImportDirectory);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        });
        await T;
        return "OK";
    }
    /// <summary>
    ///  Get latest Monthly on MasterID; If Monthly.date <= lastDateImported copy and allocate next month;s date else new monthly allocate next month's date
    /// </summary>
    /// <remarks>If the date of the retrieved <see cref="Monthly"/> is less than or equal to the last imported
    /// date, a copy of the latest <see cref="Monthly"/> is created with the next month's date.</remarks>
    /// <param name="master">The <see cref="Master"/> entity for which to retrieve the latest <see cref="Monthly"/> record.</param>
    /// <returns>The most recent <see cref="Monthly"/> record for the given <see cref="Master"/>. If no previous records exist, a
    /// new <see cref="Monthly"/> is created.</returns>
    public async Task<Monthly> GetLastMonthly(Master master)//NB!!!! get latest Monthly on MasterID; If date<=lastDateImported copy and allocate next month;s date else new monthly allocate next month's date
    {
        Monthly monthlyLast = await repo.monthlyLastNTAsync(x => x.MasterID == master.ID);//get the last Monthly on MasterID
        monthlyLast = monthlyLast ?? await MonthlyNew(master);//New member; No history of previous Monthly
        monthlyLast = (monthlyLast.Date <= gData.lastDateClubImported) ? await MonthlyCopy(master, monthlyLast) : monthlyLast;//If <= to lastDateImported then No Salons or Titles added => copy latest Monthly and allocate Next month; s date
        await repo.UpdateSaveDetachAsync(monthlyLast);
        return monthlyLast;
    }
    public async Task LastDates()
    {
        var dat = await repo.lastDateAsync(x => x.ID == "clubYearStart");
        gData.lastDateClubYear = dat.toDateFirstDay();
        dat = await repo.lastDateAsync(x => x.ID =="lastImport");
        gData.lastDateClubImported = dat.toDateFirstDay();
    }
    public async Task<Master> Master(int Id)
    {
        Master mas = await repo.GetEntityNTAsync<Master>(x => x.ID == Id);
        return mas;
    }
    public async Task<IList<Master>> Masters()
    {
        var masters = await repo.GetEntitiesNTAsync<Master>(x => x.IdVault != null);
        return masters;
    }
    /// <summary>
    /// New Monthly from Master; Date=gData.lastDateClubImported.Add one month
    /// Copy data from latest monthly to new monthly
    /// SaveDetach(new monthly)
    /// </summary>
    /// <param name="master"></param>
    /// <param name="monthlyLast"></param>
    /// <returns>MonthlyCopy</returns>
    private async Task<Monthly> MonthlyCopy(Master master, Monthly monthlyLast)
    {
        Monthly copy = await MonthlyNew(master);
        copy.Mg = monthlyLast.Mg;
        copy.Gg = monthlyLast.Gg;
        copy.Sg = monthlyLast.Sg;
        copy.Bg = monthlyLast.Bg;
        copy.GMp = monthlyLast.GMp;
        copy.Pp = monthlyLast.Pp;
        copy.Salp = monthlyLast.Salp;
        copy.GMy = monthlyLast.GMy;
        copy.Py = monthlyLast.Py;
        copy.VOy = monthlyLast.VOy;
        copy.Saly = monthlyLast.Saly;
        if (copy.Date.Month == 11 || monthlyLast.Date < gData.lastDateClubYear)
        {
            copy.GMy = null;
            copy.Py = null;
            copy.VOy = null;
            copy.Saly = null;
        }
        int cc = await repo.UpdateSaveDetachAsync(copy);
        return copy;
    }
/// <summary>
/// New Monthly from Master; Date=gData.lastDateClubImported.Add one month
/// </summary>
/// <param name="master"></param>
    private async Task<Monthly> MonthlyNew(Master master)
    {
        Monthly monthly = new Monthly();
        monthly.MasterID = master.ID;
        monthly.Date = gData.lastDateClubImported.AddMonths(1);
        monthly.RatingID = master.RatingID;
        monthly.Title = master.Title;
        await repo.UpdateSaveDetachAsync(monthly);
        return monthly;
    }
    public async Task Promotion_Due()
    {
        DateOnly lastdate = await repo.monthlyLastDateAsync();
        await using var scope = repo.CreateScope();
        IList<Monthly> monthlies = await scope.GetEntitiesAsync<Monthly>(x => x.Date.Year == lastdate.Year && x.Date.Month == lastdate.Month);
        foreach (Monthly m in monthlies)
        {
            if (m.Promotion == true)
                continue;
            m.Promotion = m switch
            {
                { RatingID: 1, GMp: >= 4, Pp: >= 20 } => true,
                { RatingID: 2, GMp: >= 6, Pp: >= 35 } => true,
                { RatingID: 3, GMp: >= 8, Pp: >= 50, Salp: >= 18 } => true,
                { RatingID: 4, GMp: >= 12, Pp: >= 80, Salp: >= 50 } => true,
                { RatingID: 5, Pp: >= 150, Salp: >= 200, Title: "APSSA" } => true,
                { RatingID: 6, Pp: >= 150, Salp: >= 400, Title: "FPSSA" } => true,
                _ => null
            };
        }
        await scope.SaveChangesDetachAsync();
        await Promote(lastdate);
    }
    private async Task Promote(DateOnly lastdate)
    {
        ratings rat;
        await using var scope = repo.CreateScope();
        IList<Monthly> promotes = await scope.GetEntitiesAsync<Monthly>(x => x.Promotion == true && x.Pp != x.Pm && (x.Date.Year == lastdate.Year && x.Date.Month == lastdate.Month));
        foreach (Monthly mo in promotes)
        {
            rat = (ratings)mo.RatingID!;
            rat += 1;
            mo.Master.RatingID = (int)rat;
            mo.RatingID = (int)rat;
            mo.GMp = null;
            mo.Pp = null;
            mo.Mg = null;
            mo.Gg = null;
            mo.Sg = null;
            mo.Bg = null;
        }
        int aa = await scope.SaveChangesDetachAsync();
    }
}
