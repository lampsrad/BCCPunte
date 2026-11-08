namespace BCC.Models;

public class HelpFlowsDocument
{
    public Dictionary<string, List<string>> Flows { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Effects { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Content { get; set; } = new(StringComparer.Ordinal);
}

public class FlowStepsUpdate
{
    public List<string> Steps { get; set; } = [];
}

public class HtmlFragmentUpdate
{
    public string Html { get; set; } = "";
}
