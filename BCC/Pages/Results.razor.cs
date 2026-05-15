using BCC.Models;
using BCC.Viewmodels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Linq;

namespace BCC.Pages;

public partial class Results
{
    [Inject] State _state { get; set; }
    [Inject] Repo _repo { get; set; }
    [Inject] IJSRuntime _jsr { get; set; }
    [Parameter] public ResultsVM Model { get; set; }
    [Parameter] public string Title { get; set; }
    [Parameter] public int Data { get; set; }

    private async void MonthChanged()
    {
        await OnParametersSetAsync();
        StateHasChanged();
    }
    protected async override Task OnParametersSetAsync()
    {
        Task<ResultsVM> T = Task.Run(async () =>
        {
            Model = new ResultsVM();
            var phots = await _repo.GetEntitiesIEnumNTAsync<Photo>(x => x.Monthly.Date.Year == _state.DatePhoto.Year && x.Monthly.Date.Month == _state.DatePhoto.Month, null);
            phots=phots.OrderByDescending(x=>x.Score).ThenBy(x=>x.Monthly.Master.Lastname).ToList();    

            Model.Winners = phots.Where(x => x.Winner == true || x.Club_Winner == true).OrderBy(x => x.Star_Group).ToList();
            foreach (Photo p in phots)
            {
                switch (p.Category)
                {
                    case "N1":
                        Model.N1.Add(p);
                        break;
                    case "N2":
                        Model.N2.Add(p);
                        break;
                    case "N3":
                        Model.N3.Add(p);
                        break;
                    case "N4":
                        Model.N4.Add(p);
                        break;
                    case "P1":
                        Model.P1.Add(p);
                        break;
                    case "P2":
                        Model.P2.Add(p);
                        break;
                    case "P3":
                        Model.P3.Add(p);
                        break;
                    case "P4":
                        Model.P4.Add(p);
                        break;
                    case "S":
                        Model.S.Add(p);
                        break;
                    case "PH":
                        Model.PH.Add(p);
                        break;
                }
            }

            return Model;
        });
        await T;
        Title = _state.DatePhoto.toMonthFull_Year() + " : " + "UITSLAE / RESULTS";
    }
    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
       // await _jsr.InvokeVoidAsync("scrollTop", "menu");
    }
}