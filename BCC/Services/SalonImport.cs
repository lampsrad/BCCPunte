using BCC;
using BCC.Models;
using BCC.Services;
using Microsoft.VisualBasic.FileIO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;

namespace BKK.Services;

public class SalonImport
{
    private DataService Ds { get; set; }

    private Repo repo;
    private string Message { get; set; } = string.Empty;
    private IList<string> Messages { get; set; } = new List<string>();
    private Master master { get; set; } = null;
    private Monthly monthly { get; set; } = null;
    private SalonMaster smaster { get; set; }
    private IList<string> Headers { get; set; }

    public SalonImport(DataService ds, Repo rep)
    {
        Ds = ds;
        repo = rep;
    }
    /// <summary>
    /// set gData.ImportDate=date; gDate.lastDateImported; gDatalastClubYear
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public async Task<IList<string>> ImportSalon(SalonMaster sm, string salonname)//IMPORT NEW SALONSHEET ENTERS HERE
    {
        IList<Salon> salons = new List<Salon>();
        await Ds.LastDates();//Gets from DB gData.lastDateImported ; gData.lastDateClubYear
        string file = Directory.GetFiles(gData.ImportDirectory, $"*{salonname}*.csv").FirstOrDefault();
        using (TextFieldParser p = new TextFieldParser(file))
        {
            TextFieldParserInitialize(p);
            int clubIdx = Headers.IndexOf(SalonHeaders.ClubName);
            while (!p.EndOfData)
            {
                var data = p.ReadFields();
                if (!data[clubIdx].Contains(gData.ClubName))
                    continue;
                Salon sal = ProcessLineSalon(data);
                salons.Add(sal);
            }
        }
        if(salons.Count == 0) {Messages.Add($"No Salons Entries for Bloemfontein Camera Club found in {file}"); return Messages; }
        smaster= await SalonMasterDo(sm.ID);
        await SalonUpdateSave(salons);
        await Ds.Promotion_Due();
        await Ds.CleanImportDirectory();
        Messages.Add($"Succesfully Imported {smaster.SalonName} with {salons.Count()} Salon Records");
        return Messages;
    }
    /// <summary>
    /// Performs the monthly update of salon points and aggregates them into monthly and yearly totals.
    /// </summary>
    /// <remarks>This method retrieves the current and previous monthly records, calculates the total points
    /// for the current month, and updates the monthly and yearly aggregates. The yearly total is reset in November or
    /// carried over based on the previous record's date.</remarks>
    /// <returns></returns>
    private async Task MonthlyUpdate()
    {
        Monthly last = await repo.GetEntityNTAsync<Monthly>(x => x.ID == monthly.ID);
        Monthly prev = await repo.monthlyPrevNTAsync(last);
        last.Salm = last.Salons.Sum(x => x.Points);
        last.Salp = prev.Salp.AddNull(last.Salm);
        if (last.Date.Month == 11)
            last.Saly = last.Salm;
        else if (prev.Date > gData.lastDateClubYear)
            last.Saly = prev.Saly.AddNull(last.Salm);
        int cc = await repo.UpdateSaveDetachAsync(last);
    }
    /// <summary>
    /// New salon => MonthlyID==monthly.ID; SalonMasterID==smaster.ID
    /// SaveChangesDetach(salon) - get salon - Update(salon) 
    /// </summary>
    /// <returns>salon</returns>
    private async Task<Salon> NewSalon()
    {
       var salon = new Salon();
        salon.MonthlyID = monthly.ID;
        salon.SalonMasterID = smaster.ID;
        await repo.UpdateSaveDetachAsync(salon);
        salon = await repo.GetEntityNTAsync<Salon>(x => x.ID == salon.ID);
        return salon;
    }
    /// <summary>
    /// New Salon; Populates Firstname - Lastname - AwardDescription
    /// </summary>
    /// <param name="data"></param>
    /// <returns>new Salon</returns>
    private Salon ProcessLineSalon(string[] data)
    {
        Salon sal = new Salon();
        var props = typeof(SalonHeaders).GetProperties();
        foreach (var p in props)
        {
            switch (p.Name)
            {
                case "Firstname":
                    sal.Firstname = ValueGet(data, p);
                    break;
                case "Lastname":
                    sal.Lastname = ValueGet(data, p);
                    break;
                case "AwardDescription":
                    string val = ValueGet(data, p).ToLower();
                    var med = Regex.Match(val, "fiap[^\n]+|pssa[^\n]+|club[^\n]+");
                    if (med.Success == true)
                    {
                        sal.Award = med.Value.toCap();
                        sal.Com = 1;
                    }
                    var com = Regex.Match(val, @"[cs]ert[^\n]+|com[^\n]+");
                    if (com.Success == true)
                        sal.Com = 1;
                    var one = Regex.Match(val, "acc[^\n]+");
                    if (one.Success == true)
                        sal.Acceptance = 1;
                    break;
            }
        }
        return sal;
        //foreach (string d in data)
        //{
        //    string val = d.ToLower().Trim();
        //    int idx = data.IndexOf(d);
        //    string Header = Headers.ElementAt(idx).ToLower();
        //    switch (Header)
        //    {
        //        case SalonHeaders.Firstname:
        //            sal.Firstname = d.Trim();
        //            break;
        //        case SalonHeaders.Lastname:
        //            sal.Lastname = d.Trim();
        //            if (sal.Lastname == "DeBeer") sal.Lastname = "De Beer";
        //            break;
        //        case SalonHeaders.AwardDescription:
        //        case "award id":
        //        case "award":
        //            var med = Regex.Match(val, "fiap[^\n]+|pssa[^\n]+|club[^\n]+");
        //            if (med.Success == true)
        //            {
        //                sal.Award = med.Value.toCap();
        //                sal.Com = 1;
        //            }
        //            var com = Regex.Match(val, @"[cs]ert[^\n]+|com[^\n]+");
        //            if (com.Success == true)
        //                sal.Com = 1;
        //            var one = Regex.Match(val, "acc[^\n]+");
        //            if (one.Success == true)
        //                sal.Acceptance = 1;
        //            break;
        //    }
        //}
        //return sal;
    }
    /// <summary>
    /// Gets master.Nt
    /// Gets monthly DataService Nt
    /// Gets salon
    /// </summary>
    /// <param name="Lastname"></param>
    /// <param name="Firtsname"></param>
    /// <returns>salon</returns>
    private async Task<Salon> SalonData(string Lastname, string Firtsname)
    {
        master = await repo.GetEntityNTAsync<Master>(x => x.Lastname == Lastname && x.Firstname == Firtsname);
        if (master == null) { throw new Exception($"Master with name {Lastname} {Firtsname} not found in DB"); };
        monthly = await Ds.GetLastMonthly(master);//No tracking
       var salon = await repo.GetEntityNTAsync<Salon>(x => x.Monthly.MasterID == master.ID && x.SalonMasterID == smaster.ID);
        return salon;
    }
    private async Task<SalonMaster> SalonMasterDo(int ID)
    {
        SalonMaster sm = await repo.GetEntityNTAsync<SalonMaster>(x => x.ID == ID);
        sm.Imported = true;
        await repo.UpdateSaveDetachAsync(sm);
        return sm;
    }
    /// <summary>
    /// SalonName from file; Check Db exit; not then new SalonMaster=> SalonName - Date-gData.ImportDate; AddSave new SalonMaster
    /// </summary>
    /// <param name="file"></param>
    /// <returns>new SalonMaster</returns>
/// <summary>
/// Updates and saves salon information by processing a list of salons and a salon master record.
/// </summary>
/// <remarks>This method groups the provided salons by their last and first names, checks for duplicates, and
/// updates the salon records with calculated acceptance, com, and points. It also updates monthly records after processing each group.
/// </remarks>
    private async Task SalonUpdateSave(IList<Salon> salons)
    {
    var q = from sal in salons group sal by new { sal.Lastname, sal.Firstname } into gp select new { gp.Key, gp };
        foreach (var g in q)
        {
           var salo = await SalonData(g.Key.Lastname, g.Key.Firstname);
            if (salo == null)// Duplicate salon not present
            {
               var salon = await NewSalon();
                salon.Acceptance = g.gp.Sum(x => x.Acceptance);
                salon.Acceptance = salon.Acceptance.AddNull(0);
                salon.Com = g.gp.Sum(x => x.Com);
                salon.Com = salon.Com.AddNull(0);
                IList<Salon> medals = g.gp.Where(x => x.Award != null).ToList();
                foreach (Salon sal in medals)
                {
                    salon.Award = salon.Award + sal.Award + " ";
                }
                if (salon.SalonMaster.International == false)
                {
                    salon.Points = salon.Acceptance.AddNull(2 * salon.Com);
                }
                else
                {
                    salon.Points = salon.Acceptance.AddNull(2 * salon.Com);
                    salon.Points = salon.Points * 2;
                }
                int cc = await repo.UpdateSaveDetachAsync(salon);
            }
            await MonthlyUpdate();
        }
    }
    /// <summary>
    /// Gets Headers to lower; set delimiters
    /// </summary>
    public TextFieldParser TextFieldParserInitialize(TextFieldParser p)
    {
        string headings = p.ReadLine();//Headers
        headings = Regex.Replace(headings, @"\s+", "");
        headings = headings.ToLower();
        bool m1 = Regex.IsMatch(headings, @";");
        string delimeter = m1 == true ? ";" : ",";
        Headers = headings.Split(delimeter).ToList();
        p.TextFieldType = FieldType.Delimited;
        p.SetDelimiters(delimeter);
        p.TrimWhiteSpace = true;
        p.HasFieldsEnclosedInQuotes = true;
        return p;
    }
    private string ValueGet(string[] data, PropertyInfo p)
    {
        string val = p.GetValue(this).ToString();
        int indx = Headers.IndexOf(val);
        return data[indx];
    }
}
