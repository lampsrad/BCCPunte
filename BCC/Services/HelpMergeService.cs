using System.Text;
using System.Text.Json;
using BCC.Models;
using HtmlAgilityPack;

namespace BCC.Services;

public class HelpMergeService(IWebHostEnvironment env)
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    string JsonPath => Path.Combine(env.WebRootPath, "Help-flows.json");
    string HtmlPath => Path.Combine(env.WebRootPath, "Help.html");
    string JsPath => Path.Combine(env.WebRootPath, "Help-flows.js");

    public async Task<HelpMergeResult> MergeAsync(CancellationToken ct = default)
    {
        if (!File.Exists(JsonPath))
            throw new FileNotFoundException("Help-flows.json not found.", JsonPath);
        if (!File.Exists(HtmlPath))
            throw new FileNotFoundException("Help.html not found.", HtmlPath);

        var data = await ReadDocumentAsync(JsonPath, ct);

        // Prefer full flow set: JSON overrides, missing keys filled from Help-flows.js
        if (File.Exists(JsPath))
        {
            var fromJs = await LoadFlowsFromJsAsync(JsPath, ct);
            if (fromJs.Count > 0)
            {
                foreach (var (id, steps) in fromJs)
                {
                    if (!data.Flows.ContainsKey(id))
                        data.Flows[id] = steps;
                }
            }
        }

        File.Copy(HtmlPath, HtmlPath + ".bak", overwrite: true);

        var doc = new HtmlDocument { OptionOutputOriginalCase = true };
        doc.LoadHtml(await File.ReadAllTextAsync(HtmlPath, Encoding.UTF8, ct));

        int effectsMerged = 0;
        int contentMerged = 0;

        foreach (var row in doc.DocumentNode.SelectNodes("//tr") ?? Enumerable.Empty<HtmlNode>())
        {
            var flowNode = row.SelectSingleNode(".//*[@data-flow-id]");
            if (flowNode is null) continue;

            var flowId = flowNode.GetAttributeValue("data-flow-id", "");
            if (string.IsNullOrEmpty(flowId) || !data.Effects.TryGetValue(flowId, out var effectHtml))
                continue;

            var cell = row.SelectSingleNode(".//td[contains(concat(' ', normalize-space(@class), ' '), ' effect-cell ')]")
                       ?? row.SelectSingleNode(".//td[@class='effect-cell']");
            if (cell is null) continue;

            cell.InnerHtml = effectHtml;
            effectsMerged++;
        }

        foreach (var (editId, fragment) in data.Content)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//*[@data-edit-id={XPath(editId)}]");
            if (node is null) continue;
            node.InnerHtml = fragment;
            contentMerged++;
        }

        await using (var htmlOut = File.Create(HtmlPath))
            doc.Save(htmlOut, Encoding.UTF8);

        int flowsSynced = 0;
        if (data.Flows.Count > 0 && File.Exists(JsPath))
            flowsSynced = await SyncFlowsJsAsync(JsPath, data.Flows, ct);

        await ClearJsonAsync(ct);

        return new HelpMergeResult(effectsMerged, contentMerged, flowsSynced);
    }

    public async Task<HelpFlowsDocument> LoadForApiAsync(CancellationToken ct = default)
    {
        if (!File.Exists(JsonPath))
            return await LoadFromJsFallbackAsync(new HelpFlowsDocument(), ct);

        var doc = await ReadDocumentAsync(JsonPath, ct);
        return await LoadFromJsFallbackAsync(doc, ct);
    }

    async Task<HelpFlowsDocument> LoadFromJsFallbackAsync(HelpFlowsDocument doc, CancellationToken ct)
    {
        doc.Effects ??= new Dictionary<string, string>(StringComparer.Ordinal);
        doc.Content ??= new Dictionary<string, string>(StringComparer.Ordinal);
        doc.Flows ??= new Dictionary<string, List<string>>(StringComparer.Ordinal);

        if (File.Exists(JsPath))
        {
            var fromJs = await LoadFlowsFromJsAsync(JsPath, ct);
            if (fromJs is { Count: > 0 })
            {
                // JSON wins for keys already present; fill missing from JS
                foreach (var (id, steps) in fromJs)
                {
                    if (!doc.Flows.ContainsKey(id))
                        doc.Flows[id] = steps;
                }
            }
        }

        return doc;
    }

    async Task ClearJsonAsync(CancellationToken ct)
    {
        File.Copy(JsonPath, JsonPath + ".bak", overwrite: true);

        var cleared = new HelpFlowsDocument();
        var json = JsonSerializer.Serialize(cleared, JsonOpts);
        var tempPath = JsonPath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, Encoding.UTF8, ct);
        File.Move(tempPath, JsonPath, overwrite: true);
    }

    static async Task<HelpFlowsDocument> ReadDocumentAsync(string jsonPath, CancellationToken ct)
    {
        await using var jsonStream = File.OpenRead(jsonPath);
        var data = await JsonSerializer.DeserializeAsync<HelpFlowsDocument>(jsonStream, JsonOpts, ct)
                   ?? throw new InvalidDataException("Help-flows.json is empty or invalid.");

        data.Effects ??= new Dictionary<string, string>(StringComparer.Ordinal);
        data.Content ??= new Dictionary<string, string>(StringComparer.Ordinal);
        data.Flows ??= new Dictionary<string, List<string>>(StringComparer.Ordinal);
        return data;
    }

    public static async Task<Dictionary<string, List<string>>> LoadFlowsFromJsAsync(string jsPath, CancellationToken ct = default)
    {
        var js = NormalizeNewlines(await File.ReadAllTextAsync(jsPath, Encoding.UTF8, ct));
        if (!TryFindDataBlock(js, out var start, out var end))
            return new Dictionary<string, List<string>>(StringComparer.Ordinal);

        var json = js[(start + "var DATA = ".Length)..end].Trim().TrimEnd(';');
        return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOpts)
               ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
    }

    static string XPath(string value) =>
        value.Contains('\'')
            ? $"\"{value.Replace("\"", "&quot;")}\""
            : $"'{value}'";

    static async Task<int> SyncFlowsJsAsync(string jsPath, Dictionary<string, List<string>> flows, CancellationToken ct)
    {
        var js = NormalizeNewlines(await File.ReadAllTextAsync(jsPath, Encoding.UTF8, ct));
        if (!TryFindDataBlock(js, out var start, out var end))
            return 0;

        var flowsJson = JsonSerializer.Serialize(flows, new JsonSerializerOptions { WriteIndented = true });
        var indented = string.Join("\n", flowsJson.Split('\n').Select(l => "  " + l));
        var replacement = "var DATA = " + indented.TrimEnd();

        File.Copy(jsPath, jsPath + ".bak", overwrite: true);
        var updated = js[..start] + replacement + js[end..];
        await File.WriteAllTextAsync(jsPath, updated, Encoding.UTF8, ct);
        return flows.Count;
    }

    /// <summary>
    /// Locate the <c>var DATA = …</c> block that ends before <c>window.HELP_FLOWS</c>.
    /// Newlines are normalized to LF first so CRLF files (Windows) still match.
    /// </summary>
    static bool TryFindDataBlock(string js, out int start, out int end)
    {
        start = js.IndexOf("var DATA = ", StringComparison.Ordinal);
        end = js.IndexOf("\n\n  window.HELP_FLOWS", StringComparison.Ordinal);
        if (start < 0 || end < 0 || end <= start)
        {
            // Fallback: any blank line before window.HELP_FLOWS
            if (start >= 0)
            {
                var alt = js.IndexOf("\n\nwindow.HELP_FLOWS", StringComparison.Ordinal);
                if (alt < 0) alt = js.IndexOf("\n  window.HELP_FLOWS", StringComparison.Ordinal);
                if (alt > start)
                {
                    end = alt;
                    return true;
                }
            }
            start = -1;
            end = -1;
            return false;
        }
        return true;
    }

    static string NormalizeNewlines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');
}

public record HelpMergeResult(int EffectsMerged, int ContentMerged, int FlowsSynced);
