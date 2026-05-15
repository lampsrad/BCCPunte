using BCC.Models;
using BCC.Pages;
using BCC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace BCC.Adm.Salonne;

[Authorize]
public partial class SalonEdit
{
    [Inject] DataService Ds { get; set; }
    [Inject] Repo repo { get; set; }
    [Parameter] public SalonMaster SalonMaster { get; set; }
    [Parameter] public Salon Salon { get; set; }
    [Parameter] public EventCallback<Salon> Close { get; set; }
    private void close()
    {
        Close.InvokeAsync(null);
    }

    private async Task Submitted()
    {
        await Close.InvokeAsync(Salon);
    }
}