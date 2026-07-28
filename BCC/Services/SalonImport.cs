using BCC;
using BCC.Models;
using BCC.Services;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;

namespace BKK.Services;

public class SalonImport
{
    private DataService Ds { get; set; }
    private Repo repo;
    private IList<string> Headers { get; set; }
    private IList<string> Messages { get; set; } = new List<string>();

    public SalonImport(DataService ds, Repo rep)
    {
        Ds = ds;
        repo = rep;
    }

    public async Task<IList<string>> ImportSalon(SalonMaster sm, string salonname)
    {
        await Ds.LastDates();
        string file = Directory.GetFiles(gData.ImportDirectory, $"*{salonname}*.csv").FirstOrDefault();

        var salons = new List<Salon>();
        using (var p = new TextFieldParser(file))
        {
            TextFieldParserInitialize(p);
            int clubIdx = Headers.IndexOf(SalonHeaders.ClubName);
            int fnIdx   = Headers.IndexOf(SalonHeaders.Firstname);
            int lnIdx   = Headers.IndexOf(SalonHeaders.Lastname);
            int awIdx   = Headers.IndexOf(SalonHeaders.AwardDescription);

            while (!p.EndOfData)
            {
                var data = p.ReadFields();
                if (!data[clubIdx].Contains(gData.ClubName))
                    continue;
                salons.Add(ProcessLineSalon(data, fnIdx, lnIdx, awIdx));
            }
        }

        if (salons.Count == 0)
        {
            Messages.Add($"No salon entries for {gData.ClubName} found in {file}");
            return Messages;
        }

        var smaster = await SalonMasterDo(sm.ID);
        await SalonUpdateSave(salons, smaster);
        await Ds.Promotion_Due();
        await Ds.CleanImportDirectory();
        Messages.Add($"Successfully imported {smaster.SalonName} with {salons.Count} salon records");
        return Messages;
    }

    private async Task MonthlyUpdate(Monthly monthlyNT)
    {
        Monthly last = await repo.GetEntityNTAsync<Monthly>(x => x.ID == monthlyNT.ID);
        Monthly prev = await repo.monthlyPrevNTAsync(last);
        last.Salm = last.Salons.Sum(x => x.Points);
        last.Salp = prev.Salp.AddNull(last.Salm);
        if (last.Date.Month == 11)
            last.Saly = last.Salm;
        else if (prev.Date > gData.lastDateClubYear)
            last.Saly = prev.Saly.AddNull(last.Salm);
        await repo.UpdateSaveDetachAsync(last);
    }

    private async Task<Salon> NewSalon(Monthly monthly, SalonMaster smaster)
    {
        var salon = new Salon { MonthlyID = monthly.ID, SalonMasterID = smaster.ID };
        await repo.UpdateSaveDetachAsync(salon);
        return await repo.GetEntityNTAsync<Salon>(x => x.ID == salon.ID);
    }

    private Salon ProcessLineSalon(string[] data, int fnIdx, int lnIdx, int awIdx)
    {
        var sal = new Salon
        {
            Firstname = data[fnIdx],
            Lastname  = data[lnIdx]
        };

        string awardVal = data[awIdx].ToLower();
        var med = Regex.Match(awardVal, "fiap[^\n]+|pssa[^\n]+|club[^\n]+");
        if (med.Success)
        {
            sal.Award = med.Value.toCap();
            sal.Com = 1;
        }
        if (Regex.IsMatch(awardVal, @"[cs]ert[^\n]+|com[^\n]+"))
            sal.Com = 1;
        if (Regex.IsMatch(awardVal, "acc[^\n]+"))
            sal.Acceptance = 1;

        return sal;
    }

    private async Task<(Salon salon, Monthly monthly)> SalonData(string lastname, string firstname, SalonMaster smaster)
    {
        lastname = lastname.ToLower() switch
        {
            "debeer" => "De Beer",
            _ => lastname
        };
        var master = await repo.GetEntityNTAsync<Master>(x => x.Lastname == lastname && x.Firstname == firstname);
        if (master == null)
            throw new Exception($"Master with name {lastname} {firstname} not found in DB");
        var monthly = await Ds.GetLastMonthly(master);
        var salon   = await repo.GetEntityNTAsync<Salon>(x => x.Monthly.MasterID == master.ID && x.SalonMasterID == smaster.ID);
        return (salon, monthly);
    }

    private async Task<SalonMaster> SalonMasterDo(int id)
    {
        var sm = await repo.GetEntityNTAsync<SalonMaster>(x => x.ID == id);
        sm.Imported = true;
        await repo.UpdateSaveDetachAsync(sm);
        return sm;
    }

    private async Task SalonUpdateSave(IList<Salon> salons, SalonMaster smaster)
    {
        foreach (var g in salons.GroupBy(s => new { s.Lastname, s.Firstname }))
        {
            var (salo, monthly) = await SalonData(g.Key.Lastname, g.Key.Firstname, smaster);
            if (salo == null)
            {
                var salon = await NewSalon(monthly, smaster);
                salon.Acceptance = g.Sum(x => x.Acceptance).AddNull(0);
                salon.Com        = g.Sum(x => x.Com).AddNull(0);
                salon.Award      = string.Join(" ", g.Where(x => x.Award != null).Select(x => x.Award));
                salon.Points     = salon.Acceptance.AddNull(2 * salon.Com);
                if (salon.SalonMaster.International)
                    salon.Points *= 2;
                await repo.UpdateSaveDetachAsync(salon);
            }
            await MonthlyUpdate(monthly);
        }
    }

    public TextFieldParser TextFieldParserInitialize(TextFieldParser p)
    {
        string headings = Regex.Replace(p.ReadLine(), @"\s+", "").ToLower();
        string delimiter = headings.Contains(';') ? ";" : ",";
        Headers = headings.Split(delimiter).ToList();
        p.TextFieldType = FieldType.Delimited;
        p.SetDelimiters(delimiter);
        p.TrimWhiteSpace = true;
        p.HasFieldsEnclosedInQuotes = true;
        return p;
    }
}
