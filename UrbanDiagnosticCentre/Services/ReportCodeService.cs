using UrbanDiagnosticCentre.Data;

namespace UrbanDiagnosticCentre.Services;

public class ReportCodeService : IReportCodeService
{
    private readonly AppDbContext _db;

    public ReportCodeService(AppDbContext db) => _db = db;

    /// <summary>
    /// Generates UDC-{MACHINECODE}-YYYYMMDD-HHMMSS-XXX.
    /// XXX is a per-second sequence starting at 001.
    /// Existing reports keep their original format; only new reports include the machine code.
    /// </summary>
    public string Generate()
    {
        var machineCode = AppDbContext.CurrentMachineCode;
        var now = DateTime.Now;
        var prefix = $"UDC-{machineCode}-{now:yyyyMMdd-HHmmss}-";

        var existingCodes = _db.Reports
            .Where(r => r.ReportCode.StartsWith(prefix))
            .Select(r => r.ReportCode)
            .ToList();

        int nextSeq = 1;

        if (existingCodes.Count > 0)
        {
            nextSeq = existingCodes
                .Select(code => int.TryParse(code[prefix.Length..], out var n) ? n : 0)
                .Max() + 1;
        }

        return $"{prefix}{nextSeq:D3}";
    }
}
