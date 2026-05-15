using BCC.Models;
using BCC.Viewmodels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BCC.Pages;

public partial class Points
{
    [Parameter] public ScoresVM Model { get; set; }
    [Parameter] public string Title { get; set; }
    [Parameter] public string Data { get; set; }
    [Inject] State state { get; set; }
    [Inject] Repo repo { get; set; }
    [Inject] IJSRuntime jsr { get; set; }
    [Inject] NavigationManager nav { get; set; }


    private async void MonthChanged()
    {

        await OnParametersSetAsync();
        StateHasChanged();
    }
    protected async override Task OnParametersSetAsync()
    {
        DateOnly clubdate, date;
        date = state.DatePhoto;
        clubdate = date.toClubDate();
        if (date == clubdate)//Month 11
            clubdate = date.AddMonths(-1);
        IList<Monthly> mons = await repo.monthliesLastAsyncNT(x => x.Date >= clubdate && x.Date <= date, date);
        ScoresVM vM = new ScoresVM();
        if (date.Month == 11)//Month 11
        {
            vM.Prom = mons.Where(x => x.Promotion == true).ToList();
        }
        else
        {
            vM.Prom = mons.Where(x => x.Promotion == true && x.Date == date).ToList();
        }
        mons = mons.OrderByDescending(x => x.Py).ToList();
        vM.Py = mons.Take(10).ToList();
        mons = mons.OrderByDescending(x => x.VOy).ToList();
        vM.Voy = mons.Take(10).ToList();
        mons = mons.OrderByDescending(x => x.Saly).ToList();
        vM.Saly = mons.Take(10).ToList();
        foreach (Monthly mp in vM.Prom)
        {
            rate rat = (rate)mp.RatingID;
            rate prev = (rate)mp.RatingID - 1;
            mp.PromoString = prev + " to " + rat;
        }
        foreach (Monthly m in mons)
        {
            switch (m.RatingID)
            {
                case 1:
                    vM.S1.Add(m);
                    break;
                case 2:
                    vM.S2.Add(m);
                    break;
                case 3:
                    vM.S3.Add(m);
                    break;
                case 4:
                    vM.S4.Add(m);
                    break;
                case 5:
                    vM.S5.Add(m);
                    break;
                case 6:
                    vM.S6.Add(m);
                    break;
                case 7:
                    vM.S7.Add(m);
                    break;
            }
        }

        vM.S1 = vM.S1.OrderBy(x => x.Master.Name).ToList();
        vM.S2 = vM.S2.OrderBy(x => x.Master.Name).ToList();
        vM.S3 = vM.S3.OrderBy(x => x.Master.Name).ToList();
        vM.S4 = vM.S4.OrderBy(x => x.Master.Name).ToList();
        vM.S5 = vM.S5.OrderBy(x => x.Master.Name).ToList();
        vM.S6 = vM.S6.OrderBy(x => x.Master.Name).ToList();
        vM.S7 = vM.S7.OrderBy(x => x.Master.Name).ToList();
        Model = vM;
        Title = date.toMonthFull_Year() + " SCORE SHEET FOR ACTIVE MEMBERS";
    }
    private void ViewAllScores(Monthly mon)
    {
        nav.NavigateTo($"pointsmember/{mon.MasterID}");
    }
}