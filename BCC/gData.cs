using System.Diagnostics;

namespace BCC;

public class gData
{
    public static string Api { get; set; } = "https://bkk.co.za/";
    //public static string Api { get; set; } = "https://oef.bkk.co.za/";
    //public static string Api { get; set; } = "http://localhost:5125/";

    public static string backupPath { get; set; }= "C:\\Users\\Lamps\\OneDrive\\Database\\SQL16\\Backup\\BCC\\";
    public static string sqlStagingPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "BCC", "SqlBackup");
    public static string ClubName { get; set; } = "Bloemfontein";
    public static string clubPhotos { get; set; } = "wwwroot\\ClubPhotos\\";
    public static string connectionKey { get; set; }
    public static string Exports { get; set; } = "D:\\VS\\Exports\\";
    public static string machineName { get; set; }
    public static string dbName { get; set; } = "BCC";
    public static string ServerName { get; set; } = "SQL16";
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
    public static string Downloads { get; set; } = Environment.MachineName=="XPS"?  "C:\\Users\\lammie\\Downloads\\BKK\\" : "C:\\Users\\lamps\\Downloads\\BKK\\";

    public static void EnsureSqlStagingPath()
    {
        Directory.CreateDirectory(sqlStagingPath);
        string[] sqlAccounts =
        [
            $@"NT SERVICE\MSSQL${ServerName}",
            @"NT SERVICE\MSSQLSERVER"
        ];
        foreach (string account in sqlAccounts)
        {
            try
            {
                using Process p = Process.Start(new ProcessStartInfo
                {
                    FileName = "icacls",
                    Arguments = $"\"{sqlStagingPath}\" /grant \"{account}:(OI)(CI)M\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                });
                p?.WaitForExit(5000);
            }
            catch { }
        }
    }

    public static void StartBrowser(string url)
    {
        Process g = new Process();
        process = g;
        g.StartInfo.FileName = Environment.MachineName == "XPS" ? @"C:\Program Files\Google\Chrome\Application\chrome.exe" : @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        g.StartInfo.Arguments = url;
        g.Start();
    }

    /// <summary>
    /// Creates an HttpClientHandler suitable for calling the remote upload API.
    /// For localhost targets, accepts self-signed certificates (dev convenience).
    /// </summary>
    public static HttpClientHandler CreateUploadClientHandler()
    {
        var handler = new HttpClientHandler();
        if (Api.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        return handler;
    }
}
