using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrbanDiagnosticCentre.Data;

namespace UrbanDiagnosticCentre.Services;

public record MaintenanceResult(bool Success, string Message, List<string> Details)
{
    public MaintenanceResult(bool success, string message) : this(success, message, []) { }
}

public class MaintenanceService
{
    private readonly AppDbContext    _db;
    private readonly SettingsService _settingsService;

    private static readonly string AppDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "UrbanDiagnosticCentre");

    public MaintenanceService(AppDbContext db, SettingsService settingsService)
    {
        _db              = db;
        _settingsService = settingsService;
    }

    private string ReportsRoot
    {
        get
        {
            var s = _settingsService.Load();
            return !string.IsNullOrEmpty(s.ReportsRootPath)
                ? s.ReportsRootPath
                : Path.Combine(AppDataFolder, "Reports");
        }
    }

    // ── VACUUM ────────────────────────────────────────────────────────────────

    public MaintenanceResult RunVacuum()
    {
        try
        {
            _db.Database.ExecuteSqlRaw("VACUUM;");
            return new MaintenanceResult(true, "Database vacuumed successfully.");
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, "VACUUM");
            return new MaintenanceResult(false, $"VACUUM failed: {ex.Message}");
        }
    }

    // ── Integrity check ───────────────────────────────────────────────────────

    public MaintenanceResult RunIntegrityCheck()
    {
        try
        {
            var connString = _db.Database.GetConnectionString();
            using var conn = new SqliteConnection(connString);
            conn.Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = cmd.ExecuteScalar()?.ToString();

            return result == "ok"
                ? new MaintenanceResult(true,  "Database integrity check passed.")
                : new MaintenanceResult(false, $"Integrity check failed: {result}");
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, "IntegrityCheck");
            return new MaintenanceResult(false, $"Integrity check error: {ex.Message}");
        }
    }

    // ── Orphan PDFs (on disk, not in DB) ──────────────────────────────────────

    public MaintenanceResult FindOrphanPdfs()
    {
        try
        {
            var settings = _settingsService.Load();
            var knownPaths = _db.Reports
                .Where(r => r.PdfPath != null && r.PdfPath != string.Empty)
                .Select(r => r.PdfPath!)
                .ToList()
                .Select(p => PdfReportService.ResolvePath(p, settings))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var orphans = new List<string>();
            if (Directory.Exists(ReportsRoot))
            {
                foreach (var file in Directory.GetFiles(ReportsRoot, "*.pdf", SearchOption.AllDirectories))
                {
                    if (!knownPaths.Contains(file))
                        orphans.Add(file);
                }
            }

            return new MaintenanceResult(
                true,
                orphans.Count == 0 ? "No orphan PDFs found." : $"{orphans.Count} orphan PDF(s) found.",
                orphans);
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, "OrphanPdfs");
            return new MaintenanceResult(false, $"Orphan check error: {ex.Message}");
        }
    }

    // ── Missing PDFs (in DB, file absent on disk) ─────────────────────────────

    public MaintenanceResult FindMissingPdfs()
    {
        try
        {
            var settings = _settingsService.Load();
            var missing = _db.Reports
                .Where(r => r.PdfPath != null && r.PdfPath != string.Empty)
                .Select(r => new { r.ReportCode, r.PdfPath })
                .ToList()
                .Where(r => !File.Exists(PdfReportService.ResolvePath(r.PdfPath!, settings)))
                .Select(r => $"{r.ReportCode}: {r.PdfPath}")
                .ToList();

            return new MaintenanceResult(
                true,
                missing.Count == 0 ? "All PDF files are present." : $"{missing.Count} missing PDF(s) found.",
                missing);
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, "MissingPdfs");
            return new MaintenanceResult(false, $"Missing PDF check error: {ex.Message}");
        }
    }
}
