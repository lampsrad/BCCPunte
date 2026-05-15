using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel;

namespace BCC;

public class State
{
    private DateOnly date;
    public event Action<bool> Progress;
    public event Action Menu;
    public event Func<Task<string>> FileNameGet;
    public event Func<string,string, string, Task<string>> FileUpload;
    public event Func<string, string, string, Task<string>> MessageShow;
    public event Func<string, string, Task<bool>> MessageConfirm;
    public DateOnly Date { get; set; }
    public DateOnly DatePhoto
    {
        get { return date; }
        set
        {
            date = value;
            Menu?.Invoke();
        }
    }
    public string Member { get; set; }
    public string Message { get; set; }
    public string Title { get; set; }
    public double ProgressVal { get; set; }
    public string TitleD { get; set; }

    public void Hide()
    {
        ProgressVal = 0;
        Progress?.Invoke(false);
    }
    public async Task<string> ShowFilePicker()
    {
        var file = await FileNameGet?.Invoke();
        return file;
    }
    public async Task<string> ShowFileUpload(string title, string message, string destination)
    {
       return await FileUpload?.Invoke(title, message, destination); 
    }
    public async Task<string> ShowMessageAsync(string title, string message, string button)
    {
        var mes = await MessageShow?.Invoke(title, message, button);
        return mes;
    }
    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        return await MessageConfirm?.Invoke(title, message);
    }
    public void ShowProgress(string title = "Progress", bool showProgress = true, double initialProgress = 0)
    {
        TitleD = title;
        ProgressVal = initialProgress;
        Progress?.Invoke(true);
    }
    public void UpdateProgress(double progress, string title = null)
    {
        if (Progress is not null)
        {
            TitleD = title ?? TitleD;
            ProgressVal = Math.Max(0, Math.Min(100, progress)); // Clamp to 0-100
            Progress?.Invoke(true);
        }
    }

}
