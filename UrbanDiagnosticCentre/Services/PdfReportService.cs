using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Services;

public static class PdfReportService
{
    static PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static string Generate(Report report, AppSettings settings)
    {
        var root = string.IsNullOrWhiteSpace(settings.ReportsRootPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Omega6.0", "Documents")
            : settings.ReportsRootPath;
        var dir = Path.Combine(root, report.TestDate.ToString("yyyy"), report.TestDate.ToString("MM"), report.TestDate.ToString("dd"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{report.ReportCode}.pdf");
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));
            page.Header().Element(c => ComposeHeader(c, settings));
            page.Content().Element(c => ComposeContent(c, report));
            page.Footer().Element(c => ComposeFooter(c, report));
        })).GeneratePdf(path);
        return path;
    }

    private static void ComposeHeader(IContainer c, AppSettings settings)
    {
        var name = string.IsNullOrWhiteSpace(settings.CentreName) ? "OMEGA 6.0" : settings.CentreName;
        c.BorderBottom(1).BorderColor("#CFD8DC").PaddingBottom(12).Row(row =>
        {
            if (!string.IsNullOrEmpty(settings.LogoPath) && File.Exists(settings.LogoPath))
            {
                try { row.ConstantItem(72).Height(60).Padding(4).Image(File.ReadAllBytes(settings.LogoPath), ImageScaling.FitArea); } catch { }
            }
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(name).FontSize(20).Bold().FontColor("#0D47A1");
                if (!string.IsNullOrWhiteSpace(settings.CentreAddress)) col.Item().Text(settings.CentreAddress).FontSize(8).FontColor("#757575");
                var contact = new[] { settings.CentrePhone, settings.CentreEmail }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                if (contact.Length > 0) col.Item().Text(string.Join("  |  ", contact)).FontSize(8).FontColor("#757575");
            });
            row.ConstantItem(150).AlignRight().Column(col =>
            {
                col.Item().Text("CUSTOMER RECORD").FontSize(12).Bold().FontColor("#1565C0");
                col.Item().Text($"Created: {DateTime.Now:dd MMM yyyy}").FontSize(9).FontColor("#757575");
            });
        });
    }

    private static void ComposeContent(IContainer c, Report report)
    {
        c.PaddingTop(16).Column(col =>
        {
            col.Item().Background("#F5F7FA").Border(1).BorderColor("#CFD8DC").Padding(14).Column(info =>
            {
                info.Item().Text("CUSTOMER INFORMATION").FontSize(8).Bold().FontColor("#757575").LetterSpacing(1);
                info.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(d => { InfoRow(d, "Name", report.Patient.FullName); InfoRow(d, "Contact", report.Patient.PhoneNumber); });
                    row.RelativeItem().Column(d => { InfoRow(d, "Code", report.ReportCode); InfoRow(d, "Reference", report.Patient.Age.ToString()); });
                    row.RelativeItem().Column(d => { InfoRow(d, "Owner", string.IsNullOrWhiteSpace(report.Patient.ReferringDoctor) ? "—" : report.Patient.ReferringDoctor); InfoRow(d, "Address", string.IsNullOrWhiteSpace(report.Patient.Address) ? "—" : report.Patient.Address); });
                });
            });
            col.Item().PaddingTop(20).Text("SELECTED SERVICES").FontSize(8).Bold().FontColor("#757575").LetterSpacing(1);
            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(3); cols.RelativeColumn(1.5f); cols.RelativeColumn(1); cols.RelativeColumn(1); });
                table.Header(h =>
                {
                    h.Cell().Background("#1565C0").Padding(7).Text("Service").FontSize(9).Bold().FontColor("#FFFFFF");
                    h.Cell().Background("#1565C0").Padding(7).Text("Value").FontSize(9).Bold().FontColor("#FFFFFF");
                    h.Cell().Background("#1565C0").Padding(7).Text("Format").FontSize(9).Bold().FontColor("#FFFFFF");
                    h.Cell().Background("#1565C0").Padding(7).Text("Price").FontSize(9).Bold().FontColor("#FFFFFF");
                });
                foreach (var entry in report.Entries)
                {
                    var td = entry.TestDefinition;
                    table.Cell().Padding(7).Text(td.TestName).FontSize(10);
                    table.Cell().Padding(7).Text(entry.ResultValue ?? "—").FontSize(10).Bold();
                    table.Cell().Padding(7).Text(td.Unit ?? "—").FontSize(9).FontColor("#757575");
                    table.Cell().Padding(7).Text(entry.ChargedPrice.HasValue ? entry.ChargedPrice.Value.ToString("F2") : "—").FontSize(10);
                }
            });
            col.Item().PaddingTop(18).AlignRight().Text($"TOTAL: {report.Entries.Sum(e => e.ChargedPrice ?? 0m):F2}").FontSize(12).Bold().FontColor("#1A237E");
        });
    }

    private static void InfoRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(72).Text(label).FontSize(8).FontColor("#757575");
            row.RelativeItem().Text(value).FontSize(9).Bold();
        });
    }

    private static void ComposeFooter(IContainer c, Report report)
    {
        c.BorderTop(1).BorderColor("#CFD8DC").PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text("Generated by OMEGA 6.0").FontSize(8).FontColor("#757575");
            row.ConstantItem(150).AlignRight().Text($"Record: {report.ReportCode}").FontSize(8).FontColor("#757575");
        });
    }

    public static string ResolvePath(string? storedPath, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) return string.Empty;
        if (Path.IsPathRooted(storedPath)) return storedPath;
        var root = string.IsNullOrWhiteSpace(settings.ReportsRootPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Omega6.0", "Documents")
            : settings.ReportsRootPath;
        return Path.Combine(root, storedPath);
    }
}