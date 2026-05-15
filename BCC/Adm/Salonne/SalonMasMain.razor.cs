using BCC.Models;
using BCC.Pages;
using BCC.Services;
using BCC.Viewmodels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;
using System.Reflection.PortableExecutable;

namespace BCC.Adm.Salonne;

[Authorize]
public partial class SalonMasMain
{
    [Inject] State state { get; set; }
    [Inject] Repo repo { get; set; }
    [Inject] NavigationManager Nav { get; set; }
    [Inject] DataService Ds { get; set; }
    [Parameter] public string Data { get; set; }
    SalonMaster Salonmaster { get; set; }
    Salon Salon { get; set; }
    IList<Master> Masters;
    IList<SalonMaster> salonMasters;
    IList<Salon> Salons { get; set; }
    private IList<string> Headers { get; set; }
    string Title;
    bool delete { get; set; }
    bool salonMasShow = true, createShow, editShow, salonsShow, salonCreateShow, salonEditShow;

    private void Back()
    {
        salonsShow = false;
        salonMasShow = true;
    }
    private void createSalonMaster()
    {
        salonMasShow = false;
        createShow = true;
    }
    private void Close(SalonMaster sm)
    {
        createShow = false;
        editShow = false;
        salonMasShow = true;
        if(sm != null)
        {
            var cursm = salonMasters.SingleOrDefault(x => x.ID == sm.ID);
            int idx = salonMasters.IndexOf(cursm);
            salonMasters[idx] = sm;
        }
    }
    private void CloseSMCreate(SalonMaster sm)
    {
        salonMasters.Add(sm);
        createShow = false;
        salonMasShow = true;
    }
    private async Task CreateSalon(int ID)
    {
        Salon = new Salon();
        Masters = await repo.GetEntitiesNTAsync<Master>(x => x.IdVault != null);
        salonsShow = false;
        salonCreateShow = true;
    }
    private async Task Delete(int ID)
    {
        await using var scope = repo.CreateScope();
        Salonmaster = await scope.GetEntityAsync<SalonMaster>(x => x.ID == ID);
        if (Salonmaster.Salons.Any())
        {
            await state.ShowMessageAsync("DELETE SALON", $"{Salonmaster.SalonName} contains Salons, Cannot Delete.", "ok");
            return;
        }
        scope.Delete(Salonmaster);
        await scope.SaveChangesAsync();
        var cur = salonMasters.SingleOrDefault(x => x.ID == ID);
        salonMasters.Remove(cur);
        delete = false;
        StateHasChanged();
    }
    private async Task Edit(int ID)
    {
        Salonmaster = await repo.GetEntityNTAsync<SalonMaster>(x => x.ID == ID);
        salonMasShow = false;
        editShow = true;
    }
    private async Task EditSalon(Salon sal)
    {
        Salon = await repo.GetEntityNTAsync<Salon>(x => x.ID == sal.ID);
        Salon.MasterID = sal.MasterID;
        salonsShow = false;
        salonEditShow = true;
    }
    private async Task ImportSalonList()
    {
        IList<SalonMaster> sms= new List<SalonMaster>();    
        salonMasShow = false;
        try
        {
            var filename = await state.ShowFilePicker();
            if (filename == null)
                return;
            var file = $"{gData.Downloads}{filename}";
            using (TextFieldParser p = new TextFieldParser(file))
            {
                TextFieldParserInitialize(p);
                while (!p.EndOfData)
                {
                    var ln = p.ReadFields();
                    var sm = new SalonMaster
                    {
                        Club = ln[1],
                        SalonName = ln[2],
                        Date = DateOnly.ParseExact(ln[3], "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    };
                    sms.Add(sm);
                }
            }
            await using var scope = repo.CreateScope();
            foreach (var sm in sms)
            {
                if (await repo.AnyAsync<SalonMaster>(x => x.Date == sm.Date) == false)
                {
                    await scope.AddAsync(sm);
                }
            }
            int cc = await scope.SaveChangesDetachAsync();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
    private async Task monthlyUpdate(Salon salon)
    {
        if (Salonmaster.Date < gData.lastDateClubImported)
        {
            await monthliesUpdate(salon);
            return;
        }
        await using var scope = repo.CreateScope();
        var mon = await scope.GetEntityAsync<Monthly>(x => x.ID == (int)salon.MonthlyID);
        var prev = await repo.monthlyPrevNTAsync(mon);
        mon.Salm = mon.Salons.Sum(x => x.Points).AddNull(0);
        mon.Salp = prev.Salp.AddNull(mon.Salm);
        if (mon.Date.Month == 11)
            mon.Saly = mon.Salm;
        else
            mon.Saly = prev.Saly.AddNull(mon.Salm);
        mon.Promotion = mon switch
        {
            { RatingID: 1, GMp: >= 4, Pp: >= 20 } => true,
            { RatingID: 2, GMp: >= 6, Pp: >= 35 } => true,
            { RatingID: 3, GMp: >= 8, Pp: >= 50, Salp: >= 18 } => true,
            { RatingID: 4, GMp: >= 12, Pp: >= 80, Salp: >= 50 } => true,
            { RatingID: 5, Pp: >= 150, Salp: >= 200, Title: "APSSA" } => true,
            { RatingID: 6, Pp: >= 150, Salp: >= 400, Title: "FPSSA" } => true,
            _ => null
        };
        await scope.SaveChangesAsync();
        if (mon.Promotion == true)
            await state.ShowMessageAsync("PROMOTION DUE", $"Promotion is due for {mon.Master.Name}", "ok");
    }
    private async Task monthliesUpdate(Salon salon)
    {
        await using var scope = repo.CreateScope();
        var mons = await scope.GetEntitiesAsync<Monthly>(x => x.Date >= Salonmaster.Date.AddMonths(1) && x.MasterID == salon.Monthly.MasterID);
        Monthly monthly = mons.FirstOrDefault()!;
        monthly.Salm = monthly.Salons.Sum(x => x.Points).AddNull(0);
        foreach (Monthly mon in mons)
        {
            mon.Salp = mon.Salp.AddNull(salon.Points);
            if (mon.Date.Year == salon.SalonMaster.Date.Year && mon.Date.Month < 11)
            {
                mon.Saly = mon.Saly.AddNull(salon.Points);
            }
        }
        await scope.SaveChangesDetachAsync();
    }
    private async Task salonShow(int ID)
    {
        Salonmaster = await repo.GetEntityNTAsync<SalonMaster>(x => x.ID == ID);
        Salons = await repo.GetEntitiesNTAsync<Salon>(x => x.SalonMasterID ==ID);
        Salons = Salons.OrderBy(x => x.Monthly.Master.Name).ToList();
        salonMasShow = false;
        salonsShow = true;
    }
    private async Task salonClose(Salon salon)
    {
        if (salon == null)
            return;
       Salon = salon;

        Salon.SalonMasterID = Salonmaster!.ID;
        Salon.Points = 0;
        if (Salonmaster.International != true)
        {
            Salon.Points = Salon.Points.AddNull(Salon.Acceptance);
            Salon.Points = Salon.Points.AddNull(Salon.Com * 2);
            Salon.Points = Salon.Points.AddNull(Salon.Judge);
        }
        else
        {
            Salon.Points = Salon.Points.AddNull(Salon.Acceptance * 2);
            Salon.Points = Salon.Points.AddNull(Salon.Com * 4);
            Salon.Points = Salon.Points.AddNull(Salon.Judge);
        }
        if (Salon.ID == 0)
        {
            var master = await repo.GetEntityNTAsync<Master>(x => x.ID == (int)Salon.MasterID);
            master.Active = true;
            await repo.UpdateSaveDetachAsync(master);
            Monthly monthly;
            if (Salonmaster.Date < gData.lastDateClubImported)
            {
                var mons = await repo.GetEntitiesNTAsync<Monthly>(x => x.Date >= Salonmaster.Date && x.MasterID == master.ID);
                monthly = mons.FirstOrDefault()!;
                if (monthly == null)
                    monthly = await Ds.GetLastMonthly(master);
            }
            else
                monthly = await Ds.GetLastMonthly(master);
            Salon.MonthlyID = monthly.ID;
        }
        else
            Salon.MasterID=Salonmaster.ID;
        await repo.UpdateSaveDetachAsync(Salon);
        await monthlyUpdate(Salon);
        Salons = await repo.GetEntitiesNTAsync<Salon>(x => x.SalonMasterID == Salonmaster.ID);
        Salons = Salons.OrderBy(x => x.SalonMaster.SalonName).ToList();
        salonCreateShow = false;
        salonEditShow = false;
        salonsShow = true;
        StateHasChanged();
    }
    protected override async Task OnParametersSetAsync()
    {
        if (Data == "importlist")
        {
            await ImportSalonList();
        }
        DateOnly datestart = DateOnly.Parse($"{gData.lastDateClubYear.Year}-07-01");
        DateOnly dateend = DateOnly.Parse($"{gData.lastDateClubYear.Year + 1}-06-30");
        if(Data=="prev")
        {
            datestart=datestart.AddYears(-1);
            dateend=dateend.AddYears(-1);
        }
        salonMasters = await repo.GetEntitiesNTAsync<SalonMaster>(x => x.Date >= datestart && x.Date < dateend,x=>x.Date);
        Title = $"Salons from {datestart} to {dateend}";
    }
    private void SortTableSalons(sortVM vm)
    {
        if (vm.colName == "Name")
        {
            if (vm.ascending)
                Salons = Salons!.OrderBy(x => x.Monthly.Master.Name).ToList();
            else
                Salons = Salons!.OrderByDescending(x => x.Monthly.Master.Name).ToList();
        }
        else
        {
            if (vm.ascending)
                Salons = Salons!.OrderBy(x => x.GetType().GetProperty(vm.colName)!.GetValue(x, null)).ToList();
            else
                Salons = Salons!.OrderByDescending(x => x.GetType().GetProperty(vm.colName)!.GetValue(x, null)).ToList();
        }
    }
    private void SortTableSalonMasters(sortVM vm)
    {
        if (vm.colName == "Name")
        {
            if (vm.ascending)
                salonMasters = salonMasters!.OrderBy(x => x.SalonName).ToList();
            else
                salonMasters = salonMasters!.OrderByDescending(x => x.SalonName).ToList();
        }
        else
        {
            if (vm.ascending)
                salonMasters = salonMasters!.OrderBy(x => x.GetType().GetProperty(vm.colName)!.GetValue(x, null)).ToList();
            else
                salonMasters = salonMasters!.OrderByDescending(x => x.GetType().GetProperty(vm.colName)!.GetValue(x, null)).ToList();
        }
    }
    public TextFieldParser TextFieldParserInitialize(TextFieldParser p)
    {
        string headings = p.ReadLine();//Headers
        headings = headings.ToLower();
        Headers = headings.Split(",").ToList();
        p.TextFieldType = FieldType.Delimited;
        p.SetDelimiters(",");
        p.TrimWhiteSpace = true;
        return p;
    }


}