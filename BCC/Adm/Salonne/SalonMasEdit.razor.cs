using BCC.Models;
using BCC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace BCC.Adm.Salonne;

[Authorize]
public partial class SalonMasEdit
{
    [Inject] Repo repo { get; set; }
    [Parameter] public EventCallback<SalonMaster> Close { get; set; }
    [Parameter] public SalonMaster SalonMaster { get; set; }

    private async void Cancel()
    {
        await Close.InvokeAsync(null);
    }
    private async Task Submitted()
    {
        if (SalonMaster.ID == 0)
        {
            SalonMaster = new();
        }
        else
            SalonMaster.SalonName = SalonMaster.Alias;
        await repo.UpdateSaveDetachAsync(SalonMaster);
        await Close.InvokeAsync(SalonMaster);
    }

}