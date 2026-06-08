namespace UrbanDiagnosticCentre.Models;

public class TestPrice
{
    public int     Id               { get; set; }
    public int     TestDefinitionId { get; set; }
    public string  TierName         { get; set; } = string.Empty;
    public decimal Price            { get; set; }
    public int     SortOrder        { get; set; }
    public bool    IsActive         { get; set; } = true;

    public TestDefinition TestDefinition { get; set; } = null!;
}
