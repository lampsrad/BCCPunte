using BCC.Models;

namespace BCC.Viewmodels;

public class ScoresSingleVM
{
    public IList<Monthly> Scores { get; set; }
    public string Name { get; set; } = string.Empty;
}
