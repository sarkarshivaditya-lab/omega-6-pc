using System.Globalization;
using System.IO;
using UrbanDiagnosticCentre.Data;
using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Services;

public class SettingsService
{
    private readonly AppDbContext _db;
    private AppSettings? _cached;

    private static readonly string AssetsFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "UrbanDiagnosticCentre", "Assets");

    public SettingsService(AppDbContext db) => _db = db;

    // ── Load ──────────────────────────────────────────────────────────────────

    public AppSettings Load()
    {
        if (_cached is not null) return _cached;

        var s = _db.AppSettings.FirstOrDefault();
        if (s is null)
        {
            s = new AppSettings { Id = 1 };
            _db.AppSettings.Add(s);
            _db.SaveChanges();
        }

        _cached = s;
        return s;
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    public void Save(AppSettings incoming)
    {
        // Validate storage paths before persisting
        if (!string.IsNullOrEmpty(incoming.ReportsRootPath))
            ValidateWritablePath(incoming.ReportsRootPath, "Reports folder");

        if (!string.IsNullOrEmpty(incoming.BackupsRootPath))
            ValidateWritablePath(incoming.BackupsRootPath, "Backups folder");

        // Copy logo into managed assets folder so the app is not dependent on the original path
        incoming.LogoPath = CopyLogoToAssets(incoming.LogoPath);

        // Normalize tier name to canonical Title Case before persistence
        incoming.DefaultPriceTier = NormalizeTierName(incoming.DefaultPriceTier);

        incoming.UpdatedAt = DateTime.Now;
        incoming.Id        = 1;

        var existing = _db.AppSettings.FirstOrDefault();
        if (existing is null)
        {
            _db.AppSettings.Add(incoming);
        }
        else
        {
            existing.CentreName          = incoming.CentreName;
            existing.CentreAddress       = incoming.CentreAddress;
            existing.CentrePhone         = incoming.CentrePhone;
            existing.CentreEmail         = incoming.CentreEmail;
            existing.LogoPath            = incoming.LogoPath;
            existing.ReportsRootPath     = incoming.ReportsRootPath;
            existing.BackupsRootPath     = incoming.BackupsRootPath;
            existing.DefaultPriceTier    = incoming.DefaultPriceTier;
            existing.GstTaxId            = incoming.GstTaxId;
            existing.SignatureFooterText                = incoming.SignatureFooterText;
            existing.WatermarkText                      = incoming.WatermarkText;
            existing.LabInchargeName                    = incoming.LabInchargeName;
            existing.LabInchargeSignaturePath           = incoming.LabInchargeSignaturePath;
            existing.ConsultantPathologistName          = incoming.ConsultantPathologistName;
            existing.ConsultantPathologistSignaturePath = incoming.ConsultantPathologistSignaturePath;
            existing.UpdatedAt                          = incoming.UpdatedAt;
        }

        _db.SaveChanges();
        _cached = existing ?? incoming;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Mirrors the normalization in TestDefinitionService so the stored default tier name
    // is always in the same canonical Title Case form as the stored tier rows.
    private static string NormalizeTierName(string name) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Trim().ToLowerInvariant());

    // Throws InvalidOperationException if the directory cannot be created or written to.
    private static void ValidateWritablePath(string path, string fieldName)
    {
        try
        {
            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, $".udc_write_{Guid.NewGuid():N}");
            File.WriteAllText(probe, "probe");
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"{fieldName} is not accessible or writable: {ex.Message}", ex);
        }
    }

    // Copies the logo to the managed assets folder and returns the new path.
    // Falls back to the original path on any error.
    private static string? CopyLogoToAssets(string? logoPath)
    {
        if (string.IsNullOrEmpty(logoPath)) return logoPath;

        if (logoPath.StartsWith(AssetsFolder, StringComparison.OrdinalIgnoreCase))
            return logoPath;  // already in managed folder

        if (!File.Exists(logoPath)) return logoPath;

        try
        {
            Directory.CreateDirectory(AssetsFolder);
            var ext  = Path.GetExtension(logoPath);
            var dest = Path.Combine(AssetsFolder, $"logo{ext}");
            File.Copy(logoPath, dest, overwrite: true);
            return dest;
        }
        catch
        {
            return logoPath;  // best-effort — original path as fallback
        }
    }

    // ── Backup reminder ───────────────────────────────────────────────────────

    // Returns a non-null message when the most recent manual backup is older than 7 days.
    public string? GetBackupReminder()
    {
        var lastDate = _db.BackupRecords
            .Where(b => !b.IsAutoBackup)
            .OrderByDescending(b => b.BackupDate)
            .Select(b => (DateTime?)b.BackupDate)
            .FirstOrDefault();

        if (lastDate is null)
            return "No manual backup has been created yet. Back up your data regularly.";

        var days = (int)(DateTime.Now - lastDate.Value).TotalDays;
        if (days > 7)
            return $"Last manual backup was {days} days ago. Consider creating a new backup soon.";

        return null;
    }
}
