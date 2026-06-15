using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddSyncIds : Migration
{
    private static readonly string[] _tables =
    [
        "Users", "Patients", "Reports", "ReportEntries",
        "TestDefinitions", "TestPrices", "TestPackages", "TestPackageItems",
        "FinancialTransactions"
    ];

    // SQLite UUID expression — generates a properly-formatted v4-ish UUID.
    private const string UuidExpr =
        "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-' || " +
        "lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(6)))";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Add SyncId to every syncable table ──────────────────────────────
        foreach (var table in _tables)
        {
            migrationBuilder.AddColumn<string>(
                name: "SyncId",
                table: table,
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql($@"UPDATE ""{table}"" SET ""SyncId"" = {UuidExpr} WHERE ""SyncId"" IS NULL");

            migrationBuilder.CreateIndex(
                name: $"IX_{table}_SyncId",
                table: table,
                column: "SyncId",
                unique: true);
        }

        // ── Add UpdatedAt to every syncable table ────────────────────────────
        foreach (var table in _tables)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: table,
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql($@"UPDATE ""{table}"" SET ""UpdatedAt"" = datetime('now') WHERE ""UpdatedAt"" IS NULL");
        }

        // ── OriginMachineCode on Reports ─────────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "OriginMachineCode",
            table: "Reports",
            type: "TEXT",
            nullable: false,
            defaultValue: "ADM");

        // ── Soft-delete fields on FinancialTransactions ──────────────────────
        migrationBuilder.AddColumn<bool>(
            name: "IsDeleted",
            table: "FinancialTransactions",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeletedAt",
            table: "FinancialTransactions",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "OriginMachineCode", table: "Reports");
        migrationBuilder.DropColumn(name: "IsDeleted",         table: "FinancialTransactions");
        migrationBuilder.DropColumn(name: "DeletedAt",         table: "FinancialTransactions");

        foreach (var table in _tables)
        {
            migrationBuilder.DropIndex(name: $"IX_{table}_SyncId", table: table);
            migrationBuilder.DropColumn(name: "SyncId",    table: table);
            migrationBuilder.DropColumn(name: "UpdatedAt", table: table);
        }
    }
}
