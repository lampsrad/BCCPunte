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
    private void InterClub()
    {

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
