#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddAppSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppSettings",
            columns: table => new
            {
                Id                  = table.Column<int>(type: "INTEGER", nullable: false),
                CentreName          = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Urban Diagnostic Centre"),
                CentreAddress       = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                CentrePhone         = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                CentreEmail         = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                LogoPath            = table.Column<string>(type: "TEXT", nullable: true),
                ReportsRootPath     = table.Column<string>(type: "TEXT", nullable: true),
                BackupsRootPath     = table.Column<string>(type: "TEXT", nullable: true),
                GstTaxId            = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                SignatureFooterText = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                WatermarkText       = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                UpdatedAt           = table.Column<DateTime>(type: "TEXT",    nullable: true)
            },
            constraints: table =>
                table.PrimaryKey("PK_AppSettings", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppSettings");
    }
}
