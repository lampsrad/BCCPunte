using BCC.Models;
using BCC.Viewmodels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace BCC.Adm.Salonne;

[Authorize]
public partial class SalonIndex
{
    [Parameter] public IList<Salon> Salons { get; set; }
    [Parameter] public SalonMaster SalonMaster { get; set; }
    [Parameter] public EventCallback BacktoParent { get; set; }
    [Parameter] public EventCallback<int> CreateSalon { get; set; }
    [Parameter] public EventCallback<Salon> EditSalon { get; set; }
    [Parameter] public EventCallback<sortVM> sortTable { get; set; }
    private string activeSortColum = "Points";
    private bool isSortedAscending = true;

    private async Task Back()
    {
       await BacktoParent.InvokeAsync();
    }
    private void Create()
    {
        CreateSalon.InvokeAsync(SalonMaster!.ID);
    }
    private void Edit(Salon sal)
    {
        EditSalon.InvokeAsync(sal);
    }
    private string setSortIcon(string columnName)
    {
        if (activeSortColum != columnName)
            return string.Empty;
        if (isSortedAscending)
            return "fa-sort-up";
        else
            return "fa-sort-down";
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

}