using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddTestPackages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TestPackages",
            columns: table => new
            {
                Id          = table.Column<int>(type: "INTEGER", nullable: false)
                                  .Annotation("Sqlite:Autoincrement", true),
                Name        = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                Price       = table.Column<decimal>(type: "TEXT", nullable: false),
                IsActive    = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                CreatedAt   = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TestPackages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TestPackageItems",
            columns: table => new
            {
                Id               = table.Column<int>(type: "INTEGER", nullable: false)
                                       .Annotation("Sqlite:Autoincrement", true),
                PackageId        = table.Column<int>(type: "INTEGER", nullable: false),
                TestDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                SortOrder        = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TestPackageItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_TestPackageItems_TestPackages_PackageId",
                    column: x => x.PackageId,
                    principalTable: "TestPackages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TestPackageItems_TestDefinitions_TestDefinitionId",
                    column: x => x.TestDefinitionId,
                    principalTable: "TestDefinitions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TestPackageItems_PackageId",
            table: "TestPackageItems",
            column: "PackageId");

        migrationBuilder.CreateIndex(
            name: "IX_TestPackageItems_TestDefinitionId",
            table: "TestPackageItems",
            column: "TestDefinitionId");

        migrationBuilder.CreateIndex(
            name: "IX_TestPackages_Name",
            table: "TestPackages",
            column: "Name");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TestPackageItems");
        migrationBuilder.DropTable(name: "TestPackages");
    }
}
