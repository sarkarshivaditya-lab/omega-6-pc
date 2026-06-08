namespace UrbanDiagnosticCentre.Services;

public interface IReportCodeService
{
    /// <summary>Returns a unique code in the format UDC-YYYYMMDD-HHMMSS.</summary>
    string Generate();
}
