using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace BCC.Pages;


public partial class DateChange
{
    [Inject] State state { get; set; }
    [Parameter] public EventCallback<string> ecbMonthChanged { get; set; }
    [Parameter] public bool Months13 { get; set; } = true;
    [Parameter] public bool IsSalons { get; set; }
    private async Task OnClicked(int m)
    {
        if (m == 13)
        {
            state.Date = gData.lastDateClubImported.AddMonths(1);
        }
        else
        {
           int sm = gData.lastDateClubImported.Month - m;
            if (sm < 0)
                sm = 12 + sm;
            state.Date = gData.lastDateClubImported.AddMonths(-sm);
        }
        state.DatePhoto = state.Date;
       await ecbMonthChanged.InvokeAsync(null);
    }
    private async Task OnBCCYear()
    {
        await ecbMonthChanged.InvokeAsync("bcc");
    }
    private async Task OnNewMonth()
    {
        await ecbMonthChanged.InvokeAsync("new");
    }
    private async Task OnPSSAYear()
    {
        await ecbMonthChanged.InvokeAsync("pssa");
    }

}