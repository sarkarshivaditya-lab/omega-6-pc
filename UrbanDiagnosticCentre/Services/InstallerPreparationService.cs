using System.IO;
using Microsoft.Win32;
using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Services;

public record DeploymentCheckItem(string Name, bool Passed, string? Detail = null);

public class InstallerPreparationService
{
    private static readonly string AppDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "UrbanDiagnosticCentre");

    // ── Deployment checklist ──────────────────────────────────────────────────
    public List<DeploymentCheckItem> RunDeploymentChecklist() =>
    [
        Check("Application data folder",    () => Directory.Exists(AppDataFolder)),
        Check("Database file",              () => File.Exists(Path.Combine(AppDataFolder, "udc.db"))),
        Check("Reports folder",             () => Directory.Exists(Path.Combine(AppDataFolder, "Reports"))),
        Check("Logs folder",                () => Directory.Exists(Path.Combine(AppDataFolder, "Logs"))),
        Check("Assets folder",              () => Directory.Exists(Path.Combine(AppDataFolder, "Assets"))),
        Check("Application executable",     () => File.Exists(Environment.ProcessPath ?? string.Empty)),
        Check("Start menu shortcut",        HasStartMenuShortcut),
        Check("Uninstall entry registered", HasUninstallEntry),
    ];

    // ── Runtime checklist ─────────────────────────────────────────────────────
    public List<DeploymentCheckItem> RunRuntimeChecklist() =>
    [
        Check(".NET 9+ Runtime",               () => Environment.Version.Major >= 9),
        Check("Windows OS",                    () => OperatingSystem.IsWindows()),
        Check("64-bit process",                () => Environment.Is64BitProcess),
        Check("Write access to %LocalAppData%", CanWriteToAppData),
        Check("PDF viewer registered",         HasPdfViewerRegistered),
        Check("Printer available",             HasAnyPrinterInstalled),
    ];

    // ── First-run detection ───────────────────────────────────────────────────
    public static bool IsFirstRun(AppSettings settings)
        => string.IsNullOrWhiteSpace(settings.CentreName);

    // ── Setup warnings (used by Dashboard to guide admin after login) ─────────
    // Returns a list of actionable messages. Empty means fully configured.
    public static List<string> GetSetupWarnings(AppSettings settings, string reportsRoot)
    {
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.CentreName))
            warnings.Add(
                "Centre name is not configured. PDF reports will not display a centre name. " +
                "Please go to Settings to complete setup.");

        // Verify reports folder is writable
        if (!string.IsNullOrEmpty(reportsRoot))
        {
            try
            {
                Directory.CreateDirectory(reportsRoot);
                var probe = Path.Combine(reportsRoot, $".setup_probe_{Guid.NewGuid():N}");
                File.WriteAllText(probe, "x");
                File.Delete(probe);
            }
            catch
            {
                warnings.Add(
                    $"Reports folder is not writable: {reportsRoot}. " +
                    "PDF generation will fail until this is resolved.");
            }
        }
        else
        {
            // Check the default AppData location
            var defaultRoot = Path.Combine(AppDataFolder, "Reports");
            try
            {
                Directory.CreateDirectory(defaultRoot);
                var probe = Path.Combine(defaultRoot, $".setup_probe_{Guid.NewGuid():N}");
                File.WriteAllText(probe, "x");
                File.Delete(probe);
            }
            catch
            {
                warnings.Add(
                    "Default reports folder is not writable. PDF generation will fail.");
            }
        }

        return warnings;
    }

    // ── First-run setup ───────────────────────────────────────────────────────
    public void EnsureFirstRunDirectories()
    {
        Directory.CreateDirectory(AppDataFolder);
        Directory.CreateDirectory(Path.Combine(AppDataFolder, "Reports"));
        Directory.CreateDirectory(Path.Combine(AppDataFolder, "Logs"));
        Directory.CreateDirectory(Path.Combine(AppDataFolder, "Assets"));
        ErrorLoggingService.LogInfo("First-run directory structure verified.");
    }

    // ── Capability checks (used by runtime checklist + PrintService) ──────────

    public static bool HasPdfViewerRegistered()
    {
        try
        {
            // Check for .pdf extension handler in registry (HKCR\.pdf)
            using var key = Registry.ClassesRoot.OpenSubKey(".pdf");
            return key is not null;
        }
        catch { return false; }
    }

    public static bool HasAnyPrinterInstalled()
    {
        try
        {
            using var server = new System.Printing.LocalPrintServer();
            return server.GetPrintQueues().Any();
        }
        catch { return false; }
    }

    // ── Installer / packaging checks ─────────────────────────────────────────

    private static bool HasStartMenuShortcut()
    {
        var dirs = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                         "Programs", "Urban Diagnostic Centre"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                         "Programs", "Urban Diagnostic Centre"),
        };
        return dirs.Any(d => Directory.Exists(d) && Directory.GetFiles(d, "*.lnk").Length > 0);
    }

    private static bool HasUninstallEntry()
    {
        const string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        try
        {
            foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                using var root = hive.OpenSubKey(uninstallKey);
                if (root is null) continue;
                foreach (var name in root.GetSubKeyNames())
                {
                    using var sub = root.OpenSubKey(name);
                    if (sub?.GetValue("DisplayName") is string displayName &&
                        displayName.Contains("Urban Diagnostic Centre", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }
        catch { /* non-fatal */ }
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool CanWriteToAppData()
    {
        try
        {
            Directory.CreateDirectory(AppDataFolder);
            var probe = Path.Combine(AppDataFolder, $".probe_{Guid.NewGuid():N}");
            File.WriteAllText(probe, "x");
            File.Delete(probe);
            return true;
        }
        catch { return false; }
    }

    private static DeploymentCheckItem Check(string name, Func<bool> test)
    {
        try   { return new DeploymentCheckItem(name, test()); }
        catch (Exception ex) { return new DeploymentCheckItem(name, false, ex.Message); }
    }
}
