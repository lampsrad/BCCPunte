using BCC.Models;
using BCC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace BCC.Adm.Salonne;

[Authorize]
public partial class SalonCreate
{
    [Parameter] public SalonMaster SalonMaster { get; set; }
    [Parameter] public IList<Master> Masters { get; set; }
    [Parameter] public Salon Salon { get; set; }
    [Parameter] public EventCallback<Salon> Close { get; set; }

    private void close()
    {
        Close.InvokeAsync(null);
    }

    private async Task Submitted()
    {
        await Close.InvokeAsync(Salon);
        return;
    }
}