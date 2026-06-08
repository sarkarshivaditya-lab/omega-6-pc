#nullable disable

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UrbanDiagnosticCentre.Data;

namespace UrbanDiagnosticCentre.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260526120000_InitialCreate")]
partial class InitialCreate
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "9.0.5");

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.BackupRecord", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTime>("BackupDate").HasColumnType("TEXT");
            b.Property<string>("BackupPath").IsRequired().HasColumnType("TEXT");
            b.HasKey("Id");
            b.ToTable("BackupRecords");
        });

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
            b.HasKey("Id");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("FullName");
            b.ToTable("Patients");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.Report", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTime?>("ArchivedAt").HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<int?>("CreatedByUserId").HasColumnType("INTEGER");
            b.Property<bool>("IsArchived").HasColumnType("INTEGER");
            b.Property<DateTime?>("ModifiedAt").HasColumnType("TEXT");
            b.Property<int?>("ModifiedByUserId").HasColumnType("INTEGER");
            b.Property<int>("PatientId").HasColumnType("INTEGER");
            b.Property<string>("PdfPath").HasColumnType("TEXT");
            b.Property<DateTime?>("PrintedAt").HasColumnType("TEXT");
            b.Property<DateTime>("ReportDate").HasColumnType("TEXT");
            b.Property<DateTime?>("ReportGeneratedAt").HasColumnType("TEXT");
            b.Property<string>("ReportCode").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Status").IsRequired().HasColumnType("TEXT").HasDefaultValue("Draft");
            b.Property<DateTime>("TestDate").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("ModifiedByUserId");
            b.HasIndex("PatientId");
            b.HasIndex("ReportCode").IsUnique();
            b.HasIndex("TestDate");
            b.ToTable("Reports");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.ReportEntry", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<string>("ReferenceRangeUsed").IsRequired().HasColumnType("TEXT");
            b.Property<int>("ReportId").HasColumnType("INTEGER");
            b.Property<string>("ResultFlag").IsRequired().HasColumnType("TEXT");
            b.Property<string>("ResultValue").IsRequired().HasColumnType("TEXT");
            b.Property<int>("TestDefinitionId").HasColumnType("INTEGER");
            b.HasKey("Id");
            b.HasIndex("ReportId");
            b.HasIndex("TestDefinitionId");
            b.ToTable("ReportEntries");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.TestDefinition", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<string>("Category").IsRequired().HasColumnType("TEXT");
            b.Property<decimal>("ChildMaxValue").HasColumnType("TEXT");
            b.Property<decimal>("ChildMinValue").HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<int>("DecimalPrecision").HasColumnType("INTEGER");
            b.Property<decimal>("FemaleMaxValue").HasColumnType("TEXT");
            b.Property<decimal>("FemaleMinValue").HasColumnType("TEXT");
            b.Property<decimal>("MaleMaxValue").HasColumnType("TEXT");
            b.Property<decimal>("MaleMinValue").HasColumnType("TEXT");
            b.Property<string>("Notes").IsRequired().HasColumnType("TEXT");
            b.Property<string>("SampleType").IsRequired().HasColumnType("TEXT");
            b.Property<string>("TestName").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Unit").IsRequired().HasColumnType("TEXT");
            b.HasKey("Id");
            b.ToTable("TestDefinitions");
        });

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.User", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<string>("FullName").IsRequired().HasColumnType("TEXT");
            b.Property<bool>("IsActive").HasColumnType("INTEGER");
            b.Property<string>("PasswordHash").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Role").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Username").IsRequired().HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("Username").IsUnique();
            b.ToTable("Users");
        });

        // ── Relationships ────────────────────────────────────────────────────

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

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.Patient", b =>
            b.Navigation("Reports"));

        modelBuilder.Entity("UrbanDiagnosticCentre.Models.Report", b =>
            b.Navigation("Entries"));
#pragma warning restore 612, 618
    }
}
