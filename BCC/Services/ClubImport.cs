using BCC.Models;
using Microsoft.VisualBasic.FileIO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BCC.Services;

public class ClubImport
{
    private Repo repo;
    private DataService ds;
    private string Message { get; set; }
    private IList<string> Headers { get; set; }
    private IList<string> Messages { get; set; } = new List<string>();

    public ClubImport(Repo rep, DataService dser)
    {
        repo = rep;
        ds = dser;
    }


    private async Task<Master> AddNewMaster(Photo phot)
    {
        Master master = new Master();
        string[] data = phot.Name.Split(' ');
        master.Lastname = data[0].Trim();
        master.Firstname = data[1].Trim();
        master.Name = phot.Name;
        master.RatingID = phot.Club_Rating;
        master.Title = phot.Honours;
        master.Active = true;
        master.IdVault = phot.PVID != null ? phot.PVID : master.IdVault;
        master.Email = phot.Email != null ? phot.Email : master.Email;
        return master;
    }
    public async Task DatesStoreInDb()
    {
        Datum lastImport = await repo.Datum(x => x.ID == "lastImport");
        var latestdate = await repo.monthlyLastDateAsync();//Date of Monhly with max ID 
        if(latestdate.Month==9)
            latestdate = latestdate.AddMonths(1);
        lastImport.Date = latestdate;
        gData.lastDateClubImported = lastImport.Date;
        if (lastImport.Date.Month == 11)
        {
            Datum newyear = await repo.Datum(x => x.ID == "clubYearStart");
            newyear.Date = lastImport.Date;
            gData.lastDateClubYear = newyear.Date;
        }
        //await repo.SaveChangesAsync();
        await repo.UpdateSaveDetachAsync(lastImport);   
    }
    public async Task<Master> GetMasterRow(Photo phot)
    {
        Master master = await repo.GetEntityNTAsync<Master>(x => x.IdVault == phot.PVID);
        if (master == null)
        {
                Messages.Add($"A New Master was added as {phot.Name} was not found in DB");
                master = await AddNewMaster(phot);
        }
        master.Title = phot.Honours;
        await repo.UpdateSaveDetachAsync(master);
        return master;
    }
    private async Task<bool> HeadersCheckScores(TextFieldParser p)
    {
        foreach (var pr in typeof(ScoreHeaders).GetProperties())
        {
            string val = pr.GetValue(this).ToString();
            if (Headers.Any(x => x == val) == false)
                throw new Exception("Headers of Import File is Incorrect");
        }
        string ln = p.PeekChars(400);
        ln=ln.Replace("\"",string.Empty).Replace(",",";");
        var m1 = Regex.Match(ln, @"jpg;(?<iref>\d+);");
        string intref = m1.Groups["iref"].Value;
        int? ir = int.Parse(intref);
        bool imported = await repo.AnyAsync<Photo>(x => x.IntRef == ir && x.Monthly.Date.Year == gData.lastDateClubImported.Year);
        return imported;
    }
    private async Task<string> Import(string fn)
    {
       // gData.ImportDate = DateOnly.FromDateTime(DateTime.Now);
        Message = string.Empty;
        await ds.LastDates();//Gets from DB gData.lastDateImported ; gData.lastDateClubYear
        string file = Directory.GetFiles(gData.ImportDirectory, $"*{fn}*").FirstOrDefault();
        if (file==null)
            throw new Exception($"File is not a {fn} File");
        return file;
    }
    public async Task<IList<string>> ImportResults(DateOnly date)//IMPORT RESULTSHEET ENTERS HERE
    {
        IList<Photo> photos = new List<Photo>();
        var file = await Import("Results");//Sets gData.lastDates; gets file to import
        using (TextFieldParser p = new TextFieldParser(file))
        {
            TextFieldParserInitialize(p);//Headers done
             //await HeadersCheckResults();
            while (!p.EndOfData)
            {
                Photo photo = ProcessLineResults(p.ReadFields());
                Master master = await GetMasterRow(photo);
                photos.Add(photo);
            }
        }
        photos = ProcessAwards(photos);
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
    public async Task<IList<string>> ImportScores(string filename)//IMPORT SCORESHEET
    {
        IList<Photo> photos = new List<Photo>();
        string file = await Import(filename);        
        using (TextFieldParser p = new TextFieldParser(file))
        {
           TextFieldParserInitialize(p);
            bool imported = await HeadersCheckScores(p);
            if (imported == true)
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
        foreach(var key in gpvid)
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
    public async Task<string> OctoberMonth()
    {
        Message = string.Empty;
        await ds.LastDates();//Gets from DB gData.lastDateImported ; gData.lastDateClubYear
        await ds.Promotion_Due();//Promotions for latest month(Promote for salons entered September)
        await DatesStoreInDb();
        Message = "October month with no Imports, but Promotions succesfully done";
        return Message;
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
    private IList<Photo> ProcessAwards(IList<Photo> photos)
    {
        // Group photos by Internal Reference and filter for duplicates
        var duplicateGroups = photos
            .GroupBy(p => p.IntRef)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            var groupPhotos = group.ToList();
            var mainPhoto = groupPhotos.FirstOrDefault(p => IsMainAward(p.Award));

            if (mainPhoto == null) continue;

            bool? isClubWinner = null;
            bool? isWinner = null;

            foreach (var photo in groupPhotos)
            {
                if (photo.Award == mainPhoto.Award) continue;

                photo.Flag = true;
                isClubWinner = UpdateClubWinner(photo.Award, isClubWinner);
                isWinner = UpdateWinner(photo.Award, isWinner);
            }

            mainPhoto.Winner = isWinner;
            mainPhoto.Club_Winner = isClubWinner;
        }

        // Remove flagged photos
        photos = photos.Where(p => p.Flag != true).ToList();
        return photos;
    }
    private bool IsMainAward(string award) =>
        award is "C" or "G" or "S" or "B";
    private bool? UpdateClubWinner(string award, bool? current) =>
        award is "IC-JNR1" or "IC-SNR1" ? true : current;
    private bool? UpdateWinner(string award, bool? current) =>
        award is "N12" or "N3" or "N45" or "NGG" or "P12" or "P3" or "P45" or "PGG" or "SET" ? true : current;
    private Photo ProcessLineResults(string[] data)
    {
        var photo = new Photo();
        string name = string.Empty;
        for (int i = 0; i < Headers.Count; i++)
        {
            var header = Headers[i].ToLowerInvariant();
            var value = data[i];
            switch (header)
            {
                case "category name":
                    photo.Category = value switch
                    {
                        "Nature Photos" => "N",
                        "Pictorial Photos" => "P",
                        "Set Subject" => "S",
                        _ => null
                    };
                    break;
                case "award id":
                    photo.Award = value == "COM" ? "C" : value;
                    break;
                case "event photo id":
                    if (int.TryParse(value, out int intRef))
                        photo.IntRef = intRef;
                    break;
                case "photo title":
                    photo.Title = value;
                    break;
                case "lastname":
                    name = $"{value} {name}".Trim();
                    break;
                case "firstname":
                    name = $"{name} {value}".Trim();
                    break;
                case "club star rating":
                    photo.Club_Rating = value.ToLowerInvariant() switch
                    {
                        "golden honours" => 6,
                        "galaxy" => 7,
                        _ => int.TryParse(value, out int rating) ? rating : 0
                    };
                    photo.Star_Group = photo.Club_Rating switch
                    {
                        1 or 2 => 1,
                        3 => 2,
                        4 or 5 => 3,
                        6 or 7 => 4,
                        _ => 1
                    };
                    break;
                case "honours":
                    photo.Honours = value;
                    break;
            }
        }
        photo.Name = name.Trim();
        return photo;
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
    private string ValueGet(string[] data, PropertyInfo p)
    {
        string val = p.GetValue(this).ToString();
        int indx = Headers.IndexOf(val);
        return data[indx];
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
    private async Task UpdateScores(string file)
    {
        await using var scope = repo.CreateScope();
        var phots = await scope.GetEntitiesAsync<Photo>(x => x.Monthly.Date >= gData.lastDateClubImported);
        Photo first = phots.FirstOrDefault();
        if (first.Score > 0)
            throw new Exception("Scores already Present; No Update is neccesary!!", null) { HResult=1};
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
}
