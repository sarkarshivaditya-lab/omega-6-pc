using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class AddSyncInfrastructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── SyncIdentities ────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "SyncIdentities",
            columns: table => new
            {
                Id             = table.Column<int>(type: "INTEGER", nullable: false),
                MachineId      = table.Column<string>(type: "TEXT", nullable: false),
                MachineCode    = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "ADM"),
                CreatedAt      = table.Column<DateTime>(type: "TEXT", nullable: false),
                AdminApiBaseUrl = table.Column<string>(type: "TEXT", nullable: true),
                ApiKey         = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_SyncIdentities", x => x.Id));

        // ── SyncCursors ───────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "SyncCursors",
            columns: table => new
            {
                Id           = table.Column<int>(type: "INTEGER", nullable: false)
                                    .Annotation("Sqlite:Autoincrement", true),
                EntityType   = table.Column<string>(type: "TEXT", nullable: false),
                LastPulledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SyncCursors", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_SyncCursors_EntityType",
            table: "SyncCursors",
            column: "EntityType",
            unique: true);

        // ── SyncOutboxEntries ─────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "SyncOutboxEntries",
            columns: table => new
            {
                Id            = table.Column<int>(type: "INTEGER", nullable: false)
                                     .Annotation("Sqlite:Autoincrement", true),
                CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                EntityType    = table.Column<string>(type: "TEXT", nullable: false),
                EntitySyncId  = table.Column<string>(type: "TEXT", nullable: false),
                Operation     = table.Column<string>(type: "TEXT", nullable: false),
                Payload       = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt     = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsSent        = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                SentAt        = table.Column<DateTime>(type: "TEXT", nullable: true),
                AttemptCount  = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                LastError     = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_SyncOutboxEntries", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_SyncOutboxEntries_CorrelationId",
            table: "SyncOutboxEntries",
            column: "CorrelationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SyncOutboxEntries_IsSent",
            table: "SyncOutboxEntries",
            column: "IsSent");

        // ── AppSyncLogEntries ─────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "AppSyncLogEntries",
            columns: table => new
            {
                Id                = table.Column<int>(type: "INTEGER", nullable: false)
                                         .Annotation("Sqlite:Autoincrement", true),
                CorrelationId     = table.Column<string>(type: "TEXT", nullable: false),
                EntityType        = table.Column<string>(type: "TEXT", nullable: false),
                EntitySyncId      = table.Column<string>(type: "TEXT", nullable: false),
                Operation         = table.Column<string>(type: "TEXT", nullable: false),
                Payload           = table.Column<string>(type: "TEXT", nullable: false),
                SourceMachineCode = table.Column<string>(type: "TEXT", nullable: false),
                AppliedAt         = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AppSyncLogEntries", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_AppSyncLogEntries_AppliedAt",
            table: "AppSyncLogEntries",
            column: "AppliedAt");

        migrationBuilder.CreateIndex(
            name: "IX_AppSyncLogEntries_EntityType",
            table: "AppSyncLogEntries",
            column: "EntityType");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppSyncLogEntries");
        migrationBuilder.DropTable(name: "SyncOutboxEntries");
        migrationBuilder.DropTable(name: "SyncCursors");
        migrationBuilder.DropTable(name: "SyncIdentities");
    }
}
