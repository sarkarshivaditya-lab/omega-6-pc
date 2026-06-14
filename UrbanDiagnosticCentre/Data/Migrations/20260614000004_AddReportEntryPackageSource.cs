using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddReportEntryPackageSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BillingMode",
            table: "Reports",
            type: "TEXT",
            nullable: false,
            defaultValue: "Normal");

        migrationBuilder.AddColumn<string>(
            name: "PackageNameSnapshot",
            table: "Reports",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PackageTotalPrice",
            table: "Reports",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BillingMode",         table: "Reports");
        migrationBuilder.DropColumn(name: "PackageNameSnapshot", table: "Reports");
        migrationBuilder.DropColumn(name: "PackageTotalPrice",   table: "Reports");
    }
}
