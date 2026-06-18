using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Text.RegularExpressions;

namespace BCC.Adm.Club;

[Authorize]
public partial class Backup
{
    [Parameter] public string Data { get; set; }
    [Parameter] public EventCallback<string> ecbBackup { get; set; }
    [Parameter] public EventCallback<string> ecbRestore { get; set; }
    ElementReference refFile;
    string btnClass = "btn-primary";
    IList<string> Files = new List<string>();
    string Model { get; set; } = string.Empty;
    string InputText { get; set; }
    string Title { get; set; }

    private void keyDownText(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Enter":
                btnClass = "btn-danger";
                StateHasChanged();
                break;
        }
    }
    protected override void OnInitialized()
    {
        var files = Directory.GetFiles($"{gData.backupPath}", "*.bak");
        foreach (string file in files)
        {
            var m1 = Regex.Match(file, @"\w+\.\w+$");
            Files.Add(m1.Value);
        }
    }
    protected override void OnParametersSet()
    {
        Title = Data;
    }
    private async Task Submit()
    {
        if (Title == "Backup")
        {
            string file = string.IsNullOrEmpty(InputText) ? Model : $"{InputText}.bak";
            if (!string.IsNullOrWhiteSpace(file))
                await ecbBackup.InvokeAsync(file);
        }
        if (Title == "Restore" && !string.IsNullOrWhiteSpace(Model))
            await ecbRestore.InvokeAsync(Model);
    }
}
