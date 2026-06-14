using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddAccountingEntities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FinancialTransactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Type = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Expense"),
                Category = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: true),
                PaymentMethod = table.Column<string>(type: "TEXT", nullable: true),
                InvoiceNumber = table.Column<string>(type: "TEXT", nullable: true),
                TaxAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                SourceReportId = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_FinancialTransactions_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_CreatedByUserId",
            table: "FinancialTransactions",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_TransactionDate",
            table: "FinancialTransactions",
            column: "TransactionDate");

        migrationBuilder.CreateIndex(
            name: "IX_FinancialTransactions_Type",
            table: "FinancialTransactions",
            column: "Type");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FinancialTransactions");
    }
}
