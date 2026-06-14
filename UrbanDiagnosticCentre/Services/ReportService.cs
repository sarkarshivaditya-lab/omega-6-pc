using System.Diagnostics;
using System.IO;
using Microsoft.EntityFrameworkCore;
using UrbanDiagnosticCentre.Data;
using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Services;

// ── Lightweight query result types ────────────────────────────────────────────

public record DashboardStats(int TodayPatients, int TodayTests, int ReadyReports, int PendingReports);

public record ReportSummary(
    int Id,
    string ReportCode,
    string PatientName,
    DateTime TestDate,
    ReportStatus Status,
    int EntryCount);

public class ReportHistoryRow
{
    public int         ReportId       { get; init; }
    public string      ReportCode     { get; init; } = string.Empty;
    public string      PatientName    { get; init; } = string.Empty;
    public string      PhoneNumber    { get; init; } = string.Empty;
    public int         Age            { get; init; }
    public Gender      Gender         { get; init; }
    public DateTime    TestDate       { get; init; }
    public ReportStatus Status        { get; init; }
    public bool        HasPdf         { get; init; }
    public string?     PdfPath        { get; init; }
    public int         EntryCount     { get; init; }
    public DateTime    CreatedAt      { get; init; }
    public DateTime?   PdfGeneratedAt { get; init; }

    public string GenderDisplay => Gender.ToString();
    public string StatusDisplay => Status.ToString();
}

// ── Service ───────────────────────────────────────────────────────────────────

public class ReportService
{
    private readonly AppDbContext    _db;
    private readonly SettingsService _settingsService;

    private static readonly string _defaultReportsRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "UrbanDiagnosticCentre", "Reports");

    private string ReportsRoot
    {
        get
        {
            var s = _settingsService.Load();
            return !string.IsNullOrEmpty(s.ReportsRootPath) ? s.ReportsRootPath : _defaultReportsRoot;
        }
    }

    // Resolves a stored path (relative or legacy absolute) to the full filesystem path.
    private string ResolvePdfPath(string? pdfPath) =>
        PdfReportService.ResolvePath(pdfPath, _settingsService.Load());

    // Throws if the report has already been finalised and must not be mutated.
    private static void ThrowIfLocked(Report report)
    {
        if (report.Status != ReportStatus.Draft)
            throw new InvalidOperationException(
                $"Report {report.ReportCode} is {report.Status} and cannot be modified.");
    }

    // Throws a friendly exception when a report with the same code already exists in the DB.
    // Prevents accidental resubmission and surfaces a readable message instead of a raw DB error.
    private void ThrowIfDuplicateCode(string reportCode)
    {
        if (_db.Reports.Any(r => r.ReportCode == reportCode))
            throw new InvalidOperationException(
                $"A report with code {reportCode} already exists. " +
                "This can happen if the form was submitted more than once. " +
                "Please go back to the Dashboard and start a new report.");
    }

    // Exposes the resolved reports root for callers that need it (e.g. disk space check).
    public string GetReportsRoot() => ReportsRoot;

    public ReportService(AppDbContext db, SettingsService settingsService)
    {
        _db              = db;
        _settingsService = settingsService;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public void Save(Patient patient, Report report, List<ReportEntry> entries)
    {
        ThrowIfDuplicateCode(report.ReportCode);

        patient.Reports.Add(report);
        foreach (var entry in entries)
            report.Entries.Add(entry);

        _db.Patients.Add(patient);
        _db.SaveChanges();
    }

    // Saves patient + entries + generates PDF + transitions to Completed — all in one
    // EF Core transaction. Rolls back DB and deletes any partial PDF on failure.
    //
    // All DbContext operations (SaveChanges, Commit) execute on the calling thread.
    // Only PdfReportService.Generate — which is static and accesses no DbContext — runs
    // on a thread-pool thread via Task.Run, keeping the UI responsive during rendering.
    public async Task<string> FinalizeReportAsync(Patient patient, Report report, List<ReportEntry> entries)
    {
        ThrowIfDuplicateCode(report.ReportCode);

        // Validate required setup before starting any DB or file work
        var settings = _settingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.CentreName))
            throw new InvalidOperationException(
                "Centre name is not configured. " +
                "Please go to Settings → Centre Details and enter your centre name before generating reports.");

        report.Status = ReportStatus.Draft;  // normalize; transition to Completed happens inside

        string? generatedPath = null;
        var root = !string.IsNullOrEmpty(settings.ReportsRootPath) ? settings.ReportsRootPath : _defaultReportsRoot;

        using var tx = _db.Database.BeginTransaction();
        try
        {
            patient.Reports.Add(report);
            foreach (var entry in entries)
                report.Entries.Add(entry);
            _db.Patients.Add(patient);
            _db.SaveChanges();  // assigns report.Id; EF Core relationship fixup populates navigation properties

            Directory.CreateDirectory(root);  // ensure destination exists before write

            // PdfReportService.Generate is static and holds no DbContext reference.
            // The report object is fully populated after the SaveChanges above.
            // The continuation is marshalled back to the UI SynchronizationContext by await,
            // so the second SaveChanges and Commit below run on the original thread.
            generatedPath = await Task.Run(() => PdfReportService.Generate(report, settings));

            report.PdfPath           = Path.GetRelativePath(root, generatedPath).Replace('\\', '/');
            report.ReportGeneratedAt = DateTime.Now;
            report.PdfVersion        = 1;
            report.Status            = ReportStatus.Completed;
            _db.SaveChanges();

            tx.Commit();
            return generatedPath;
        }
        catch
        {
            // IDbContextTransaction.Dispose() auto-rolls back if not committed
            if (generatedPath is not null)
                try { File.Delete(generatedPath); } catch { }
            throw;
        }
    }

    // ── Edit existing draft ───────────────────────────────────────────────────

    // Updates patient info, report metadata, and entries for an existing Draft in place.
    public void UpdateDraft(int reportId, Patient updatedPatient, Report updatedReportData, List<ReportEntry> updatedEntries)
    {
        var report = _db.Reports
            .Include(r => r.Patient)
            .Include(r => r.Entries)
            .FirstOrDefault(r => r.Id == reportId);
        if (report is null)
            throw new InvalidOperationException($"Report {reportId} not found.");
        ThrowIfLocked(report);

        report.Patient.FullName        = updatedPatient.FullName;
        report.Patient.Age             = updatedPatient.Age;
        report.Patient.Gender          = updatedPatient.Gender;
        report.Patient.PhoneNumber     = updatedPatient.PhoneNumber;
        report.Patient.ReferringDoctor = updatedPatient.ReferringDoctor;
        report.Patient.Address         = updatedPatient.Address;

        report.TestDate            = updatedReportData.TestDate;
        report.ReportDate          = updatedReportData.ReportDate;
        report.PriceTierName       = updatedReportData.PriceTierName;
        report.ModifiedAt          = DateTime.Now;
        report.ModifiedByUserId    = updatedReportData.ModifiedByUserId;
        report.BillingMode         = updatedReportData.BillingMode;
        report.PackageNameSnapshot = updatedReportData.PackageNameSnapshot;
        report.PackageTotalPrice   = updatedReportData.PackageTotalPrice;

        var oldEntries = report.Entries.ToList();
        _db.ReportEntries.RemoveRange(oldEntries);
        report.Entries.Clear();
        foreach (var entry in updatedEntries)
            report.Entries.Add(entry);

        _db.SaveChanges();
    }

    // Updates a Draft, generates its PDF, and transitions it to Completed — atomically.
    public async Task<string> FinalizeDraftReportAsync(int reportId, Patient updatedPatient, Report updatedReportData, List<ReportEntry> updatedEntries)
    {
        var report = _db.Reports
            .Include(r => r.Patient)
            .Include(r => r.Entries)
            .Include(r => r.CreatedByUser)
            .FirstOrDefault(r => r.Id == reportId);
        if (report is null)
            throw new InvalidOperationException($"Report {reportId} not found.");
        ThrowIfLocked(report);

        var settings = _settingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.CentreName))
            throw new InvalidOperationException(
                "Centre name is not configured. " +
                "Please go to Settings → Centre Details and enter your centre name before generating reports.");

        string? generatedPath = null;
        var root = !string.IsNullOrEmpty(settings.ReportsRootPath) ? settings.ReportsRootPath : _defaultReportsRoot;

        using var tx = _db.Database.BeginTransaction();
        try
        {
            report.Patient.FullName        = updatedPatient.FullName;
            report.Patient.Age             = updatedPatient.Age;
            report.Patient.Gender          = updatedPatient.Gender;
            report.Patient.PhoneNumber     = updatedPatient.PhoneNumber;
            report.Patient.ReferringDoctor = updatedPatient.ReferringDoctor;
            report.Patient.Address         = updatedPatient.Address;

            report.TestDate            = updatedReportData.TestDate;
            report.ReportDate          = updatedReportData.ReportDate;
            report.PriceTierName       = updatedReportData.PriceTierName;
            report.ModifiedAt          = DateTime.Now;
            report.ModifiedByUserId    = updatedReportData.ModifiedByUserId;
            report.BillingMode         = updatedReportData.BillingMode;
            report.PackageNameSnapshot = updatedReportData.PackageNameSnapshot;
            report.PackageTotalPrice   = updatedReportData.PackageTotalPrice;

            var oldEntries = report.Entries.ToList();
            _db.ReportEntries.RemoveRange(oldEntries);
            report.Entries.Clear();
            foreach (var entry in updatedEntries)
                report.Entries.Add(entry);

            _db.SaveChanges();

            Directory.CreateDirectory(root);
            generatedPath = await Task.Run(() => PdfReportService.Generate(report, settings));

            report.PdfPath           = Path.GetRelativePath(root, generatedPath).Replace('\\', '/');
            report.ReportGeneratedAt = DateTime.Now;
            report.PdfVersion        = report.PdfVersion + 1;
            report.Status            = ReportStatus.Completed;
            _db.SaveChanges();

            tx.Commit();
            return generatedPath;
        }
        catch
        {
            if (generatedPath is not null)
                try { File.Delete(generatedPath); } catch { }
            throw;
        }
    }

    // ── Mutation guards ───────────────────────────────────────────────────────

    // Archives a report (hides from active views). Non-destructive and always allowed.
    public void ArchiveReport(int reportId)
    {
        var report = _db.Reports.Find(reportId);
        if (report is null || report.IsArchived) return;
        report.IsArchived = true;
        report.ArchivedAt = DateTime.Now;
        _db.SaveChanges();
    }

    // Permanently deletes a Draft report and its patient if no other reports remain.
    // Throws InvalidOperationException if the report is Completed or Printed.
    public void DeleteDraftReport(int reportId)
    {
        var report = _db.Reports
            .Include(r => r.Patient).ThenInclude(p => p.Reports)
            .FirstOrDefault(r => r.Id == reportId);

        if (report is null) return;
        ThrowIfLocked(report);

        using var tx = _db.Database.BeginTransaction();
        try
        {
            var removePatient = report.Patient.Reports.Count == 1;
            _db.Reports.Remove(report);  // entries cascade-deleted by FK constraint
            if (removePatient)
                _db.Patients.Remove(report.Patient);
            _db.SaveChanges();
            tx.Commit();
        }
        catch
        {
            throw;
        }
    }

    // ── Disk space ────────────────────────────────────────────────────────────

    // Returns a warning message when the reports drive has less than thresholdMb free.
    // Returns null when space is adequate.
    public string? GetDiskSpaceWarning(long thresholdMb = 500)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(ReportsRoot));
            if (root is null) return null;
            var drive = new DriveInfo(root);
            if (drive.IsReady && drive.AvailableFreeSpace < thresholdMb * 1_048_576L)
                return $"Reports drive is low on space: {drive.AvailableFreeSpace / 1_048_576} MB remaining.";
        }
        catch { /* non-fatal */ }
        return null;
    }

    // ── Dashboard stats ───────────────────────────────────────────────────────

    public DashboardStats GetTodayStats()
    {
        var today    = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var todayReports = _db.Reports
            .Where(r => !r.IsArchived && r.CreatedAt >= today && r.CreatedAt < tomorrow)
            .ToList();

        return new DashboardStats(
            TodayPatients:  todayReports.Select(r => r.PatientId).Distinct().Count(),
            TodayTests:     _db.ReportEntries.Count(e =>
                                e.Report.CreatedAt >= today && e.Report.CreatedAt < tomorrow && !e.Report.IsArchived),
            ReadyReports:   _db.Reports.Count(r => !r.IsArchived &&
                                (r.Status == ReportStatus.Completed || r.Status == ReportStatus.Printed)),
            PendingReports: _db.Reports.Count(r => !r.IsArchived && r.Status == ReportStatus.Draft)
        );
    }

    // ── Recent reports (dashboard) ────────────────────────────────────────────

    public List<ReportSummary> GetRecent(int count = 10)
    {
        return _db.Reports
            .Where(r => !r.IsArchived)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .Select(r => new ReportSummary(
                r.Id,
                r.ReportCode,
                r.Patient.FullName,
                r.TestDate,
                r.Status,
                r.Entries.Count))
            .ToList();
    }

    // ── Search (history screen) ───────────────────────────────────────────────

    public List<ReportHistoryRow> Search(
        string?      query,
        DateTime?    from,
        DateTime?    to,
        ReportStatus? status,
        Gender?      gender)
    {
        var q = _db.Reports
            .Where(r => !r.IsArchived)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            q = q.Where(r =>
                r.ReportCode.ToLower().Contains(term) ||
                r.Patient.FullName.ToLower().Contains(term) ||
                r.Patient.PhoneNumber.Contains(term));
        }

        if (from.HasValue)
            q = q.Where(r => r.TestDate >= from.Value.Date);

        if (to.HasValue)
            q = q.Where(r => r.TestDate < to.Value.Date.AddDays(1));

        if (status.HasValue)
            q = q.Where(r => r.Status == status.Value);

        if (gender.HasValue)
            q = q.Where(r => r.Patient.Gender == gender.Value);

        // Materialise first, then project display-only properties in .NET
        var raw = q
            .OrderByDescending(r => r.TestDate)
            .ThenByDescending(r => r.CreatedAt)
            .Take(200)
            .Select(r => new
            {
                r.Id,
                r.ReportCode,
                PatientName    = r.Patient.FullName,
                PhoneNumber    = r.Patient.PhoneNumber,
                Age            = r.Patient.Age,
                Gender         = r.Patient.Gender,
                r.TestDate,
                r.Status,
                HasPdf         = r.PdfPath != null && r.PdfPath != string.Empty,
                r.PdfPath,
                EntryCount     = r.Entries.Count,
                r.CreatedAt,
                r.ReportGeneratedAt
            })
            .ToList();

        return raw.Select(x => new ReportHistoryRow
        {
            ReportId       = x.Id,
            ReportCode     = x.ReportCode,
            PatientName    = x.PatientName,
            PhoneNumber    = x.PhoneNumber,
            Age            = x.Age,
            Gender         = x.Gender,
            TestDate       = x.TestDate,
            Status         = x.Status,
            HasPdf         = x.HasPdf,
            PdfPath        = x.PdfPath,
            EntryCount     = x.EntryCount,
            CreatedAt      = x.CreatedAt,
            PdfGeneratedAt = x.ReportGeneratedAt
        }).ToList();
    }

    // ── Single report (full load) ─────────────────────────────────────────────

    public Report? GetById(int id)
    {
        return _db.Reports
            .Include(r => r.Patient)
            .Include(r => r.Entries).ThenInclude(e => e.TestDefinition)
            .Include(r => r.CreatedByUser)
            .FirstOrDefault(r => r.Id == id);
    }

    // ── PDF lifecycle ─────────────────────────────────────────────────────────

    // Generates the PDF (Draft→Completed), stores a relative path, and returns the
    // resolved absolute path for immediate use. Never regenerates an existing PDF.
    public string EnsurePdfGenerated(int reportId)
    {
        var report = GetById(reportId);
        if (report is null) return string.Empty;

        if (!string.IsNullOrEmpty(report.PdfPath))
            return ResolvePdfPath(report.PdfPath);  // already generated

        var settings = _settingsService.Load();
        var root     = !string.IsNullOrEmpty(settings.ReportsRootPath) ? settings.ReportsRootPath : _defaultReportsRoot;
        Directory.CreateDirectory(root);  // ensure destination exists before write
        var fullPath = PdfReportService.Generate(report, settings);

        // Store as path relative to ReportsRoot (e.g. "2026/05/27/RPT001.pdf").
        // Legacy absolute paths written before this change are preserved by ResolvePdfPath.
        var relativePath = Path.GetRelativePath(root, fullPath).Replace('\\', '/');

        report.PdfPath           = relativePath;
        report.ReportGeneratedAt = DateTime.Now;
        report.PdfVersion        = 1;
        report.Status            = ReportStatus.Completed;
        _db.Reports.Update(report);
        _db.SaveChanges();

        return fullPath;
    }

    // Opens the existing PDF (or generates it first if somehow missing).
    // Updates PrintedAt timestamp and transitions to Printed.
    public void OpenPdf(int reportId)
    {
        var fullPath = EnsurePdfGenerated(reportId);
        if (string.IsNullOrEmpty(fullPath)) return;

        var report = _db.Reports.Find(reportId);
        if (report is not null)
        {
            report.PrintedAt = DateTime.Now;
            if (report.Status == ReportStatus.Completed)
                report.Status = ReportStatus.Printed;
            _db.SaveChanges();
        }

        Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
    }

    // Sends the PDF to the default printer (falls back to open if verb unsupported).
    public void PrintPdf(int reportId)
    {
        var fullPath = EnsurePdfGenerated(reportId);
        if (string.IsNullOrEmpty(fullPath)) return;

        var report = _db.Reports.Find(reportId);
        if (report is not null)
        {
            report.PrintedAt = DateTime.Now;
            if (report.Status == ReportStatus.Completed)
                report.Status = ReportStatus.Printed;
            _db.SaveChanges();
        }

        try
        {
            Process.Start(new ProcessStartInfo(fullPath)
            {
                UseShellExecute = true,
                Verb            = "print"
            });
        }
        catch
        {
            Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
        }
    }
}
