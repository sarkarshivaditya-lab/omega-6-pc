using System.IO;
using Microsoft.EntityFrameworkCore;
using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<TestDefinition> TestDefinitions { get; set; }
    public DbSet<TestPrice> TestPrices { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<ReportEntry> ReportEntries { get; set; }
    public DbSet<BackupRecord> BackupRecords { get; set; }
    public DbSet<AppSettings> AppSettings   { get; set; }

    public AppDbContext() { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (options.IsConfigured) return;

        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbFolder = Path.Combine(folder, "UrbanDiagnosticCentre");
        Directory.CreateDirectory(dbFolder);
        var dbPath = Path.Combine(dbFolder, "udc.db");
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Users ────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // ── Patients ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Patient>()
            .Property(p => p.Gender)
            .HasConversion<string>();

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.FullName);

        modelBuilder.Entity<Patient>()
            .HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Reports ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Report>()
            .Property(r => r.Status)
            .HasConversion<string>()
            .HasDefaultValue(ReportStatus.Draft);

        modelBuilder.Entity<Report>()
            .Property(r => r.PdfVersion)
            .HasDefaultValue(0);

        modelBuilder.Entity<Report>()
            .HasIndex(r => r.ReportCode)
            .IsUnique();

        modelBuilder.Entity<Report>()
            .HasIndex(r => r.TestDate);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.Patient)
            .WithMany(p => p.Reports)
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.CreatedByUser)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.ModifiedByUser)
            .WithMany()
            .HasForeignKey(r => r.ModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── ReportEntries ────────────────────────────────────────────────────
        modelBuilder.Entity<ReportEntry>()
            .Property(re => re.ResultFlag)
            .HasConversion<string>();

        modelBuilder.Entity<ReportEntry>()
            .HasOne(re => re.Report)
            .WithMany(r => r.Entries)
            .HasForeignKey(re => re.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReportEntry>()
            .HasOne(re => re.TestDefinition)
            .WithMany()
            .HasForeignKey(re => re.TestDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── BackupRecords ─────────────────────────────────────────────────────
        modelBuilder.Entity<BackupRecord>()
            .Property(b => b.BackupSizeBytes).HasDefaultValue(0L);
        modelBuilder.Entity<BackupRecord>()
            .Property(b => b.IsAutoBackup).HasDefaultValue(false);
        modelBuilder.Entity<BackupRecord>()
            .Property(b => b.Note).HasDefaultValue("");

        // ── TestPrices ───────────────────────────────────────────────────────
        modelBuilder.Entity<TestPrice>()
            .HasOne(tp => tp.TestDefinition)
            .WithMany()
            .HasForeignKey(tp => tp.TestDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestPrice>()
            .HasIndex(tp => new { tp.TestDefinitionId, tp.TierName })
            .IsUnique();

        modelBuilder.Entity<TestPrice>()
            .Property(tp => tp.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<TestPrice>()
            .Property(tp => tp.SortOrder).HasDefaultValue(0);

        // ── AppSettings ──────────────────────────────────────────────────────
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.Id)
            .ValueGeneratedNever();
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.CentreName).HasDefaultValue("Urban Diagnostic Centre");
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.CentreAddress).HasDefaultValue("");
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.CentrePhone).HasDefaultValue("");
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.CentreEmail).HasDefaultValue("");
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.GstTaxId).HasDefaultValue("");
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.SignatureFooterText).HasDefaultValue("");
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.WatermarkText).HasDefaultValue("");
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.DefaultPriceTier).HasDefaultValue("");
    }
}
