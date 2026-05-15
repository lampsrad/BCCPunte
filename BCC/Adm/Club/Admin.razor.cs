using BCC.Models;
using BCC.Pages;
using BCC.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualBasic.FileIO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BCC.Pages.FileUploadAbs;


namespace BCC.Adm.Club;

public partial class Admin
{
    [Inject] Repo repo { get; set; }
    [Inject] State state { get; set; }
    [Inject] DataService ds { get; set; }
    [Inject] IConfiguration config { get; set; }
    [Parameter] public string Data { get; set; }
    private ProgressBar progressBar { get; set; }
    private ProgressInfo Info = new ProgressInfo();
    private List<string> Errors { get; set; } = new();
    private IList<string> Messages { get; set; } = new List<string>();
    string Title { get; set; }
    bool filePick { get; set; }
    private IList<string> Headers { get; set; }
    private string status { get; set; }
    private string Filename { get; set; }
    private string Category { get; set; } = null;

    private string Backup()
    {
        Title = "Backup";
        filePick = true;
        return "OK";
    }
    private async Task CsvWrite(IList<Monthly> ms, DateOnly dat)
    {
        string ln = $"Date,Name,Grade,VOm,Mm,Gm,Sm,Bm,Pm,Salm,Mg,Gg,Sg,Bg,GMp,Pp,Salp,GMy,Py,VOy,Saly";
        string dire = $"D:\\BKK\\ARCHIVES\\Ou punte rekords\\{dat.Year}\\";
        string fn = $"{dat}.csv";
        string dest = $"{dire}{fn}";
        if (!Directory.Exists(dire))
            Directory.CreateDirectory(dire);
        if (File.Exists(dest))
            return;
        await using FileStream fs = new FileStream(dest, FileMode.Create, FileAccess.Write);
        await using StreamWriter sw = new StreamWriter(fs);
        await sw.WriteLineAsync(ln);//Header
        foreach (Monthly m in ms)
        {
            ln = $"{m.Date},{m.Master.Name},{m.RatingID},{m.VOm},{m.Mm},{m.Gm},{m.Sm},{m.Bm},{m.Pm},{m.Salm},{m.Mg},{m.Gg},{m.Sg},{m.Bg},{m.GMp},{m.Pp},{m.Salp},{m.GMy},{m.Py},{m.VOy},{m.Saly}";
            await sw.WriteLineAsync(ln);
        }
    }
    private async Task<string> DeleteMonth()
    {
        var lastdat = gData.lastDateClubImported;
        var cutoffDate = lastdat.AddMonths(-1);
        await using var scope = repo.CreateScope();
        using var transaction = await scope.BeginTransactionAsync();
        try
        {
            var mons = await scope.GetEntitiesAsync<Monthly>(x => x.Date > cutoffDate);
            var monthlyIDs = mons.Select(x => x.ID).ToList();
            var phots = await scope.GetEntitiesAsync<Photo>(x => monthlyIDs.Contains((int)x.MonthlyID));
            foreach (var photo in phots)
            {
                scope.Delete(photo);
            }
            var cc = await scope.SaveChangesDetachAsync();
            foreach (Monthly m in mons)
            {
                scope.Delete(m);
            }
            cc = await scope.SaveChangesDetachAsync();

            var salonmasters = await scope.GetEntitiesAsync<SalonMaster>(x => x.Date > cutoffDate);
            var salonMasterIds = salonmasters.Select(sm => sm.ID).ToList();
            var salons = await scope.GetEntitiesAsync<Salon>(x => salonMasterIds.Contains((int)x.SalonMasterID));
            foreach (var sal in salons)
            {
                scope.Delete(sal);
            }
            await scope.SaveChangesDetachAsync();
            foreach (var sm in salonmasters)
            {
                scope.Delete(sm);
            }
            await scope.SaveChangesDetachAsync();
            await DatesStoreInDb();
            lastdat = gData.lastDateClubImported;
            mons = await scope.GetEntitiesAsync<Monthly>(x => x.Date >= lastdat);
            foreach (var m in mons)
            {
                m.Master.RatingID = m.RatingID;
            }
            cc = await scope.SaveChangesDetachAsync();
            await transaction.CommitAsync();
            return "Month data deleted successfully.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return $"Error deleting month data: {ex.Message}";
        }
    }
    private void DirectoryPrepare(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        var files = Directory.GetFiles(directory, "*");
        foreach (var file in files)
        {
            File.Delete(file);
        }
    }
    private async void ecbBackup(string file)
    {
        string fn = Path.Combine(gData.backupPath, file);
        if (File.Exists(fn))
            File.Delete(fn);
        try
        {
            await repo.SqlBackupAsync(fn);
            Messages.Add("Success in Backingup DB");
        }
        catch (Exception ex)
        {
            Errors.Clear();
            Errors.Add(ex.Message);
            Errors.Add(ex.StackTrace);
        }
        filePick = false;
        StateHasChanged();
    }
    private async void ecbRestore(string file)
    {
        string fn = Path.Combine(gData.backupPath, file);
        filePick = false;
        try
        {
            await repo.SqlRestoreAsync(fn);
            Messages.Add("Success in Restoring DB");
        }
        catch (Exception ex)
        {
            Errors.Clear();
            Errors.Add($"{ex.Message}");
            Errors.Add(ex.StackTrace);
        }
        StateHasChanged();
    }
    private async Task<string> Excel()
    {
        DateOnly datstart = DateOnly.Parse("2013-11-01");
        DateOnly datend = DateOnly.FromDateTime(DateTime.Now);
        int months = (datend.Year - datstart.Year) * 12 + (datend.Month - datstart.Month) + 1;
        IList<Master> masters = await repo.GetEntitiesNTAsync<Master>(null);
        var allMonthlys = await repo.GetEntitiesNTAsync<Monthly>(x => x.Date >= datstart && x.Date <= datend);
        // Group and sort by MasterID for efficient cumulative access
        var monthlyByMaster = allMonthlys
            .GroupBy(m => m.MasterID)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Date).ToList());
        // Track current index per Master for O(1) amortized access to latest entry as dates progress
        var currentIndices = masters.ToDictionary(m => m.ID, _ => -1);
        state.ShowProgress("EXCEL", true, 0);
        int count = 0;
        DateOnly dat = datstart;
        while (dat <= datend)
        {
            IList<Monthly> ms = new List<Monthly>();
            foreach (Master ma in masters)
            {
                if (monthlyByMaster.TryGetValue(ma.ID, out var monthsForMaster) && monthsForMaster.Count > 0)
                {
                    int idx = currentIndices[ma.ID];
                    // Advance index only as needed (amortized O(1) per iteration)
                    while (idx < monthsForMaster.Count - 1 && monthsForMaster[idx + 1].Date <= dat)
                    {
                        idx++;
                    }
                    currentIndices[ma.ID] = idx;
                    if (idx >= 0)
                    {
                        ms.Add(monthsForMaster[idx]);
                    }
                }
            }
            await CsvWrite(ms, dat);
            ms.Clear();
            dat = dat.AddMonths(1);
            int progressValue = Interlocked.Increment(ref count);
            double prog = progressValue * 100 / months;
            state.UpdateProgress(prog);
        }
        state.Hide();
        return "OK";
    }
    protected override void OnInitialized()
    {
        DirectoryPrepare(gData.ImportDirectory);
    }
    protected override async Task OnParametersSetAsync()
    {
        Messages = new List<string>();
        Title = "Admin Page";
        StateHasChanged();
        string com = Data switch
        {
            "backup" => Backup(),
            "images-import" => await ImagesProcessAsync(),
            "club-import" => await ImportClub(),
            "restore" => Restore(),
            "excel" => await Excel(),
            "deletemonth" => await DeleteMonth(),
            _ => null
        };
        StateHasChanged();
    }
    private string Restore()
    {
        string source = $"{gData.Downloads}bcc.bak";
        string dest = $"{gData.backupPath}bcc.bak";
        if (File.Exists(source))
            File.Move(source, dest, true);
        Title = "Restore";
        filePick = true;
        return "OK";
    }
    private TextFieldParser TextFieldParserInitialize(TextFieldParser p)
    {
        string headings = p.ReadLine();//Headers
        headings = headings.ToLower();
        Headers = headings.Split(",").ToList();
        p.TextFieldType = FieldType.Delimited;
        p.SetDelimiters(",");
        p.TrimWhiteSpace = true;
        return p;
    }

    #region ClubImport
    private async Task<Master> AddNewMaster(Photo phot)
    {
        Master master = new Master();
        var m = Regex.Match(phot.Name, @"^(.*)\s+(\S+)$");
        master.Lastname = m.Groups[1].Value.Trim();
        master.Firstname = m.Groups[2].Value.Trim();
        master.Name = phot.Name;
        master.RatingID = phot.Club_Rating;
        master.Title = phot.Honours;
        master.Active = true;
        master.IdVault = phot.PVID != null ? phot.PVID : master.IdVault;
        master.Email = phot.Email != null ? phot.Email : master.Email;
        master.Title = phot.Honours;
        await repo.AddSaveAsync(master);
        return master;
    }
    public async Task DatesStoreInDb()
    {
        Datum lastImport = await repo.Datum(x => x.ID == "lastImport");
        var latestdate = await repo.monthlyLastDateAsync();//Date of Monhly with max ID 
        if (latestdate.Month == 9)
            latestdate = latestdate.AddMonths(1);
        lastImport.Date = latestdate;
        gData.lastDateClubImported = lastImport.Date;
        if (lastImport.Date.Month == 11)
        {
            Datum newyear = await repo.Datum(x => x.ID == "clubYearStart");
            newyear.Date = lastImport.Date;
            gData.lastDateClubYear = newyear.Date;
        }
        await repo.UpdateSaveDetachAsync(lastImport);
    }
    public async Task<Master> GetMasterRow(Photo phot)
    {
        Master master = null;
        if (phot.PVID == null)
            master = await repo.GetEntityNTAsync<Master>(x => x.Name == phot.Name);
        else
            master = await repo.GetEntityNTAsync<Master>(x => x.IdVault == phot.PVID);
        if (master == null)
        {
            var resp = await state.ShowConfirmAsync($"{phot.Name}", "Add New Master??");
            if (resp == true)
                master = await AddNewMaster(phot);
        }
        return master;
    }
    private async Task<bool> HeadersCheckScores(TextFieldParser p, string fn = null)
    {
        if (fn.StartsWith("Scoresheet", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var pr in typeof(ScoreHeaders).GetProperties())
            {
                string val = pr.GetValue(this).ToString();
                if (Headers.Any(x => x == val) == false)
                    throw new Exception("Headers of Import File is Incorrect");
            }
        }
        if (fn.StartsWith("Results", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var pr in typeof(ResultHeaders).GetProperties())
            {
                string val = pr.GetValue(this).ToString();
                if (Headers.Any(x => x == val) == false)
                    throw new Exception("Headers of Import File is Incorrect");
            }
        }
        string ln = p.PeekChars(400);
        ln = ln.Replace("\"", string.Empty).Replace(",", ";");
        var m1 = Regex.Match(ln, @";(\d{7});");
        string intref = m1.Groups[1].Value;
        int? ir = int.Parse(intref);
        bool imported = await repo.AnyAsync<Photo>(x => x.IntRef == ir && x.Monthly.Date.Year == gData.lastDateClubImported.Year);
        return imported;
    }
    private async Task<string> Import(string fn)
    {
        await ds.LastDates();//Gets from DB gData.lastDateImported ; gData.lastDateClubYear
        string file = Directory.GetFiles(gData.ImportDirectory, $"*{fn}*").FirstOrDefault();
        if (file == null)
            throw new Exception($"File is not a {fn} File");
        return file;
    }
    private async Task<string> ImportClub()//ENTRY POINT FOR IMPORT CLUB
    {
        IList<string> lst = new List<string>();
        try
        {
            if (gData.lastDateClubImported.Month == 9)
            {
                await YearEnd();
                return null;
            }
            string fn = await state.ShowFileUpload("CLUB IMPORT", "Upload Score or Results", gData.ImportDirectory);
            if (fn == null)
                return null;
            if (fn.StartsWith("Scoresheet", StringComparison.OrdinalIgnoreCase))
                lst = await ImportScores(fn);
            else if (fn.StartsWith("Results", StringComparison.OrdinalIgnoreCase))
                lst = await ImportResults(fn);
            else
                throw new Exception("File Name is not correct; It should start with Scoresheet or Results");
            return string.Join("", lst);
        }
        catch (Exception ex)
        {
            Errors.Clear();
            Errors.Add(ex.Message);
            Errors.Add(ex.StackTrace);
            return null;
        }
    }
    public async Task<IList<string>> ImportScores(string filename)//IMPORT SCORESHEET
    {
        IList<Photo> photos = new List<Photo>();
        string file = await Import(filename);
        using (TextFieldParser p = new TextFieldParser(file))
        {
            TextFieldParserInitialize(p);
            bool imported = await HeadersCheckScores(p, Path.GetFileName(file));
            if (imported == true) // Already Imported; updatescores from Resultssheet
            {
                await UpdateScores(file);
                return Messages;
            }
            while (!p.EndOfData)
            {
                Photo photo = processLineScores(p.ReadFields());
                photos.Add(photo);
            }
        }
        var gpvid = photos.GroupBy(x => x.PVID);
        foreach (var key in gpvid)
        {
            Master master = await GetMasterRow(key.FirstOrDefault());
            Monthly monthly = await ds.GetLastMonthly(master);
            foreach (var p in key)
            {
                p.MonthlyID = monthly.ID;
            }
        }
        photos = PhotoQuantity(photos);//Correct Awards; Remove duplicates for Winners; Only 5 photos entered
        await using var scope = repo.CreateScope();
        scope.AddRange(photos);//Adds new Photos to Db
        int count = await scope.SaveChangesDetachAsync();
        await MonthlyUpdate();
        await MonthlyCompute();
        await ds.Promotion_Due();
        await DatesStoreInDb();
        await ds.CleanImportDirectory();
        Messages.Add($"Succesfully Imported {count} Records");
        return Messages;
    }
    private async Task<IList<string>> ImportResults(string filename)//IMPORT RESULTSHEET
    {
        IList<Photo> photos = new List<Photo>();
        string file = await Import(filename);
        using (TextFieldParser p = new TextFieldParser(file))
        {
            TextFieldParserInitialize(p);
            bool imported = await HeadersCheckScores(p, Path.GetFileName(file));
            if (imported == true)
            {
                //await UpdateScores(file);
                Messages.Add("Already Imported Scoresheet into DB");
                return Messages;
            }
            while (!p.EndOfData)
            {
                Photo photo = processLineResults(p.ReadFields(), photos);
                photos.Add(photo);
            }
        }
        var ig = photos.GroupBy(x => x.IntRef).Where(g => g.Count() > 1).ToList();
        foreach (var g in ig)
        {
            var phots = g.ToList();
            var phot = phots.FirstOrDefault(x => x.Award?.Length == 1);
            phot?.Club_Winner = phots.Any(x => x.Award == "CW");
            phot?.Winner = phots.Any(x => x.Award != "CW" && x.Award.Length > 1);
        }
        photos = photos.Where(x => x.Award.Length == 1).ToList();
        var gname = photos.GroupBy(x => x.Name);
        foreach (var key in gname)
        {
            Master master = await GetMasterRow(key.FirstOrDefault());
            Monthly monthly = await ds.GetLastMonthly(master);
            foreach (var p in key)
            {
                p.MonthlyID = monthly.ID;
            }
        }
        photos = PhotoQuantity(photos);//Correct Awards; Remove duplicates for Winners; Only 5 photos entered
        await using var scope = repo.CreateScope();
        scope.AddRange(photos);//Adds new Photos to Db
        int count = await scope.SaveChangesDetachAsync();
        await MonthlyUpdate();
        await MonthlyCompute();
        await ds.Promotion_Due();
        await DatesStoreInDb();
        await ds.CleanImportDirectory();
        return Messages;
    }
    private async Task MonthlyCompute()
    {
        DateOnly date = await repo.monthlyLastDateAsync();//Date of Monhly with max ID
        await using var scope = repo.CreateScope();
        IList<Monthly> monthlies = await scope.GetEntitiesAsync<Monthly>(x => x.Date.Year == date.Year && x.Date.Month == date.Month);
        foreach (Monthly m in monthlies)
        {
            int? pts = null;
            pts = m.Mm * 5;
            if (pts != null)
            {
                m.Pm = m.Pm.AddNull(pts);
                m.Mg = m.Mg.AddNull(m.Mm);
            }
            pts = m.Gm * 3;
            if (pts != null)
            {
                m.Pm = m.Pm.AddNull(pts);
                m.Gg = m.Gg.AddNull(m.Gm);
            }
            pts = m.Sm * 2;
            if (pts != null)
            {
                m.Pm = m.Pm.AddNull(pts);
                m.Sg = m.Sg.AddNull(m.Sm);
            }
            pts = m.Bm * 1;
            if (pts != null)
            {
                m.Pm = m.Pm.AddNull(pts);
                m.Bg = m.Bg.AddNull(m.Bm);
            }
            m.GMp = m.Mg.AddNull(m.Gg);
            m.Pp = m.Pp.AddNull(m.Pm);
            m.GMy = (m.GMy.AddNull(m.Mm)).AddNull(m.Gm);
            m.Py = m.Py.AddNull(m.Pm);
            m.VOy = m.VOy.AddNull(m.VOm);
        }
        int aa = await scope.SaveChangesDetachAsync();
    }
    private async Task MonthlyUpdate()
    {
        DateOnly date = await repo.monthlyLastDateAsync();//Last Monthly Date
        await using var scope = repo.CreateScope();
        IList<Photo> photos = await scope.GetEntitiesAsync<Photo>(x => x.Monthly.Date >= date);
        foreach (Photo p in photos)
        {
            if (p.Category != "S" && p.Category != "PH")
            {
                switch (p.Award)
                {
                    case "C":
                        p.Monthly.Mm = p.Monthly.Mm.AddNull(1);
                        break;
                    case "G":
                        p.Monthly.Gm = p.Monthly.Gm.AddNull(1);
                        break;
                    case "S":
                        p.Monthly.Sm = p.Monthly.Sm.AddNull(1);
                        break;
                    case "B":
                        p.Monthly.Bm = p.Monthly.Bm.AddNull(1);
                        break;
                }
            }
            if (p.Category == "S")
            {
                switch (p.Award)
                {
                    case "C":
                        p.Monthly.VOm = p.Monthly.VOm.AddNull(5);
                        break;
                    case "G":
                        p.Monthly.VOm = p.Monthly.VOm.AddNull(3);
                        break;
                    case "S":
                        p.Monthly.VOm = p.Monthly.VOm.AddNull(2);
                        break;
                    case "B":
                        p.Monthly.VOm = p.Monthly.VOm.AddNull(1);
                        break;
                }
            }
        }
        int aa = await scope.SaveChangesDetachAsync();
    }
    private async Task<string> OctoberMonth()
    {
        await ds.LastDates();//Gets from DB gData.lastDateImported ; gData.lastDateClubYear
        await ds.Promotion_Due();//Promotions for latest month(Promote for salons entered September)
        await DatesStoreInDb();
        return "October month with no Imports, but Promotions succesfully done";
    }
    private IList<Photo> PhotoQuantity(IList<Photo> Photos)
    {
        int count = 0;
        var monthlies = Photos.GroupBy(x => x.MonthlyID);
        foreach (var m in monthlies)
        {
            var photos = m.AsEnumerable().ToList();
            var phots = photos.Where(x => x.Category != "S").OrderBy(x => x.Category).ToList();
            count = phots.Count();
            if (count > 3)
            {
                for (int i = 0; i < count - 3; i++)
                {
                    var phot = phots.LastOrDefault();
                    phots.Remove(phot);
                    Photos.Remove(phot);
                }
            }
            phots = photos.Where(x => x.Category == "S").ToList();
            count = phots.Count();
            if (count > 2)
            {
                for (int i = 0; i < count - 2; i++)
                {
                    var phot = phots.LastOrDefault();
                    phots.Remove(phot);
                    Photos.Remove(phot!);
                }
            }
        }
        var almas = Photos.Where(x => x.Name == "Erasmus Alma").ToList();
        return Photos;
    }
    private Photo processLineResults(string[] data, IList<Photo> photos)
    {
        string d = string.Empty;
        string firstname = string.Empty, lastname = string.Empty;
        var props = typeof(ResultHeaders).GetProperties();
        Photo phot = new Photo();
        foreach (var p in props)
        {
            switch (p.Name)
            {
                case "CategoryName":
                    string catname = ValueGet(data, p);
                    Category = catname[0].ToString();
                    break;
                case "Lastname":
                    lastname = ValueGet(data, p);
                    break;
                case "Firstname":
                    firstname = ValueGet(data, p);
                    break;
                case "ClubStarRating":
                    d = ValueGet(data, p);
                    phot.Club_Rating = d switch
                    {
                        "Golden Honours" => 6,
                        "Galaxy" => 7,
                        "5" => 5,
                        "4" => 4,
                        "3" => 3,
                        "2" => 2,
                        "1" or "" => 1,
                        _ => 0
                    };
                    phot.Star_Group = phot.Club_Rating switch
                    {
                        1 or 2 => 1,
                        3 => 2,
                        4 or 5 => 3,
                        6 or 7 => 4,
                        _ => 1
                    };
                    break;
                case "PhotoTitle":
                    phot.Title = ValueGet(data, p);
                    break;
                case "EventPhotoID":
                    d = ValueGet(data, p);
                    phot.IntRef = int.Parse(d);
                    break;
                case "Honours":
                    phot.Honours = ValueGet(data, p);
                    break;
                case "AwardID":
                    d = ValueGet(data, p).ToLower();
                    phot.Award = d switch
                    {
                        "com" => "C",
                        _ => d
                    };
                    phot.Award = phot.Award.ToUpper();
                    break;
            }
        }
        lastname = lastname == "DeBeer" ? "De Beer" : lastname;
        phot.Name = $"{lastname} {firstname}";
        phot.Category = Category == "S" ? Category : $"{Category}{phot.Star_Group}";
        Category = null;
        return phot;
    }
    private Photo processLineScores(string[] data)
    {

        Photo phot = new Photo();
        string d = string.Empty;
        string firstname = string.Empty, lastname = string.Empty;
        var props = typeof(ScoreHeaders).GetProperties();
        foreach (var p in props)
        {
            switch (p.Name)
            {
                case "Category":
                    phot.Category = ValueGet(data, p);
                    break;
                case "MemberId":
                    phot.PVID = ValueGet(data, p);
                    break;
                case "Lastname":
                    lastname = ValueGet(data, p);
                    break;
                case "Firstname":
                    firstname = ValueGet(data, p);
                    break;
                case "ClubStarRating":
                    d = ValueGet(data, p);
                    phot.Club_Rating = d switch
                    {
                        "Golden Honours" => 6,
                        "Galaxy" => 7,
                        _ => int.Parse(d)
                    };
                    phot.Star_Group = phot.Club_Rating switch
                    {
                        1 or 2 => 1,
                        3 => 2,
                        4 or 5 => 3,
                        6 or 7 => 4,
                        _ => 1
                    };
                    break;
                case "Title":
                    phot.Title = ValueGet(data, p);
                    break;
                case "InternalReference":
                    d = ValueGet(data, p);
                    phot.IntRef = int.Parse(d);
                    break;
                case "Honours":
                    phot.Honours = ValueGet(data, p);
                    break;
                case "Email":
                    phot.Email = ValueGet(data, p);
                    break;
                case "ScoreTotal":
                    phot.Score = int.Parse(ValueGet(data, p));
                    break;
                case "Awards":
                    d = ValueGet(data, p);
                    if (Regex.IsMatch(d, @"~\w+~"))
                    {
                        phot.Winner = true;
                        phot.Club_Winner = true;
                    }
                    else
                        if (Regex.IsMatch(d, @"~"))
                            phot.Winner = true;
                    phot.Award = Regex.Match(d, @"^\w").ToString();
                    break;
            }
        }
        phot.Name = $"{lastname} {firstname}";
        phot.Category = phot.Category == "S" ? phot.Category : $"{phot.Category}{phot.Star_Group}";
        return phot;
    }
    private Photo processLineScoresUpdate(string[] data)
    {
        Photo phot = new Photo();
        string d = string.Empty;
        var props = typeof(ScoreHeaders).GetProperties();
        var fnp = props.FirstOrDefault(x => x.Name == "InternalReference");
        phot.IntRef = int.Parse(ValueGet(data, fnp));
        var st = props.FirstOrDefault(x => x.Name == "ScoreTotal");
        phot.Score = int.Parse(ValueGet(data, st));
        return phot;
    }
    private async Task UpdateScores(string file)
    {
        await using var scope = repo.CreateScope();
        var phots = await scope.GetEntitiesAsync<Photo>(x => x.Monthly.Date >= gData.lastDateClubImported);
        Photo first = phots.FirstOrDefault();
        if (first.Score > 0)
            throw new Exception("Scores already Present; No Update is neccesary!!", null) { HResult = 1 };
        using (TextFieldParser p = new TextFieldParser(file))
        {
            TextFieldParserInitialize(p);
            while (!p.EndOfData)
            {
                Photo phot = processLineScoresUpdate(p.ReadFields());
                Photo photo = phots.SingleOrDefault(x => x.IntRef == phot.IntRef);
                photo.Score = phot.Score;
            }
        }
        int cc = await scope.SaveChangesAsync();
        Messages.Add($"Succesfully Updated {cc} Scores");
    }
    private string ValueGet(string[] data, PropertyInfo p)
    {
        string val = p.GetValue(this).ToString();
        int indx = Headers.IndexOf(val);
        return data[indx];
    }
    private async Task YearEnd()
    {
        try
        {
            string mes = await OctoberMonth();
            Messages.Add(mes);
        }
        catch (Exception ex)
        {
            Errors.Clear();
            Errors.Add(ex.Message);
            Errors.Add(ex.StackTrace);
        }
        StateHasChanged();
    }

    #endregion

    #region ImagesProcess
    /// <summary>
    /// FileChooser to select multiple .zips; Copy these to wwwroot/Import/
    /// </summary>
    /// <returns></returns>
    private async Task<string> FileUpload()
    {
        Filename = await state.ShowFileUpload("ZIPPED PHOTOS", "Upload Zipped Photos from PhotoVault", gData.ImportDirectory);
        if (Filename == null)
            return null;
        return Filename;
    }
    private async Task<string> GenerateImagesAsync(string source, string destination, int width, int height)
    {
        try
        {
            using (var image = await Image.LoadAsync(source))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(width, height)
                }));
                await image.SaveAsync(destination);
                await image.SaveAsync(destination);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to generate thumbnail for {source}: {ex.Message}", ex);
        }
        return destination;
    }
    private async Task<string> ImagesProcessAsync()//START OF IMAGES PROCESS
    {
        try
        {
            Messages.Clear();
            IProgress<ProgressInfo> progress = new Progress<ProgressInfo>(value =>
            {
                Info = value;
                StateHasChanged();
            });
            Filename = await PhotosLocalUnzipAsync(progress);
            Messages.Add(Info.CurrentStage); StateHasChanged();
            await PhotosCopyRenameAsync(progress, Filename);
            Messages.Add(Info.CurrentStage); StateHasChanged();
            await PhotosGeneratePreviewsAsync(progress, Filename, 1365, 768, 50);
            Messages.Add(Info.CurrentStage); StateHasChanged();
            await PhotosZipAsync(progress);
            Messages.Add("UPLOAD TO ABS HOST"); StateHasChanged();
            await UploadToHosting(progress, $"{gData.Downloads}ZippedExport\\BKK-{Filename}.zip");
            Messages.Add(Info.CurrentStage); StateHasChanged();
            progressBar.IsVisible = false;
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
    private async Task<string> Login(HttpClient client)
    {
        string username = config["Auth:Username"];
        string password = config["Auth:Password"];
        var loginData = new
        {
            username = username,
            password = password
        };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync($"{gData.Api}Auth/login", loginContent);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult.Token;
    }
    /// <summary>
    /// Calls FileUpload; Unzip to D:\\BKK\\Inbox\\Club\\Photos\\Date\\
    /// </summary>
    /// <param name="progress"></param>
    /// <returns></returns>
    public async Task<string> PhotosLocalUnzipAsync(IProgress<ProgressInfo> progress)
    {
        string dest = string.Empty;
        await FileUpload();
        var zips = Directory.GetFiles($"{gData.ImportDirectory}", "*.zip");
        int totalZips = zips.Length;
        dest = Path.Combine(gData.photosLocal, Filename);
        Directory.CreateDirectory(dest);
        progressBar.IsVisible = true;
        Info.Message = $"Starting extraction of {zips.Length} ZIP file(s)...";
        Info.Percentage = 0;
        progress.Report(Info);
        progress.Report(new ProgressInfo
        {
            CurrentStage = "UNZIPPING",
            Total = totalZips,
            Current = 0,
            Message = $"Unzipped to BKK folder {totalZips} images...",
            Percentage = 0
        });
        for (int i = 0; i < zips.Length; i++)
        {
            string zipPath = zips[i];
            string zipName = Path.GetFileName(zipPath);
            Info.Message = $"Extracting {zipName} ({i + 1}/{zips.Length})...";
            Info.Percentage = (int)Math.Round((i + 1.0) / totalZips);
            progress.Report(Info);
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, dest, overwriteFiles: true));
        }
        return Filename;
    }
    /// <summary>
    /// Copies all .jpg files from photosLocal\{fn}\ to a Temp\ subfolder, renaming each to its embedded 7-digit ID
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="fn"></param>
    /// <returns></returns>
    public async Task PhotosCopyRenameAsync(IProgress<ProgressInfo> progress, string fn)
    {
        int processed = 0;
        string sourceDir = $"{gData.photosLocal}{fn}\\";
        string destDir = $"{sourceDir}Temp\\";
        Directory.CreateDirectory(destDir);
        string[] files = await Task.Run(() => Directory.GetFiles(sourceDir, "*.jpg"));
        int total = files.Length;
        progress.Report(new ProgressInfo
        {
            CurrentStage = "COPYING & RENAMING",
            Percentage = 0,
            Current = 0,
            Total = total,
            Message = $"Found {total} files – starting copy & rename..."
        });
        var copyTasks = files.Select(filePath => Task.Run(() =>
        {
            try
            {
                string originalName = Path.GetFileName(filePath);
                Match match = Regex.Match(originalName, @"\d{7}");
                string newName = match.Value + ".jpg";
                string destinationPath = Path.Combine(destDir, newName);
                File.Copy(filePath, destinationPath, overwrite: true);
                int current = Interlocked.Increment(ref processed);
                int percent = (int)((current * 100.0) / total);
                progress.Report(new ProgressInfo
                {
                    CurrentStage = "COPYING & RENAMING",
                    Percentage = percent,
                    Current = current,
                    Total = total,
                    Message = $"Processed {current}/{total}"
                });
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref processed);
            }
        })).ToArray();
        await Task.WhenAll(copyTasks);
    }
    /// <summary>
    /// Resizes all .jpg files from Temp\ to photosLocal\WEB\, generating web previews at the given width/height
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="fn"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="degreeOfParallelism"></param>
    /// <returns></returns>
    public async Task PhotosGeneratePreviewsAsync(IProgress<ProgressInfo> progress, string fn, int width, int height, int degreeOfParallelism = 50)
    {
        string sourceDir = $"{gData.photosLocal}{fn}\\Temp\\";
        string destDir = $"{gData.photosLocal}WEB\\";
        string[] imagePaths = Directory.GetFiles(sourceDir, "*.jpg");
        int processedCount = 0;
        int totalImages = imagePaths.Count();
        Directory.CreateDirectory(destDir);
        var options = new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism };
        progress.Report(new ProgressInfo
        {
            CurrentStage = "GENERATING PREVIEWS",
            Total = totalImages,
            Current = 0,
            Message = $"Starting preview generation for {totalImages} images...",
            Percentage = 0
        });
        await Parallel.ForEachAsync(imagePaths, options, async (source, cancellationToken) =>
        {
            try
            {
                string destination = $"{destDir}{Path.GetFileName(source)}";
                await GenerateImagesAsync(source, destination, width, height);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to process {source}: {ex.Message}", ex);
            }
            var newCount = Interlocked.Increment(ref processedCount);
            int percent = (int)Math.Round((newCount * 100.0) / totalImages);
            progress.Report(new ProgressInfo
            {
                CurrentStage = "GENERATING PREVIEWS",
                Total = totalImages,
                Current = newCount,
                Message = $"Generated preview {newCount} of {totalImages}",
                Percentage = percent
            });
        });
    }
    /// <summary>
    /// Zips all web previews from photosLocal\WEB\ into \\Downloads\BKK\\ZippedExport\BKK-{Filename}.zip, then copies each file to the local website path
    /// </summary>>
    public async Task<string> PhotosZipAsync(IProgress<ProgressInfo> progress)
    {
        progress.Report(new ProgressInfo
        {
            CurrentStage = "PHOTOS ZIP",
            Total = 0,
            Current = 0,
            Message = $"",
            Percentage = 0
        });
        string rootdest = $"{gData.clubPhotos}{Filename}\\";
        Directory.CreateDirectory(rootdest);
        string source = string.Empty, dest = string.Empty;
        Task T = Task.Run(() =>
        {
            source = $"{gData.photosLocal}WEB\\";
            string destDir = $"{gData.Downloads}ZippedExport\\";
            Directory.CreateDirectory(destDir);
            dest = $"{destDir}BKK-{Filename}.zip";
            if (File.Exists(dest))
                File.Delete(dest);
            ZipFile.CreateFromDirectory(source, dest);
            foreach (var file in Directory.GetFiles(source))
            {
                string fn = Path.GetFileName(file);
                string localwebimas = $"{gData.LocalWebsitePath}{rootdest}";
                Directory.CreateDirectory(localwebimas);
                dest = $"{localwebimas}{fn}";
                File.Copy(file, dest, true);
                File.Delete(file);
            }
        });
        await T;
        return $"Succesfully ZIPPED {Directory.GetFiles(source).Count()} Photos to {dest}";
    }
    /// <summary>
    /// Authenticates and uploads the zipped photo file to the remote hosting server via HTTP multipart POST (max 120 MB)
    /// </summary>
    private async Task<string> UploadToHosting(IProgress<ProgressInfo> progress, string filePath)
    {
        Messages.Clear();
        if (!File.Exists(filePath))
        {
            Messages.Add($"❌ File not found: {filePath}");
            return null;
        }
        var handler = new HttpClientHandler();
        using HttpClient client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromMinutes(15);
        string token = await Login(client);
        if (string.IsNullOrEmpty(token))
        {
            Messages.Add("❌ Failed to obtain authentication token");
            return null;
        }
        string fileName = Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > 120L * 1024 * 1024)//120 MB limit 
        {
            Messages.Add($"❌ File too large (max 120 MB): {fileName}");
            return null;
        }
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(File.OpenRead(filePath));
        content.Add(fileContent, "file", fileName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            HttpResponseMessage response = null;
            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                response = await client.PostAsync($"{gData.Api}FU/zip", content);
                status = await response.Content.ReadAsStringAsync();
            }
            else
            {
                Messages.Add($"❌ Unsupported file type: {fileName} (only .zip are supported)");
                return null;
            }
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Messages.Add($"✅ {fileName} {result}");
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Messages.Add($"❌ Upload failed: {response.StatusCode} {response.ReasonPhrase}\n{errorBody}");
            }
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Messages.Add($"❌ Error uploading {fileName}: {ex.Message}");
            if (ex.InnerException != null)
            {
                status += $"\nInner exception: {ex.InnerException.Message}";
                Messages.Add(status);
            }
        }
        return null;
    }
    #endregion


}
