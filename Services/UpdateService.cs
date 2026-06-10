using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace MeowProof.Services;

public static class UpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/sohaibirfann/MeowProof/releases/latest";

    public static string? AvailableTag { get; private set; }
    public static bool IsUpdateAvailable => AvailableTag != null;

    private static string? _downloadUrl;

    public static event EventHandler? UpdateFound;

    public static async Task CheckAsync()
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MeowProof", "1.0"));

            var json = await http.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString();
            if (tag == null || !IsNewer(tag)) return;

            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                if (asset.GetProperty("name").GetString()?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (_downloadUrl == null) return;

            AvailableTag = tag;
            UpdateFound?.Invoke(null, EventArgs.Empty);
        }
        catch { }
    }

    public static async Task DownloadAndApplyAsync()
    {
        if (_downloadUrl == null) return;

        var tempExe = Path.Combine(Path.GetTempPath(), "MeowProof-update.exe");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MeowProof", "1.0"));
        var bytes = await http.GetByteArrayAsync(_downloadUrl);
        await File.WriteAllBytesAsync(tempExe, bytes);

        var currentExe = System.Windows.Forms.Application.ExecutablePath;
        var pid = Environment.ProcessId;
        var scriptPath = Path.Combine(Path.GetTempPath(), "meowproof-update.ps1");

        // Wait for this process to exit, replace the exe, relaunch, then self-delete.
        // $$"""...""" raw string: {{var}} = interpolated, { } = literal PowerShell braces.
        var script = $$"""
            $deadline = (Get-Date).AddSeconds(15)
            while ((Get-Process -Id {{pid}} -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) {
                Start-Sleep -Milliseconds 500
            }
            Copy-Item -Path '{{tempExe}}' -Destination '{{currentExe}}' -Force
            Start-Process '{{currentExe}}'
            Remove-Item -Path $MyInvocation.MyCommand.Path -Force
            """;

        await File.WriteAllTextAsync(scriptPath, script);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -NonInteractive -File \"{scriptPath}\"",
            UseShellExecute = true,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
        });
    }

    private static bool IsNewer(string tag)
    {
        var s = tag.TrimStart('v');
        if (!Version.TryParse(s, out var remote)) return false;
        var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        return remote > current;
    }
}
