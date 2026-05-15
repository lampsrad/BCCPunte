using BCC.Models;
using BCC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace BCC.Adm.Club;

[Authorize]
public partial class Honours
{
    [Inject] Repo repo { get; set; }
    [Inject] DataService Ds { get; set; }
    [Inject] NavigationManager _nav { get; set; }
    ElementReference refmember;
    IList<Master> Masters = new List<Master>();
    Mod mod = new Mod();

    private void Abort()
    {
        _nav!.NavigateTo("/");
    }
    protected async override Task OnInitializedAsync()
    {
        Masters = await Ds!.Masters();
    }
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            refmember.FocusAsync();
        }
    }
    private async Task OnIDChanged()
    {
        int ID = Convert.ToInt32(mod.ID);
        Master mas = await Ds!.Master(ID);
        mod.PrevTitle = mas.Title;
    }
    private async Task Submitted()
    {
        mod.Title = mod.Title!.ToUpper();
        int ID = Convert.ToInt32(mod.ID);
        Master mas = await repo.GetEntityNTAsync<Master>(x => x.ID == ID);
        Monthly mon = await repo.monthlyLastNTAsync(x => x.MasterID == ID);
        mas.Title = $"{mas.Title} {mod.Title}";
        mon.Title = mod.Title switch
        {
            "APSSA" or "FPSSA" => mod.Title,
            _ => mon.Title
        };
        mon.Promotion = mon switch
        {
            { RatingID: 5, Pp: >= 150, Salp: >= 200, Title: "APSSA" } => true,
            { RatingID: 6, Pp: >= 150, Salp: >= 400, Title: "FPSSA" } => true,
            _ => null
        };
        if (mon.Promotion == true)
        {
            ratings rat;
            rat = (ratings)mon.RatingID!;
            rat += 1;
            mas.RatingID = (int)rat;
            mon.RatingID = (int)rat;
            mon.PromoString = $"Promoted to {rat.ToString().Replace("_", " ")}";
            mon.GMp = null;
            mon.Pp = null;
            mon.Mg = null;
            mon.Gg = null;
            mon.Sg = null;
            mon.Bg = null;
        }
        mon.Photos.Clear();
        mon.Master = null;
        mon.Rating = null;
        mas.Rating = null;
        int cc = await repo.UpdateSaveDetachAsync(mas);
        cc = await repo.UpdateSaveDetachAsync(mon);
        _nav!.NavigateTo("/");
    }

    private class Mod
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string PrevTitle { get; set; }
        public string Title { get; set; }

    }

}