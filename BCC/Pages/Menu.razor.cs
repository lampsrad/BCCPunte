using BCC.Models;
using Microsoft.AspNetCore.Components;

namespace BCC.Pages;

public partial class Menu : IDisposable
{
    [Inject] State state { get; set; }
    [Inject] IWebHostEnvironment env { get; set; }
    [Inject] IHostApplicationLifetime lifetime { get; set; }
    [Inject] Repo repo { get; set; }
    List<string> menuItems { get; set; } = new List<string>();


    private async Task DatabaseAbsolute()
    {
        gData.connectionKey = "Abshost";
        var hc = await repo.GetEntityNTAsync<HitCounter>(x=>x.ID==1);
        gData.HitCount= hc.Counter ?? 0;
    }
    private async Task DatabaseLocal()
    {
        gData.connectionKey = "Local";
        var hc = await repo.GetEntityNTAsync<HitCounter>(x => x.ID == 1);
        hc.Counter = gData.HitCount>0? gData.HitCount: hc.Counter;
        await repo.UpdateSaveDetachAsync(hc);   
    }
    public void Dispose()
    {
        state.Menu -= stateChanged;
    }
    private async Task InterClub()
    {
        DateOnly cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-12));
        var photos = await repo.GetEntitiesNTAsync<Photo>(x => x.Monthly.Date > cutoffDate && x.Score > 10);
        var juniors = photos.Where(x => x.Club_Rating < 4).OrderByDescending(x => x.Score).ToList();
        var seniors = photos.Where(x => x.Club_Rating > 3&& x.Score>11&&x.Monthly.Master.Firstname!="Trix")
            .OrderByDescending(x => x.Score)
            .ToList();
        var seniors11 = photos.Where(x => x.Club_Rating > 3 && x.Score == 11 && x.Monthly.Master.Firstname != "Trix")
            .OrderByDescending(x => x.Score)
            .ToList();
        foreach (var junior in juniors)
        {
            SetPhotoFilename(junior);
            ExportInterClubPhoto(junior, "Juniors");
        }
        foreach (var senior in seniors)
        {
            SetPhotoFilename(senior);
            ExportInterClubPhoto(senior, "Seniors");
        }
        foreach (var senior in seniors11)
        {
            SetPhotoFilename(senior);
            ExportInterClubPhoto(senior, "Seniors_11");
        }
    }
    private static void SetPhotoFilename(Photo photo)
    {
        if (!photo.IntRef.HasValue)
            return;
        string yearMonth = photo.Monthly.Date.toYear_Month_Digits();
        string photoDir = Path.Combine(gData.photosLocal, yearMonth);
        string intRef = photo.IntRef.Value.ToString("D7");
        photo.Filename = Directory.Exists(photoDir)
            ? Directory.GetFiles(photoDir, "*.jpg")
                .FirstOrDefault(f => Path.GetFileName(f).Contains(intRef))
            : null;
    }
    private static void ExportInterClubPhoto(Photo photo, string subDir)
    {
        if (string.IsNullOrEmpty(photo.Filename) || !File.Exists(photo.Filename))
            return;
        string exportDir = Path.Combine(gData.Exports, subDir);
        Directory.CreateDirectory(exportDir);
        var master = photo.Monthly.Master;
        string destName = $"{master.Lastname} {master.Firstname} {photo.Title} [{photo.Score}].jpg";
        foreach (char c in Path.GetInvalidFileNameChars())
            destName = destName.Replace(c, '_');
        File.Copy(photo.Filename, Path.Combine(exportDir, destName), overwrite: true);
    }
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            state.Menu += stateChanged;
            state.Title = state.DatePhoto.toMonthFull_Year();
            StateHasChanged();
        }
    }
    protected override void OnInitialized()
    {
        string htmldir = $"{env.WebRootPath}\\Html\\";
        //Console.WriteLine($"HTML DIR: {htmldir}");  
        var files = Directory.EnumerateFiles(htmldir, "*.html");
        foreach (var file in files)
        {
            string fn = Path.GetFileNameWithoutExtension(file);
            menuItems.Add(fn);
        }
    }
    private void stateChanged()
    {
        state.Title = state.DatePhoto.toMonthFull_Year();
        InvokeAsync(StateHasChanged);
    }
    private void Close()
    {
        gData.process.CloseMainWindow();
        gData.process.Close();
        lifetime.StopApplication();
    }


}
