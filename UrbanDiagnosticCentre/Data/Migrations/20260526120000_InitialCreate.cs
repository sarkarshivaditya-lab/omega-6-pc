#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BackupRecords",
            columns: table => new
            {
                Id         = table.Column<int>(type: "INTEGER", nullable: false)
                                  .Annotation("Sqlite:Autoincrement", true),
                BackupDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                BackupPath = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
                table.PrimaryKey("PK_BackupRecords", x => x.Id));

        migrationBuilder.CreateTable(
            name: "TestDefinitions",
            columns: table => new
            {
                Id               = table.Column<int>(type: "INTEGER", nullable: false)
                                        .Annotation("Sqlite:Autoincrement", true),
                TestName         = table.Column<string>(type: "TEXT", nullable: false),
                Category         = table.Column<string>(type: "TEXT", nullable: false),
                SampleType       = table.Column<string>(type: "TEXT", nullable: false),
                Unit             = table.Column<string>(type: "TEXT", nullable: false),
                MaleMinValue     = table.Column<decimal>(type: "TEXT", nullable: false),
                MaleMaxValue     = table.Column<decimal>(type: "TEXT", nullable: false),
                FemaleMinValue   = table.Column<decimal>(type: "TEXT", nullable: false),
                FemaleMaxValue   = table.Column<decimal>(type: "TEXT", nullable: false),
                ChildMinValue    = table.Column<decimal>(type: "TEXT", nullable: false),
                ChildMaxValue    = table.Column<decimal>(type: "TEXT", nullable: false),
                DecimalPrecision = table.Column<int>(type: "INTEGER", nullable: false),
                Notes            = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt        = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
                table.PrimaryKey("PK_TestDefinitions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id           = table.Column<int>(type: "INTEGER", nullable: false)
                                    .Annotation("Sqlite:Autoincrement", true),
                Username     = table.Column<string>(type: "TEXT", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                Role         = table.Column<string>(type: "TEXT", nullable: false),
                FullName     = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt    = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsActive     = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
                table.PrimaryKey("PK_Users", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id              = table.Column<int>(type: "INTEGER", nullable: false)
                                       .Annotation("Sqlite:Autoincrement", true),
                FullName        = table.Column<string>(type: "TEXT", nullable: false),
                Age             = table.Column<int>(type: "INTEGER", nullable: false),
                Gender          = table.Column<string>(type: "TEXT", nullable: false),
                PhoneNumber     = table.Column<string>(type: "TEXT", nullable: false),
                ReferringDoctor = table.Column<string>(type: "TEXT", nullable: false),
                Address         = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt       = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsArchived      = table.Column<bool>(type: "INTEGER", nullable: false),
                ArchivedAt      = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Patients", x => x.Id);
                table.ForeignKey(
                    name: "FK_Patients_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Reports",
            columns: table => new
            {
                Id                = table.Column<int>(type: "INTEGER", nullable: false)
                                         .Annotation("Sqlite:Autoincrement", true),
                ReportCode        = table.Column<string>(type: "TEXT", nullable: false),
                PatientId         = table.Column<int>(type: "INTEGER", nullable: false),
                TestDate          = table.Column<DateTime>(type: "TEXT", nullable: false),
                ReportDate        = table.Column<DateTime>(type: "TEXT", nullable: false),
                PdfPath           = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt         = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsArchived        = table.Column<bool>(type: "INTEGER", nullable: false),
                ArchivedAt        = table.Column<DateTime>(type: "TEXT", nullable: true),
                Status            = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Draft"),
                CreatedByUserId   = table.Column<int>(type: "INTEGER", nullable: true),
                ModifiedByUserId  = table.Column<int>(type: "INTEGER", nullable: true),
                ModifiedAt        = table.Column<DateTime>(type: "TEXT", nullable: true),
                ReportGeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                PrintedAt         = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Reports", x => x.Id);
                table.ForeignKey(
                    name: "FK_Reports_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Reports_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Reports_Users_ModifiedByUserId",
                    column: x => x.ModifiedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ReportEntries",
            columns: table => new
            {
                Id                 = table.Column<int>(type: "INTEGER", nullable: false)
                                          .Annotation("Sqlite:Autoincrement", true),
                ReportId           = table.Column<int>(type: "INTEGER", nullable: false),
                TestDefinitionId   = table.Column<int>(type: "INTEGER", nullable: false),
                ResultValue        = table.Column<string>(type: "TEXT", nullable: false),
                ResultFlag         = table.Column<string>(type: "TEXT", nullable: false),
                ReferenceRangeUsed = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReportEntries_Reports_ReportId",
                    column: x => x.ReportId,
                    principalTable: "Reports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ReportEntries_TestDefinitions_TestDefinitionId",
                    column: x => x.TestDefinitionId,
                    principalTable: "TestDefinitions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // ── Indexes ───────────────────────────────────────────────────────────

        migrationBuilder.CreateIndex(
            name: "IX_Patients_CreatedByUserId",
            table: "Patients",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Patients_FullName",
            table: "Patients",
            column: "FullName");

        migrationBuilder.CreateIndex(
            name: "IX_ReportEntries_ReportId",
            table: "ReportEntries",
            column: "ReportId");

        migrationBuilder.CreateIndex(
            name: "IX_ReportEntries_TestDefinitionId",
            table: "ReportEntries",
            column: "TestDefinitionId");

        migrationBuilder.CreateIndex(
            name: "IX_Reports_CreatedByUserId",
            table: "Reports",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Reports_ModifiedByUserId",
            table: "Reports",
            column: "ModifiedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Reports_PatientId",
            table: "Reports",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_Reports_ReportCode",
            table: "Reports",
            column: "ReportCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Reports_TestDate",
            table: "Reports",
            column: "TestDate");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BackupRecords");
        migrationBuilder.DropTable(name: "ReportEntries");
        migrationBuilder.DropTable(name: "Reports");
        migrationBuilder.DropTable(name: "Patients");
        migrationBuilder.DropTable(name: "TestDefinitions");
        migrationBuilder.DropTable(name: "Users");
    }
}
