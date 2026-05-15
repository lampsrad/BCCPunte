namespace BCC.Pages;

using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OpenXmlPowerTools;
using System.IO;
using System.Xml.Linq;

public partial class WordDoc
{
    private const long MaxUploadBytes = 25 * 1024 * 1024; // 25 MB

    [Inject] NavigationManager nav { get; set; }
    [Inject] IWebHostEnvironment env { get; set; }
    [Inject] State state { get; set; }

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        try
        {
            if (!e.File.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                await state.ShowMessageAsync("Word", "Please select a .docx file.", "OK");
                return;
            }

            string destdir = Path.Combine(env.WebRootPath, "html");
            Directory.CreateDirectory(destdir);
            string destfile = Path.GetFileNameWithoutExtension(e.File.Name);
            string dest = Path.Combine(destdir, $"{destfile}.html");

            using MemoryStream ms = new MemoryStream();
            await using (var src = e.File.OpenReadStream(MaxUploadBytes))
            {
                await src.CopyToAsync(ms);
            }
            ms.Position = 0;

            using WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, true);
            var settings = new HtmlConverterSettings
            {
                PageTitle = destfile,
                CssClassPrefix = "docx-",
                FabricateCssClasses = true
            };
            XElement html = HtmlConverter.ConvertToHtml(wordDoc, settings);
            string htmlString = html.ToString();

            string header = (e.File.Name == "Program (Afr).docx" || e.File.Name == "Program (Eng).docx")
                ? "<img style=\"max-width:100%; max-height:1000px;\" src=\"/html/Venue.png\"/><br/><br/>" +
                  "<img style=\"max-width:100%; max-height:1200px;\" src=\"/html/Program.png\"/><br/><br/>"
                : string.Empty;

            string content = $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>{destfile}</title>" +
                             "<link rel='stylesheet' href='/styles.css'></head>" +
                             $"<body class='body'>{header}{htmlString}</body></html>";

            await File.WriteAllTextAsync(dest, content);
            nav.NavigateTo($"info/{destfile}");
        }
        catch (Exception ex)
        {
            await state.ShowMessageAsync("Word conversion failed", ex.Message, "OK");
        }
    }
}
