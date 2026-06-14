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

    // ── Public entry point ────────────────────────────────────────────────────
    // Generates a PDF using the current branding settings.
    // Caller must verify no PDF already exists before calling this.
    public static string Generate(Report report, AppSettings settings)
    {
        var reportsRoot = string.IsNullOrEmpty(settings.ReportsRootPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UrbanDiagnosticCentre", "Reports")
            : settings.ReportsRootPath;

        var dir = Path.Combine(reportsRoot,
            report.TestDate.ToString("yyyy"),
            report.TestDate.ToString("MM"),
            report.TestDate.ToString("dd"));

        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{report.ReportCode}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, settings));
                page.Content().Element(c => ComposeContent(c, report, settings));
                page.Footer().Element(c => ComposeFooter(c, report, settings));
            });
        }).GeneratePdf(path);

        return path;
    }

    // ── Header ────────────────────────────────────────────────────────────────
    private static void ComposeHeader(IContainer c, AppSettings settings)
    {
        var centreName = string.IsNullOrWhiteSpace(settings.CentreName)
            ? "Urban Diagnostic Centre"
            : settings.CentreName;

        c.BorderBottom(1).BorderColor("#CFD8DC").PaddingBottom(12).Row(row =>
        {
            // Optional logo
            if (!string.IsNullOrEmpty(settings.LogoPath) && File.Exists(settings.LogoPath))
            {
                try
                {
                    var logoBytes = File.ReadAllBytes(settings.LogoPath);
                    row.ConstantItem(72).Height(60).Padding(4)
                       .Image(logoBytes, ImageScaling.FitArea);
                }
                catch { /* logo unreadable — skip gracefully */ }
            }

            // Centre name + contact details
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(centreName).FontSize(20).Bold().FontColor("#0D47A1");

                if (!string.IsNullOrEmpty(settings.CentreAddress))
                    col.Item().Text(settings.CentreAddress).FontSize(8).FontColor("#757575");

                var contactParts = new[] { settings.CentrePhone, settings.CentreEmail }
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
                if (contactParts.Length > 0)
                    col.Item().Text(string.Join("  |  ", contactParts)).FontSize(8).FontColor("#757575");

                if (!string.IsNullOrEmpty(settings.GstTaxId))
                    col.Item().Text($"GST/Tax ID: {settings.GstTaxId}").FontSize(8).FontColor("#757575");

                if (string.IsNullOrEmpty(settings.CentreAddress) &&
                    contactParts.Length == 0 &&
                    string.IsNullOrEmpty(settings.GstTaxId))
                    col.Item().Text("Accurate. Reliable. Trusted.")
                       .FontSize(9).FontColor("#757575").Italic();
            });

            // Report label
            row.ConstantItem(160).AlignRight().Column(col =>
            {
                col.Item().Text("DIAGNOSTIC REPORT").FontSize(12).Bold().FontColor("#1565C0");
                col.Item().Text($"Report Date: {DateTime.Now:dd MMM yyyy}").FontSize(9).FontColor("#757575");
            });
        });
    }

    // ── Content ───────────────────────────────────────────────────────────────
    private static void ComposeContent(IContainer c, Report report, AppSettings settings)
    {
        var showPrice = report.Entries.Any(e => e.ChargedPrice.HasValue);
        var totalCharge = showPrice ? report.Entries.Sum(e => e.ChargedPrice ?? 0m) : (decimal?)null;

        c.PaddingTop(16).Column(col =>
        {
            // Patient info card
            col.Item()
               .Background("#F5F7FA").Border(1).BorderColor("#CFD8DC").Padding(14)
               .Column(info =>
               {
                   info.Item().Text("PATIENT INFORMATION")
                       .FontSize(8).Bold().FontColor("#757575").LetterSpacing(1);

                   info.Item().PaddingTop(8).Row(row =>
                   {
                       row.RelativeItem().Column(d =>
                       {
                           InfoRow(d, "Name",   report.Patient.FullName);
                           InfoRow(d, "Age",    $"{report.Patient.Age} years");
                           InfoRow(d, "Gender", report.Patient.Gender.ToString());
                       });
                       row.RelativeItem().Column(d =>
                       {
                           InfoRow(d, "Phone",   report.Patient.PhoneNumber);
                           InfoRow(d, "Doctor",  string.IsNullOrWhiteSpace(report.Patient.ReferringDoctor) ? "—" : report.Patient.ReferringDoctor);
                           InfoRow(d, "Address", string.IsNullOrWhiteSpace(report.Patient.Address) ? "—" : report.Patient.Address);
                       });
                       row.RelativeItem().Column(d =>
                       {
                           InfoRow(d, "Report Code", report.ReportCode);
                           InfoRow(d, "Test Date",   report.TestDate.ToString("dd MMM yyyy, hh:mm tt"));
                           if (!string.IsNullOrEmpty(report.PriceTierName))
                               InfoRow(d, "Price Category", report.PriceTierName);
                       });
                   });
               });

            col.Item().PaddingTop(20).Text("TEST RESULTS")
                .FontSize(8).Bold().FontColor("#757575").LetterSpacing(1);

            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1.2f);
                    cols.RelativeColumn(1);
                    if (showPrice) cols.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Background("#1565C0").Padding(7)
                          .Text("Test Name").FontSize(9).Bold().FontColor("#FFFFFF");
                    header.Cell().Background("#1565C0").Padding(7)
                          .Text("Reference Range").FontSize(9).Bold().FontColor("#FFFFFF");
                    header.Cell().Background("#1565C0").Padding(7)
                          .Text("Result").FontSize(9).Bold().FontColor("#FFFFFF");
                    header.Cell().Background("#1565C0").Padding(7)
                          .Text("Flag").FontSize(9).Bold().FontColor("#FFFFFF");
                    if (showPrice)
                        header.Cell().Background("#1565C0").Padding(7)
                              .Text("Price").FontSize(9).Bold().FontColor("#FFFFFF");
                });

                bool shade = false;
                foreach (var entry in report.Entries)
                {
                    var bg = shade ? "#F9FAFB" : "#FFFFFF";
                    shade = !shade;

                    var (flagText, flagColor) = entry.ResultFlag switch
                    {
                        ResultFlag.Normal   => ("Normal",   "#2E7D32"),
                        ResultFlag.Low      => ("↓ Low",    "#1565C0"),
                        ResultFlag.High     => ("↑ High",   "#C62828"),
                        ResultFlag.Critical => ("Critical", "#B71C1C"),
                        _                  => ("—",         "#9E9E9E")
                    };

                    var td = entry.TestDefinition;

                    table.Cell().Background(bg).Padding(7).Text(td.TestName).FontSize(10);
                    table.Cell().Background(bg).Padding(7).Text(entry.ReferenceRangeUsed).FontSize(9).FontColor("#757575");
                    table.Cell().Background(bg).Padding(7).Text($"{entry.ResultValue} {td.Unit}").FontSize(10).Bold();
                    table.Cell().Background(bg).Padding(7).Text(flagText).FontSize(10).FontColor(flagColor).Bold();
                    if (showPrice)
                        table.Cell().Background(bg).Padding(7)
                             .Text(entry.ChargedPrice.HasValue ? entry.ChargedPrice.Value.ToString("F2") : "—")
                             .FontSize(10);
                }

                // Total row
                if (showPrice && totalCharge.HasValue)
                {
                    table.Cell().ColumnSpan(4).Background("#E8EAF6").Padding(7)
                         .AlignRight().Text("Total:").FontSize(10).Bold().FontColor("#1A237E");
                    table.Cell().Background("#E8EAF6").Padding(7)
                         .Text(totalCharge.Value.ToString("F2")).FontSize(10).Bold().FontColor("#1A237E");
                }
            });

            // Disclaimer + dual signature section
            var disclaimer = string.IsNullOrEmpty(settings.SignatureFooterText)
                ? "This report is computer generated. For any queries, please contact the laboratory."
                : settings.SignatureFooterText;

            col.Item().PaddingTop(24).BorderTop(1).BorderColor("#CFD8DC").PaddingTop(10)
               .Column(sigSection =>
               {
                   sigSection.Item()
                       .Text(disclaimer).FontSize(8).FontColor("#9E9E9E").Italic();

                   sigSection.Item().PaddingTop(20).Row(row =>
                   {
                       row.RelativeItem().Element(c => ComposeSignatureBlock(c,
                           name:        settings.LabInchargeName,
                           designation: "Lab Incharge",
                           imagePath:   settings.LabInchargeSignaturePath));

                       row.ConstantItem(40);

                       row.RelativeItem().Element(c => ComposeSignatureBlock(c,
                           name:        settings.ConsultantPathologistName,
                           designation: "Consultant Pathologist",
                           imagePath:   settings.ConsultantPathologistSignaturePath));
                   });
               });
        });
    }

    // ── Footer ────────────────────────────────────────────────────────────────
    private static void ComposeFooter(IContainer c, Report report, AppSettings settings)
    {
        var centreName = string.IsNullOrWhiteSpace(settings.CentreName)
            ? "Urban Diagnostic Centre"
            : settings.CentreName;

        c.AlignCenter().Text(t =>
        {
            t.Span($"{centreName}  |  ").FontColor("#757575");
            t.Span(report.ReportCode).FontColor("#1565C0").Bold();
            t.Span("  |  Page ").FontColor("#757575");
            t.CurrentPageNumber().FontColor("#757575");
            t.Span(" of ").FontColor("#757575");
            t.TotalPages().FontColor("#757575");
        });
    }

    // ── Path resolution ───────────────────────────────────────────────────────

    // Resolves a stored PDF path (relative or legacy absolute) to a full filesystem path.
    // Relative paths are joined against the configured ReportsRoot.
    public static string ResolvePath(string? pdfPath, AppSettings settings)
    {
        if (string.IsNullOrEmpty(pdfPath)) return string.Empty;
        if (Path.IsPathRooted(pdfPath))    return pdfPath;  // legacy absolute path

        var root = string.IsNullOrEmpty(settings.ReportsRootPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UrbanDiagnosticCentre", "Reports")
            : settings.ReportsRootPath;

        return Path.Combine(root, pdfPath.Replace('/', Path.DirectorySeparatorChar));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Renders one signature block (image optional, name optional, designation always shown).
    // Stage 1: typed name + designation text (always rendered).
    // Stage 2: digital signature image overlay above text (rendered when imagePath is set and readable).
    private static void ComposeSignatureBlock(IContainer c, string name, string designation, string? imagePath)
    {
        c.Column(col =>
        {
            // Stage 2 – optional signature image, aspect-ratio-preserved, centered
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    var imgBytes = File.ReadAllBytes(imagePath);
                    col.Item().MaxHeight(60).AlignCenter()
                       .Image(imgBytes, ImageScaling.FitArea);
                }
                catch { /* unreadable — skip gracefully */ }
            }

            // Stage 1 – typed name (omit row if unconfigured)
            if (!string.IsNullOrWhiteSpace(name))
                col.Item().AlignCenter().Text(name).FontSize(10).Bold();

            // Designation label always shown
            col.Item().AlignCenter().Text(designation).FontSize(9).FontColor("#757575");
        });
    }

    private static void InfoRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingBottom(4).Row(row =>
        {
            row.ConstantItem(72).Text(label + ":").FontSize(9).FontColor("#757575");
            row.RelativeItem().Text(value).FontSize(9).Bold();
        });
    }
}
