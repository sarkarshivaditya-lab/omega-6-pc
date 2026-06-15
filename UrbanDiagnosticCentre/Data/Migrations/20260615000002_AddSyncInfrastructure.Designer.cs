#nullable disable

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UrbanDiagnosticCentre.Data;

namespace UrbanDiagnosticCentre.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260615000002_AddSyncInfrastructure")]
partial class AddSyncInfrastructure
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "9.0.5");

        // ── AppSettings ──────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.AppSettings", b =>
        {
            b.Property<int>("Id").ValueGeneratedNever().HasColumnType("INTEGER");
            b.Property<string>("BackupsRootPath").HasColumnType("TEXT");
            b.Property<string>("CentreAddress").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.Property<string>("CentreEmail").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.Property<string>("CentreName").IsRequired().HasColumnType("TEXT").HasDefaultValue("Urban Diagnostic Centre");
            b.Property<string>("CentrePhone").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.Property<string>("ConsultantPathologistName").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.Property<string>("ConsultantPathologistSignaturePath").HasColumnType("TEXT");
            b.Property<string>("DefaultPriceTier").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.Property<string>("GstTaxId").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.Property<string>("LabInchargeName").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.Property<string>("LabInchargeSignaturePath").HasColumnType("TEXT");
            b.Property<string>("LogoPath").HasColumnType("TEXT");
            b.Property<string>("ReportsRootPath").HasColumnType("TEXT");
            b.Property<string>("SignatureFooterText").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.Property<DateTime?>("UpdatedAt").HasColumnType("TEXT");
            b.Property<string>("WatermarkText").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.HasKey("Id");
            b.ToTable("AppSettings");
        });

        // ── BackupRecord ─────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.BackupRecord", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTime>("BackupDate").HasColumnType("TEXT");
            b.Property<string>("BackupPath").IsRequired().HasColumnType("TEXT");
            b.Property<long>("BackupSizeBytes").HasColumnType("INTEGER").HasDefaultValue(0L);
            b.Property<bool>("IsAutoBackup").HasColumnType("INTEGER").HasDefaultValue(false);
            b.Property<string>("Note").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.HasKey("Id");
            b.ToTable("BackupRecords");
        });

        // ── FinancialTransaction ─────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.FinancialTransaction", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<decimal>("Amount").HasColumnType("TEXT");
            b.Property<string>("Category").IsRequired().HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<int?>("CreatedByUserId").HasColumnType("INTEGER");
            b.Property<DateTime?>("DeletedAt").HasColumnType("TEXT");
            b.Property<string>("Description").IsRequired().HasColumnType("TEXT");
            b.Property<string>("InvoiceNumber").HasColumnType("TEXT");
            b.Property<bool>("IsDeleted").HasColumnType("INTEGER").HasDefaultValue(false);
            b.Property<string>("Notes").HasColumnType("TEXT");
            b.Property<string>("PaymentMethod").HasColumnType("TEXT");
            b.Property<int?>("SourceReportId").HasColumnType("INTEGER");
            b.Property<Guid>("SyncId").HasColumnType("TEXT");
            b.Property<decimal?>("TaxAmount").HasColumnType("TEXT");
            b.Property<DateTime>("TransactionDate").HasColumnType("TEXT");
            b.Property<string>("Type").IsRequired().HasColumnType("TEXT").HasDefaultValue("Expense");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("SyncId").IsUnique();
            b.HasIndex("TransactionDate");
            b.HasIndex("Type");
            b.ToTable("FinancialTransactions");
        });

        // ── Patient ──────────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.Patient", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<string>("Address").IsRequired().HasColumnType("TEXT");
            b.Property<int>("Age").HasColumnType("INTEGER");
            b.Property<DateTime?>("ArchivedAt").HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<int?>("CreatedByUserId").HasColumnType("INTEGER");
            b.Property<string>("FullName").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Gender").IsRequired().HasColumnType("TEXT");
            b.Property<bool>("IsArchived").HasColumnType("INTEGER");
            b.Property<string>("PhoneNumber").IsRequired().HasColumnType("TEXT");
            b.Property<string>("ReferringDoctor").IsRequired().HasColumnType("TEXT");
            b.Property<Guid>("SyncId").HasColumnType("TEXT");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("FullName");
            b.HasIndex("SyncId").IsUnique();
            b.ToTable("Patients");
        });

        // ── Report ───────────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.Report", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTime?>("ArchivedAt").HasColumnType("TEXT");
            b.Property<string>("BillingMode").IsRequired().HasColumnType("TEXT").HasDefaultValue("Normal");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<int?>("CreatedByUserId").HasColumnType("INTEGER");
            b.Property<bool>("IsArchived").HasColumnType("INTEGER");
            b.Property<DateTime?>("ModifiedAt").HasColumnType("TEXT");
            b.Property<int?>("ModifiedByUserId").HasColumnType("INTEGER");
            b.Property<string>("OriginMachineCode").IsRequired().HasColumnType("TEXT").HasDefaultValue("ADM");
            b.Property<string>("PackageNameSnapshot").HasColumnType("TEXT");
            b.Property<decimal?>("PackageTotalPrice").HasColumnType("TEXT");
            b.Property<int>("PatientId").HasColumnType("INTEGER");
            b.Property<string>("PdfPath").HasColumnType("TEXT");
            b.Property<int>("PdfVersion").HasColumnType("INTEGER").HasDefaultValue(0);
            b.Property<DateTime?>("PrintedAt").HasColumnType("TEXT");
            b.Property<string>("PriceTierName").HasColumnType("TEXT");
            b.Property<DateTime>("ReportDate").HasColumnType("TEXT");
            b.Property<DateTime?>("ReportGeneratedAt").HasColumnType("TEXT");
            b.Property<string>("ReportCode").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Status").IsRequired().HasColumnType("TEXT").HasDefaultValue("Draft");
            b.Property<Guid>("SyncId").HasColumnType("TEXT");
            b.Property<DateTime>("TestDate").HasColumnType("TEXT");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("ModifiedByUserId");
            b.HasIndex("PatientId");
            b.HasIndex("ReportCode").IsUnique();
            b.HasIndex("SyncId").IsUnique();
            b.HasIndex("TestDate");
            b.ToTable("Reports");
        });

        // ── ReportEntry ──────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.ReportEntry", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<decimal?>("ChargedPrice").HasColumnType("TEXT");
            b.Property<bool>("IsFromPackage").HasColumnType("INTEGER").HasDefaultValue(false);
            b.Property<string>("PriceTierName").HasColumnType("TEXT");
            b.Property<string>("ReferenceRangeUsed").IsRequired().HasColumnType("TEXT");
            b.Property<int>("ReportId").HasColumnType("INTEGER");
            b.Property<string>("ResultFlag").IsRequired().HasColumnType("TEXT");
            b.Property<string>("ResultValue").IsRequired().HasColumnType("TEXT");
            b.Property<Guid>("SyncId").HasColumnType("TEXT");
            b.Property<int>("TestDefinitionId").HasColumnType("INTEGER");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("ReportId");
            b.HasIndex("SyncId").IsUnique();
            b.HasIndex("TestDefinitionId");
            b.ToTable("ReportEntries");
        });

        // ── TestDefinition ───────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.TestDefinition", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTime?>("ArchivedAt").HasColumnType("TEXT");
            b.Property<string>("Category").IsRequired().HasColumnType("TEXT");
            b.Property<decimal>("ChildMaxValue").HasColumnType("TEXT");
            b.Property<decimal>("ChildMinValue").HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<int>("DecimalPrecision").HasColumnType("INTEGER");
            b.Property<decimal>("FemaleMaxValue").HasColumnType("TEXT");
            b.Property<decimal>("FemaleMinValue").HasColumnType("TEXT");
            b.Property<bool>("IsArchived").HasColumnType("INTEGER");
            b.Property<decimal>("MaleMaxValue").HasColumnType("TEXT");
            b.Property<decimal>("MaleMinValue").HasColumnType("TEXT");
            b.Property<string>("Notes").IsRequired().HasColumnType("TEXT");
            b.Property<string>("SampleType").IsRequired().HasColumnType("TEXT");
            b.Property<Guid>("SyncId").HasColumnType("TEXT");
            b.Property<string>("TestName").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Unit").IsRequired().HasColumnType("TEXT");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("SyncId").IsUnique();
            b.ToTable("TestDefinitions");
        });

        // ── TestPackage ──────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.TestPackage", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<string>("Description").IsRequired().HasColumnType("TEXT").HasDefaultValue("");
            b.Property<bool>("IsActive").HasColumnType("INTEGER").HasDefaultValue(true);
            b.Property<string>("Name").IsRequired().HasColumnType("TEXT");
            b.Property<decimal>("Price").HasColumnType("TEXT");
            b.Property<Guid>("SyncId").HasColumnType("TEXT");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("Name");
            b.HasIndex("SyncId").IsUnique();
            b.ToTable("TestPackages");
        });

        // ── TestPackageItem ───────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.TestPackageItem", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<int>("PackageId").HasColumnType("INTEGER");
            b.Property<int>("SortOrder").HasColumnType("INTEGER").HasDefaultValue(0);
            b.Property<Guid>("SyncId").HasColumnType("TEXT");
            b.Property<int>("TestDefinitionId").HasColumnType("INTEGER");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("PackageId");
            b.HasIndex("SyncId").IsUnique();
            b.HasIndex("TestDefinitionId");
            b.ToTable("TestPackageItems");
        });

        // ── TestPrice ────────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.TestPrice", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<bool>("IsActive").HasColumnType("INTEGER").HasDefaultValue(true);
            b.Property<decimal>("Price").HasColumnType("TEXT");
            b.Property<int>("SortOrder").HasColumnType("INTEGER").HasDefaultValue(0);
            b.Property<Guid>("SyncId").HasColumnType("TEXT");
            b.Property<int>("TestDefinitionId").HasColumnType("INTEGER");
            b.Property<string>("TierName").IsRequired().HasColumnType("TEXT");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("SyncId").IsUnique();
            b.HasIndex("TestDefinitionId", "TierName").IsUnique();
            b.ToTable("TestPrices");
        });

        // ── User ─────────────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.User", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<string>("FullName").IsRequired().HasColumnType("TEXT");
            b.Property<bool>("IsActive").HasColumnType("INTEGER");
            b.Property<string>("PasswordHash").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Role").IsRequired().HasColumnType("TEXT");
            b.Property<Guid>("SyncId").HasColumnType("TEXT");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.Property<string>("Username").IsRequired().HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("SyncId").IsUnique();
            b.HasIndex("Username").IsUnique();
            b.ToTable("Users");
        });

        // ── SyncIdentity ─────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.SyncIdentity", b =>
        {
            b.Property<int>("Id").ValueGeneratedNever().HasColumnType("INTEGER");
            b.Property<string>("AdminApiBaseUrl").HasColumnType("TEXT");
            b.Property<string>("ApiKey").HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<string>("MachineCode").IsRequired().HasColumnType("TEXT").HasDefaultValue("ADM");
            b.Property<Guid>("MachineId").HasColumnType("TEXT");
            b.HasKey("Id");
            b.ToTable("SyncIdentities");
        });

        // ── SyncCursor ───────────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.SyncCursor", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<string>("EntityType").IsRequired().HasColumnType("TEXT");
            b.Property<DateTime>("LastPulledAt").HasColumnType("TEXT");
            b.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("EntityType").IsUnique();
            b.ToTable("SyncCursors");
        });

        // ── SyncOutboxEntry ───────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.SyncOutboxEntry", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<int>("AttemptCount").HasColumnType("INTEGER").HasDefaultValue(0);
            b.Property<Guid>("CorrelationId").HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<string>("EntityType").IsRequired().HasColumnType("TEXT");
            b.Property<Guid>("EntitySyncId").HasColumnType("TEXT");
            b.Property<bool>("IsSent").HasColumnType("INTEGER").HasDefaultValue(false);
            b.Property<string?>("LastError").HasColumnType("TEXT");
            b.Property<string>("Operation").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Payload").IsRequired().HasColumnType("TEXT");
            b.Property<DateTime?>("SentAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("CorrelationId").IsUnique();
            b.HasIndex("IsSent");
            b.ToTable("SyncOutboxEntries");
        });

        // ── AppSyncLogEntry ───────────────────────────────────────────────────
        modelBuilder.Entity("UrbanDiagnosticCentre.Models.AppSyncLogEntry", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTime>("AppliedAt").HasColumnType("TEXT");
            b.Property<Guid>("CorrelationId").HasColumnType("TEXT");
            b.Property<string>("EntityType").IsRequired().HasColumnType("TEXT");
            b.Property<Guid>("EntitySyncId").HasColumnType("TEXT");
            b.Property<string>("Operation").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Payload").IsRequired().HasColumnType("TEXT");
            b.Property<string>("SourceMachineCode").IsRequired().HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("AppliedAt");
            b.HasIndex("EntityType");
            b.ToTable("AppSyncLogEntries");
        });

        // ── Relationships ────────────────────────────────────────────────────

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.FinancialTransaction", b =>
        {
            b.HasOne("UrbanDiagnosticCentre.Models.User", "CreatedByUser")
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict);
            b.Navigation("CreatedByUser");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.Patient", b =>
        {
            b.HasOne("UrbanDiagnosticCentre.Models.User", "CreatedByUser")
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict);
            b.Navigation("CreatedByUser");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.Report", b =>
        {
            b.HasOne("UrbanDiagnosticCentre.Models.User", "CreatedByUser")
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne("UrbanDiagnosticCentre.Models.User", "ModifiedByUser")
                .WithMany()
                .HasForeignKey("ModifiedByUserId")
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne("UrbanDiagnosticCentre.Models.Patient", "Patient")
                .WithMany("Reports")
                .HasForeignKey("PatientId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            b.Navigation("CreatedByUser");
            b.Navigation("ModifiedByUser");
            b.Navigation("Patient");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.ReportEntry", b =>
        {
            b.HasOne("UrbanDiagnosticCentre.Models.Report", "Report")
                .WithMany("Entries")
                .HasForeignKey("ReportId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.HasOne("UrbanDiagnosticCentre.Models.TestDefinition", "TestDefinition")
                .WithMany()
                .HasForeignKey("TestDefinitionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            b.Navigation("Report");
            b.Navigation("TestDefinition");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.TestPackageItem", b =>
        {
            b.HasOne("UrbanDiagnosticCentre.Models.TestPackage", "Package")
                .WithMany("Items")
                .HasForeignKey("PackageId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.HasOne("UrbanDiagnosticCentre.Models.TestDefinition", "TestDefinition")
                .WithMany()
                .HasForeignKey("TestDefinitionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            b.Navigation("Package");
            b.Navigation("TestDefinition");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.TestPrice", b =>
        {
            b.HasOne("UrbanDiagnosticCentre.Models.TestDefinition", "TestDefinition")
                .WithMany()
                .HasForeignKey("TestDefinitionId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.Navigation("TestDefinition");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.Patient", b =>
            b.Navigation("Reports"));

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.Report", b =>
            b.Navigation("Entries"));

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.TestPackage", b =>
            b.Navigation("Items"));

#pragma warning restore 612, 618
    }
}
