#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddTestPricing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // New child table: tier-named prices per test definition
        migrationBuilder.CreateTable(
            name: "TestPrices",
            columns: table => new
            {
                Id               = table.Column<int>(type: "INTEGER", nullable: false)
                                        .Annotation("Sqlite:Autoincrement", true),
                TestDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                TierName         = table.Column<string>(type: "TEXT", nullable: false),
                Price            = table.Column<decimal>(type: "TEXT", nullable: false),
                SortOrder        = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                IsActive         = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TestPrices", x => x.Id);
                table.ForeignKey(
                    name:              "FK_TestPrices_TestDefinitions_TestDefinitionId",
                    column:            x => x.TestDefinitionId,
                    principalTable:    "TestDefinitions",
                    principalColumn:   "Id",
                    onDelete:          ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name:    "IX_TestPrices_TestDefinitionId_TierName",
            table:   "TestPrices",
            columns: new[] { "TestDefinitionId", "TierName" },
            unique:  true);

        // Nullable additions to existing tables — safe on databases with existing rows
        migrationBuilder.AddColumn<string>(
            name:       "PriceTierName",
            table:      "Reports",
            type:       "TEXT",
            nullable:   true);

        migrationBuilder.AddColumn<string>(
            name:       "PriceTierName",
            table:      "ReportEntries",
            type:       "TEXT",
            nullable:   true);

        migrationBuilder.AddColumn<decimal>(
            name:       "ChargedPrice",
            table:      "ReportEntries",
            type:       "TEXT",
            nullable:   true);

        migrationBuilder.AddColumn<string>(
            name:         "DefaultPriceTier",
            table:        "AppSettings",
            type:         "TEXT",
            nullable:     false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TestPrices");

        migrationBuilder.DropColumn(name: "PriceTierName", table: "Reports");
        migrationBuilder.DropColumn(name: "PriceTierName", table: "ReportEntries");
        migrationBuilder.DropColumn(name: "ChargedPrice",  table: "ReportEntries");
        migrationBuilder.DropColumn(name: "DefaultPriceTier", table: "AppSettings");
    }
}
