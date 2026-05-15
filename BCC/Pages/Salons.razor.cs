using BCC.Models;
using Microsoft.AspNetCore.Components;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BCC.Pages;

public partial class Salons
{
    IList<Salon> Salonne { get; set; }
    [Parameter] public string Title { get; set; }
    [Inject] Repo _repo { get; set; }
    [Inject] State _state { get; set; }
    [Parameter] public string Data { get; set; }
    bool IsSalons => true;  

    private async void MonthChanged(string mes)
    {
        IList<Salon> salList = null;
        DateOnly date;
        DateOnly enddate;
        if (mes == null)
            await OnParametersSetAsync();
        else
        {
            if (mes == "new")
            {
                date = _state.DatePhoto.AddMonths(1);
                salList = await _repo.GetEntitiesNTAsync<Salon>(x => x.Monthly.Date == date);
                Title = date.toMonthFull_Year();
            }
            if (mes == "bcc")
            {
                date = gData.lastDateClubYear;
                Title = $"BCC Year {date.Year}-{date.Year + 1}";
                salList = await _repo.GetEntitiesNTAsync<Salon>(x => x.Monthly.Date >= date);
            }
            if (mes == "pssa")
            {
                date = DateOnly.Parse($"{gData.lastDateClubYear.Year}-{07}-{01}");
                enddate = date.AddMonths(12);
                Title = $"PSSA Impala Year {date.Year}-{date.Year + 1}";
                salList = await _repo.GetEntitiesNTAsync<Salon>(x => x.Monthly.Date >= date && x.Monthly.Date < enddate);
            }
            salList = salList.OrderBy(x => x.SalonMaster.SalonName).ThenByDescending(x => x.Points).ToList();
            Salonne = salList;
        }
        StateHasChanged();
    }
    protected async override Task OnParametersSetAsync()
    {
        DateOnly date = _state.DatePhoto;
        var salList = await _repo.GetEntitiesNTAsync<Salon>(x => x.Monthly.Date == date);
        salList = salList.OrderBy(x => x.SalonMaster.SalonName).ThenByDescending(x => x.Points).ToList();
        Salonne = salList;
        Title = date.toMonthFull_Year();
    }
}