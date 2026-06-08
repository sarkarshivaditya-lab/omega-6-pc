using System.Diagnostics;
using System.IO;

namespace UrbanDiagnosticCentre.Services;

public class PrintService
{
    private readonly ReportService _reportService;

    public PrintService(ReportService reportService)
    {
        _reportService = reportService;
    }

    public void OpenPdf(int reportId, string reportCode)
    {
        var path = ResolveAndValidate(reportId, reportCode);
        if (path is null) return;

        WarnIfNoPdfViewer();

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, $"PrintService.Open[{reportCode}]");
            DialogService.ShowPdfOpenError(reportCode, ex.Message);
        }
    }

    public void PrintPdf(int reportId, string reportCode)
    {
        var path = ResolveAndValidate(reportId, reportCode);
        if (path is null) return;

        if (!InstallerPreparationService.HasAnyPrinterInstalled())
        {
            DialogService.ShowWarning(
                "No printer appears to be installed or available on this computer.\n\n" +
                "The PDF will be opened for manual printing instead.",
                "No Printer Available");
            // Fall through to open the file
        }

        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "print"
            });
            return;
        }
        catch { /* print verb not supported — fall back to open */ }

        // Fallback: open in viewer
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, $"PrintService.Print[{reportCode}]");
            DialogService.ShowPdfOpenError(reportCode, ex.Message);
        }
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private string? ResolveAndValidate(int reportId, string reportCode)
    {
        string path;
        try
        {
            path = _reportService.EnsurePdfGenerated(reportId);
        }
        catch (Exception ex)
        {
            ErrorLoggingService.Log(ex, $"PrintService.EnsurePdf[{reportCode}]");
            DialogService.ShowError(
                $"Could not generate PDF for report {reportCode}.\n\n{ex.Message}",
                "PDF Generation Failed");
            return null;
        }

        if (string.IsNullOrEmpty(path))
        {
            DialogService.ShowPdfNotFound(reportCode);
            return null;
        }

        if (!File.Exists(path))
        {
            ErrorLoggingService.LogInfo($"PDF file missing on disk for {reportCode}: {path}");
            DialogService.ShowError(
                $"The PDF file for report {reportCode} could not be found on disk.\n\n" +
                $"Expected location: {path}\n\n" +
                "The file may have been moved or deleted. Contact your system administrator.",
                "PDF File Not Found");
            return null;
        }

        return path;
    }

    private static void WarnIfNoPdfViewer()
    {
        if (!InstallerPreparationService.HasPdfViewerRegistered())
        {
            DialogService.ShowWarning(
                "No PDF viewer appears to be registered on this computer.\n\n" +
                "Please install a PDF viewer such as Adobe Acrobat Reader or Microsoft Edge " +
                "to view generated reports.",
                "PDF Viewer Not Found");
        }
    }
}
