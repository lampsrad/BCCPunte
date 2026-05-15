using BCC.Models;
using BCC.Viewmodels;
using BKK.Services;
using Microsoft.AspNetCore.Authorization;
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

    private async void Delete(SalonMaster sm)
    {
        await salonDelete.InvokeAsync(sm.ID);
    }
    private async void Edit(SalonMaster sm)
    {
        await salonEdit.InvokeAsync(sm.ID);
    }
    private async void SalonImport(SalonMaster sm)
    {
        var m1 = Regex.Match(sm.Alias?? sm.SalonName, @"^(\w+)");
        string salonname = m1.ToString();
        if (sm.Salons.Any())
        {
            var m = await state.ShowMessageAsync("SALON IMPORT",$"{sm.SalonName} Already Imported", "ok");
            return;
        }
        state.TitleD = "Choose CSV File";
       var fn  = await state.ShowFileUpload("File Upload",$"Select {sm.SalonName} File", gData.ImportDirectory);
        if (fn == null)
            return;
        if(!fn.Contains(salonname.ToLower()))
        {
            await state.ShowMessageAsync("SALON IMPORT", $"File does not contain {salonname}", "ok");
            return;
        }
       var mess = await si.ImportSalon(sm, salonname);
        string mes = string.Join(Environment.NewLine, mess);
        await state.ShowMessageAsync("SALON IMPORT", $"{mes}", "ok");
    }
    private async void Show(SalonMaster sm)
    {
        await salonShow.InvokeAsync(sm.ID);
    }
    private void SortTable(string colName)
    {
        sortVM vm = new sortVM();
        vm.colName = colName;
        if (colName != activeSortColum)
        {
            vm.ascending = true;
            isSortedAscending = true;
            activeSortColum = colName;
        }
        else
        {
            if (isSortedAscending)
                vm.ascending = false;
            else
                vm.ascending = true;
            isSortedAscending = !isSortedAscending;
        }
        sortTable.InvokeAsync(vm);
    }
    private string setSortIcon(string ico)
    {
        if (activeSortColum != ico)
            return string.Empty;
        if (isSortedAscending)
            return "fa-sort-up";
        else
            return "fa-sort-down";
    }

}