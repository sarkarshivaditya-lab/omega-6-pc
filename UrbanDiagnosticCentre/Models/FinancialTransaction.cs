namespace UrbanDiagnosticCentre.Models;

public enum TransactionType { Income, Expense }

public class FinancialTransaction
{
    public int             Id              { get; set; }
    public DateTime        TransactionDate { get; set; }
    public TransactionType Type            { get; set; } = TransactionType.Expense;
    public string          Category        { get; set; } = string.Empty;
    public string          Description     { get; set; } = string.Empty;
    public decimal         Amount          { get; set; }
    public string?         Notes           { get; set; }

    // Future-proof columns (nullable, ignored by current UI)
    public string?         PaymentMethod   { get; set; }
    public string?         InvoiceNumber   { get; set; }
    public decimal?        TaxAmount       { get; set; }

    public int?            CreatedByUserId { get; set; }
    public DateTime        CreatedAt       { get; set; } = DateTime.Now;

    // Links auto-derived income row back to its source report
    public int?            SourceReportId  { get; set; }

    public User?           CreatedByUser   { get; set; }
}
