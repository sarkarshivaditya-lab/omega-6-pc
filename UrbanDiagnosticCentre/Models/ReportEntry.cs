namespace UrbanDiagnosticCentre.Models;

public enum ResultFlag { Normal, Low, High, Critical, Unknown }

public class ReportEntry
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int TestDefinitionId { get; set; }
    public string ResultValue { get; set; } = string.Empty;
    public ResultFlag ResultFlag { get; set; } = ResultFlag.Unknown;

    /// <summary>Which reference band was applied: "Male", "Female", or "Child"</summary>
    public string ReferenceRangeUsed { get; set; } = string.Empty;

    // Price snapshots — set at save time, null for reports created before pricing was introduced
    public string?  PriceTierName { get; set; }
    public decimal? ChargedPrice  { get; set; }

    public Report Report { get; set; } = null!;
    public TestDefinition TestDefinition { get; set; } = null!;
}
