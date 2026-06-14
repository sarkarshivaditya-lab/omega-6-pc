#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddSignatureSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ConsultantPathologistName",
            table: "AppSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "ConsultantPathologistSignaturePath",
            table: "AppSettings",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LabInchargeName",
            table: "AppSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "LabInchargeSignaturePath",
            table: "AppSettings",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ConsultantPathologistName",
            table: "AppSettings");

        migrationBuilder.DropColumn(
            name: "ConsultantPathologistSignaturePath",
            table: "AppSettings");

        migrationBuilder.DropColumn(
            name: "LabInchargeName",
            table: "AppSettings");

        migrationBuilder.DropColumn(
            name: "LabInchargeSignaturePath",
            table: "AppSettings");
    }
}
