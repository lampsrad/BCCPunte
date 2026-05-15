namespace BCC.Models;

public class ProgressInfo
{
    public string CurrentStage { get; set; } = "";
    public int Current { get; set; } = 0;
    public int Total { get; set; } = 0;
    public int Percentage { get; set; } = 0;
    public string Message { get; set; } = "";
}
