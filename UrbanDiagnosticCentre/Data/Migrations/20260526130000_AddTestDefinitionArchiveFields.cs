#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddTestDefinitionArchiveFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsArchived",
            table: "TestDefinitions",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "ArchivedAt",
            table: "TestDefinitions",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsArchived",  table: "TestDefinitions");
        migrationBuilder.DropColumn(name: "ArchivedAt", table: "TestDefinitions");
    }
}
