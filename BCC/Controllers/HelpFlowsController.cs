using System.Text.Json;
using BCC.Models;
using BCC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BCC.Controllers;

[Route("api/help-flows")]
[ApiController]
[IgnoreAntiforgeryToken]
public class HelpFlowsController(IWebHostEnvironment env, HelpMergeService helpMerge) : ControllerBase
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    string JsonPath => Path.Combine(env.WebRootPath, "Help-flows.json");
    string BackupPath => JsonPath + ".bak";

    [HttpGet]
    public async Task<ActionResult<HelpFlowsDocument>> Get(CancellationToken ct)
    {
        if (!System.IO.File.Exists(JsonPath))
            return NotFound("Help-flows.json not found.");

        var doc = await helpMerge.LoadForApiAsync(ct);
        return doc;
    }

    [HttpPost("merge")]
    public async Task<ActionResult<HelpMergeResult>> Merge(CancellationToken ct)
    {
        try
        {
            var result = await helpMerge.MergeAsync(ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("flows/{flowId}")]
    public async Task<IActionResult> PutFlow(string flowId, [FromBody] FlowStepsUpdate update, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(flowId))
            return BadRequest("flowId is required.");
        if (update.Steps is null || update.Steps.Count == 0)
            return BadRequest("Steps must not be empty.");

        // Keep full set when JSON was empty/partial so other flows are not dropped
        var doc = await helpMerge.LoadForApiAsync(ct);
        doc.Flows[flowId] = update.Steps.Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (doc.Flows[flowId].Count == 0)
            return BadRequest("Steps must not be empty.");

        await WriteWithBackup(doc, ct);
        return Ok(new { flowId, stepCount = doc.Flows[flowId].Count });
    }

    [HttpPut("effects/{flowId}")]
    public async Task<IActionResult> PutEffect(string flowId, [FromBody] HtmlFragmentUpdate update, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(flowId))
            return BadRequest("flowId is required.");

        var doc = await ReadOrCreate(ct);
        doc.Effects[flowId] = update.Html ?? "";
        await WriteWithBackup(doc, ct);
        return Ok(new { flowId });
    }

    [HttpPut("content/{editId}")]
    public async Task<IActionResult> PutContent(string editId, [FromBody] HtmlFragmentUpdate update, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(editId))
            return BadRequest("editId is required.");

        var doc = await ReadOrCreate(ct);
        doc.Content[editId] = update.Html ?? "";
        await WriteWithBackup(doc, ct);
        return Ok(new { editId });
    }

    [HttpPut]
    public async Task<IActionResult> PutAll([FromBody] HelpFlowsDocument doc, CancellationToken ct)
    {
        if (doc.Flows is null || doc.Flows.Count == 0)
            return BadRequest("Flows must not be empty.");

        doc.Effects ??= new Dictionary<string, string>(StringComparer.Ordinal);
        doc.Content ??= new Dictionary<string, string>(StringComparer.Ordinal);
        await WriteWithBackup(doc, ct);
        return Ok(new { flowCount = doc.Flows.Count });
    }

    async Task<HelpFlowsDocument> ReadOrCreate(CancellationToken ct)
    {
        if (!System.IO.File.Exists(JsonPath))
            return new HelpFlowsDocument();

        await using var stream = System.IO.File.OpenRead(JsonPath);
        var doc = await JsonSerializer.DeserializeAsync<HelpFlowsDocument>(stream, JsonOpts, ct)
                  ?? new HelpFlowsDocument();
        doc.Effects ??= new Dictionary<string, string>(StringComparer.Ordinal);
        doc.Content ??= new Dictionary<string, string>(StringComparer.Ordinal);
        doc.Flows ??= new Dictionary<string, List<string>>(StringComparer.Ordinal);
        return doc;
    }

    async Task WriteWithBackup(HelpFlowsDocument doc, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(JsonPath)!;
        Directory.CreateDirectory(dir);

        if (System.IO.File.Exists(JsonPath))
            System.IO.File.Copy(JsonPath, BackupPath, overwrite: true);

        doc.Effects ??= new Dictionary<string, string>(StringComparer.Ordinal);
        doc.Content ??= new Dictionary<string, string>(StringComparer.Ordinal);
        doc.Flows ??= new Dictionary<string, List<string>>(StringComparer.Ordinal);

        await using var stream = System.IO.File.Create(JsonPath);
        await JsonSerializer.SerializeAsync(stream, doc, JsonOpts, ct);
    }
}
