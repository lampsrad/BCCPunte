using BCC.Models;
using BCC.Viewmodels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BCC.Pages;

public partial class PointsMember
{
    [Parameter] public ScoresSingleVM Model { get; set; }
    [Parameter] public string Data { get; set; }
    //[Inject] IJSRuntime _jsr { get; set; }
    [Inject] Repo repo { get; set; }
    string Message = string.Empty;

    //protected async override Task OnAfterRenderAsync(bool firstRender)
    //{
    //    if (firstRender)
    //        //await _jsr.InvokeVoidAsync("scrollTop", "menu");
    //}
    private async void OnSalonClick(Monthly mon)
    {
        Monthly prev;
        await using var scope = repo.CreateScope();
        var mons = await scope.GetEntitiesAsync<Monthly>(x => x.MasterID == mon.MasterID && x.Date > mon.Date);
        foreach (var m in mons)
        {
            prev = await repo.monthlyPrevNTAsync(m);
            m.Salm = m.Salons.Sum(x=>x.Points);
            m.Salp = prev.Salp.AddNull(m.Salm);
        }
        int cc = await scope.SaveChangesDetachAsync();
        Message = $"Succesfully Balanced Monthly Records with Salon entries and Updated {cc} Monthly Records";
        StateHasChanged();
    }
    protected override async void OnParametersSet()
    {
        if (Data == null)
            return;
        await Populate();
        StateHasChanged();  
    }
    private async Task Populate()
    {
        Model = new ScoresSingleVM();
        int IdM = Convert.ToInt32(Data);
        Model.Scores = await repo.GetEntitiesNTAsync<Monthly>(x => x.MasterID == IdM, null);
        Model.Name = Model.Scores[0].Master.Name;
        string name = Model.Name;
        foreach (Monthly m in Model.Scores)
        {
            switch (m.RatingID)
            {
                case 1:
                    if (m.GMp > 3)
                        m.Position = 1;
                    if (m.Pp > 19)
                        m.Position += 2;
                    break;
                case 2:
                    if (m.GMp > 5)
                        m.Position = 1;
                    if (m.Pp > 34)
                        m.Position += 2;
                    break;
                case 3:
                    if (m.GMp > 7)
                        m.Position = 1;
                    if (m.Pp > 49)
                        m.Position += 2;
                    if (m.Salp > 17)
                        m.Position += 4;
                    break;
                case 4:
                    if (m.GMp > 11)
                        m.Position = 1;
                    if (m.Pp > 79)
                        m.Position += 2;
                    if (m.Salp > 49)
                        m.Position += 4;
                    break;
                case 5:
                    if (m.Pp > 149)
                        m.Position += 2;
                    if (m.Salp > 199)
                        m.Position += 4;
                    break;
                case 6:
                    if (m.Pp > 149)
                        m.Position += 2;
                    if (m.Salp > 399)
                        m.Position += 4;
                    break;
            }
        }
    }
}
