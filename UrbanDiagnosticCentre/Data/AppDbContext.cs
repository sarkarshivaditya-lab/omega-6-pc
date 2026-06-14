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
    public DbSet<AppSettings>          AppSettings           { get; set; }
    public DbSet<FinancialTransaction> FinancialTransactions { get; set; }
    public DbSet<TestPackage>     TestPackages     { get; set; }
    public DbSet<TestPackageItem> TestPackageItems { get; set; }

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

        modelBuilder.Entity<Report>()
            .Property(r => r.BillingMode)
            .HasConversion<string>()
            .HasDefaultValue(BillingMode.Normal);

        modelBuilder.Entity<Report>()
            .Property(r => r.PackageTotalPrice)
            .HasColumnType("TEXT");

        // ── ReportEntries ────────────────────────────────────────────────────
        modelBuilder.Entity<ReportEntry>()
            .Property(re => re.IsFromPackage)
            .HasDefaultValue(false);

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

        // ── FinancialTransactions ─────────────────────────────────────────────
        modelBuilder.Entity<FinancialTransaction>()
            .Property(ft => ft.Type)
            .HasConversion<string>()
            .HasDefaultValue(TransactionType.Expense);

        modelBuilder.Entity<FinancialTransaction>()
            .Property(ft => ft.Amount)
            .HasColumnType("TEXT");

        modelBuilder.Entity<FinancialTransaction>()
            .Property(ft => ft.TaxAmount)
            .HasColumnType("TEXT");

        modelBuilder.Entity<FinancialTransaction>()
            .HasOne(ft => ft.CreatedByUser)
            .WithMany()
            .HasForeignKey(ft => ft.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FinancialTransaction>()
            .HasIndex(ft => ft.TransactionDate);

        modelBuilder.Entity<FinancialTransaction>()
            .HasIndex(ft => ft.Type);

        // ── TestPackages ─────────────────────────────────────────────────────────
        modelBuilder.Entity<TestPackage>()
            .Property(p => p.Description).HasDefaultValue("");

        modelBuilder.Entity<TestPackage>()
            .Property(p => p.IsActive).HasDefaultValue(true);

        modelBuilder.Entity<TestPackage>()
            .Property(p => p.Price).HasColumnType("TEXT");

        modelBuilder.Entity<TestPackage>()
            .HasIndex(p => p.Name);

        // ── TestPackageItems ──────────────────────────────────────────────────
        modelBuilder.Entity<TestPackageItem>()
            .Property(i => i.SortOrder).HasDefaultValue(0);

        modelBuilder.Entity<TestPackageItem>()
            .HasOne(i => i.Package)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestPackageItem>()
            .HasOne(i => i.TestDefinition)
            .WithMany()
            .HasForeignKey(i => i.TestDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

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
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.LabInchargeName).HasDefaultValue("");
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.ConsultantPathologistName).HasDefaultValue("");
    }
}
