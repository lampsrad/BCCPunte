using BCC.Models;
using BCC.Viewmodels;
using BKK.Services;
using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace BCC.Adm.Salonne;

public partial class SalonMasIndex
{
    [Parameter] public string Title { get; set; }
    [Parameter] public bool delete { get; set; }
    [Parameter] public IList<SalonMaster> salonMasters { get; set; }
    [Parameter] public EventCallback<int> salonDelete { get; set; }
    [Parameter] public EventCallback<int> salonEdit { get; set; }
    [Parameter] public EventCallback<int> salonShow { get; set; }
    [Parameter] public EventCallback<sortVM> sortTable { get; set; }
    [Inject] State state { get; set; }
    [Inject] SalonImport si { get; set; }

    private string activeSortColum = "Points";
    private bool isSortedAscending = true;

    private Task Delete(SalonMaster sm) => salonDelete.InvokeAsync(sm.ID);
    private Task Edit(SalonMaster sm) => salonEdit.InvokeAsync(sm.ID);
    private Task Show(SalonMaster sm) => salonShow.InvokeAsync(sm.ID);

    private async Task SalonImport(SalonMaster sm)
    {
        string salonname = Regex.Match(sm.Alias ?? sm.SalonName, @"^(\w+)").Value;

        if (sm.Salons.Any())
        {
            await state.ShowMessageAsync("SALON IMPORT", $"{sm.SalonName} Already Imported", "ok");
            return;
        }

        state.TitleD = "Choose CSV File";
        var fn = await state.ShowFileUpload("File Upload", $"Select {sm.SalonName} File", gData.ImportDirectory);
        if (fn == null)
            return;

        if (!fn.Contains(salonname.ToLower()))
        {
            await state.ShowMessageAsync("SALON IMPORT", $"File does not contain {salonname}", "ok");
            return;
        }

        var mess = await si.ImportSalon(sm, salonname);
        await state.ShowMessageAsync("SALON IMPORT", string.Join(Environment.NewLine, mess), "ok");
    }

    private void SortTable(string colName)
    {
        bool ascending = colName != activeSortColum || !isSortedAscending;
        activeSortColum = colName;
        isSortedAscending = ascending;
        sortTable.InvokeAsync(new sortVM { colName = colName, ascending = ascending });
    }

    private string setSortIcon(string ico) =>
        activeSortColum != ico ? string.Empty : isSortedAscending ? "fa-sort-up" : "fa-sort-down";
}
