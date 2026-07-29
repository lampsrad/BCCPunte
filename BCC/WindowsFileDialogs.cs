using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BCC;

/// <summary>
/// Native Windows file/folder dialogs for local admin use (same STA pattern as Project Viewer).
/// Only meaningful when the Blazor Server process runs on the same Windows desktop as the admin.
/// </summary>
public static class WindowsFileDialogs
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Invisible topmost owner so common dialogs appear in front of the browser / app window
    /// instead of behind it (STA thread has no natural UI owner).
    /// </summary>
    private static Form CreateDialogOwner()
    {
        var owner = new Form
        {
            Text = string.Empty,
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            Size = new Size(1, 1),
            Opacity = 0,
            TopMost = true
        };
        owner.Show();
        owner.BringToFront();
        SetForegroundWindow(owner.Handle);
        return owner;
    }

    /// <summary>
    /// Opens a native Windows OpenFileDialog on an STA thread.
    /// Returns selected full paths, or null if the user cancels.
    /// </summary>
    public static Task<string[]> PickFilesAsync(
        string title,
        string filter = "All files (*.*)|*.*",
        string initialDirectory = null,
        bool multiselect = true)
    {
        return Task.Run(() =>
        {
            string[] selected = null;
            var thread = new Thread(() =>
            {
                using var owner = CreateDialogOwner();
                using var dialog = new OpenFileDialog
                {
                    Title = title,
                    Filter = filter,
                    Multiselect = multiselect,
                    CheckFileExists = true
                };
                if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
                    dialog.InitialDirectory = initialDirectory;
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                    selected = dialog.FileNames;
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return selected;
        });
    }

    /// <summary>
    /// Opens a native Windows FolderBrowserDialog on an STA thread.
    /// Returns the selected path, or null if the user cancels.
    /// </summary>
    public static Task<string> PickDirectoryAsync(string description, string initialPath = null)
    {
        return Task.Run(() =>
        {
            string selected = null;
            var thread = new Thread(() =>
            {
                using var owner = CreateDialogOwner();
                using var dialog = new FolderBrowserDialog
                {
                    Description = description,
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = false
                };
                if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                    dialog.SelectedPath = initialPath;
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                    selected = dialog.SelectedPath;
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return selected;
        });
    }

    /// <summary>
    /// Parses PhotoVault zip names into "yyyy-MM Category" (e.g. photos-july_2026-nature → 2026-07 Nature).
    /// Falls back to the file name without extension.
    /// </summary>
    public static string ExtractPhotoVaultName(string filename)
    {
        filename = Path.GetFileName(filename).ToLowerInvariant();
        var match = Regex.Match(filename, @"photos-([a-z]+)_(\d{4})-([a-z]+)");
        if (match.Success)
        {
            string monthName = match.Groups[1].Value;
            string year = match.Groups[2].Value;
            string category = match.Groups[3].Value;
            string month = monthName switch
            {
                "january" or "januarie" => "01",
                "february" or "februarie" => "02",
                "march" or "maart" => "03",
                "april" => "04",
                "may" or "mei" => "05",
                "june" or "junie" => "06",
                "july" or "julie" => "07",
                "august" or "augustus" => "08",
                "september" => "09",
                "october" or "oktober" => "10",
                "november" => "11",
                "december" or "desember" => "12",
                _ => null
            };
            if (month != null)
                return $"{year}-{month} {category.toCap()}";
        }
        return Path.GetFileNameWithoutExtension(filename);
    }
}
