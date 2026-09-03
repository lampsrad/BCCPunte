using BCC.Models;
using Microsoft.AspNetCore.Components;

namespace BCC.Pages
{
    public partial class Index
    {
        [Inject] public Repo repo { get; set; }
        [Inject] public State state { get; set; }

        public IList<Photo> Winners { get; private set; }
        public string MonthTitle { get; private set; } = string.Empty;

        private async Task LoadWinnersAsync()
        {
            DateOnly photoDate = gData.lastDateClubImported;
            MonthTitle = photoDate.toMonthFull_Year();
            var photos = await repo.GetEntitiesNTAsync<Photo>(
                x => x.Monthly.Date.Year == photoDate.Year
                  && x.Monthly.Date.Month == photoDate.Month
                  && (x.Winner == true || x.Club_Winner == true));
            string monthFolder = photoDate.toYear_Month_Digits();
            string urlBase = $"/photos/{monthFolder}/Temp/";
            foreach (var p in photos)
                p.Filename = p.IntRef.HasValue
                    ? urlBase + p.IntRef + ".jpg"
                    : string.Empty;
            Winners = photos.OrderBy(p => p.Star_Group).ThenBy(p => p.Category).ToList();
        }
        protected override async Task OnInitializedAsync()
        {
            DateOnly date = await repo.lastDateAsync(x => x.ID == "lastImport");
            gData.lastDateClubImported = date.toDateFirstDay();
            date = await repo.lastDateAsync(x => x.ID == "clubYearStart");
            gData.lastDateClubYear = date.toDateFirstDay();
            state.Date = gData.lastDateClubImported;
            state.DatePhoto = state.Date;
            await LoadWinnersAsync();
        }
        
    }
}
