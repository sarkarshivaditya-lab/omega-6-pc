#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddBackupMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "BackupSizeBytes",
            table: "BackupRecords",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<bool>(
            name: "IsAutoBackup",
            table: "BackupRecords",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "Note",
            table: "BackupRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BackupSizeBytes", table: "BackupRecords");
        migrationBuilder.DropColumn(name: "IsAutoBackup",    table: "BackupRecords");
        migrationBuilder.DropColumn(name: "Note",            table: "BackupRecords");
    }
}
