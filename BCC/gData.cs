using System.Diagnostics;

namespace BCC;

public class gData
{
    public static string Api { get; set; } = "https://bkk.co.za/";
    //public static string Api { get; set; } = "https://oef.bkk.co.za/";
    //public static string Api { get; set; } = "https://localhost:7265/";
    public static string backupPath { get; set; }= "C:\\Users\\Lamps\\OneDrive\\Database\\SQL16\\Backup\\BCC\\";   
    public static string ClubName { get; set; } = "Bloemfontein";
    public static string clubPhotos { get; set; } = "wwwroot\\ClubPhotos\\";
    public static string connectionKey { get; set; }
    public static string machineName { get; set; }
    public static string dbName { get; set; } = "BCC";
    public static string FtpServer { get; set; } = "ftp.bkk.co.za";
    public static string FtpUsername { get; set; } = "bkkcoza";
    public static string FtpPassword { get; set; } = "ftxIOo]J376L!4";
    public static int HitCount { get; set; } = 0;
    public static string HtmlPath { get; set; } = "wwwroot\\Html\\";
    public static string ImportDirectory { get; set; } = "wwwroot\\Import\\";
    public static DateOnly lastDateClubYear { get; set; }
    public static DateOnly lastDateClubImported { get; set; }
    public static readonly object locked = new object();
    public static string LocalWebsitePath { get; set; } = "D:\\VS\\VS Active\\BCCBlazor\\BCC\\";
    public static string photosLocal { get; set; } = "D:\\BKK\\Inbox\\Club\\Photos\\";
    public static string WordFiles { get; set; } = "D:\\VS\\VS Active\\BCCPunte\\BCC\\Wordfiles\\";
    public static Process process { get; set; }
    public static string Downloads { get; set; } = "C:\\Users\\lammie\\Downloads\\BKK\\";

    public static void StartBrowser(string url)
    {
        Process g = new Process();
        process = g;
        g.StartInfo.FileName = Environment.MachineName == "XPS" ? @"C:\Program Files\Google\Chrome\Application\chrome.exe" : @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        g.StartInfo.Arguments = url;
        g.Start();
    }
}
