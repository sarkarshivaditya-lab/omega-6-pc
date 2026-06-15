using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Data;

public static class DatabaseSeeder
{
    public static void Seed(AppDbContext db)
    {
        SeedUsers(db);
        SeedTestDefinitions(db);
        SeedSyncIdentity(db);
    }

    private static void SeedSyncIdentity(AppDbContext db)
    {
        if (db.SyncIdentities.Any()) return;

        db.SyncIdentities.Add(new SyncIdentity
        {
            Id          = 1,
            MachineId   = Guid.NewGuid(),
            MachineCode = "ADM",
            CreatedAt   = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    private static void SeedUsers(AppDbContext db)
    {
        if (db.Users.Any()) return;

        db.Users.AddRange(
            new User
            {
                Username     = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role         = "Admin",
                FullName     = "System Administrator",
                CreatedAt    = DateTime.Now,
                IsActive     = true
            },
            new User
            {
                Username     = "tech01",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("tech123"),
                Role         = "Technician",
                FullName     = "Technician One",
                CreatedAt    = DateTime.Now,
                IsActive     = true
            }
        );

        db.SaveChanges();
    }

    // ── Test Definitions ──────────────────────────────────────────────────────

    private static void SeedTestDefinitions(AppDbContext db)
    {
        if (db.TestDefinitions.Any()) return;

        var tests = new List<TestDefinition>
        {
            // ── Hematology: Hemoglobin ────────────────────────────────────────
            new()
            {
                TestName         = "Hemoglobin",
                Category         = "Hematology",
                SampleType       = "Whole Blood (EDTA)",
                Unit             = "g/dL",
                MaleMinValue     = 13.0m,
                MaleMaxValue     = 17.0m,
                FemaleMinValue   = 12.0m,
                FemaleMaxValue   = 15.0m,
                ChildMinValue    = 11.0m,
                ChildMaxValue    = 14.0m,
                DecimalPrecision = 1,
                Notes            = "Low Hgb indicates anaemia; high may indicate polycythaemia.",
                CreatedAt        = DateTime.Now
            },

            // ── Hematology: CBC ───────────────────────────────────────────────
            new()
            {
                TestName         = "WBC - White Blood Cell Count",
                Category         = "Hematology / CBC",
                SampleType       = "Whole Blood (EDTA)",
                Unit             = "×10³/µL",
                MaleMinValue     = 4.5m,
                MaleMaxValue     = 11.0m,
                FemaleMinValue   = 4.5m,
                FemaleMaxValue   = 11.0m,
                ChildMinValue    = 5.0m,
                ChildMaxValue    = 15.0m,
                DecimalPrecision = 2,
                Notes            = "Elevated in infection/inflammation; low may indicate bone marrow suppression.",
                CreatedAt        = DateTime.Now
            },
            new()
            {
                TestName         = "RBC - Red Blood Cell Count",
                Category         = "Hematology / CBC",
                SampleType       = "Whole Blood (EDTA)",
                Unit             = "×10⁶/µL",
                MaleMinValue     = 4.5m,
                MaleMaxValue     = 5.5m,
                FemaleMinValue   = 3.8m,
                FemaleMaxValue   = 4.8m,
                ChildMinValue    = 3.8m,
                ChildMaxValue    = 5.2m,
                DecimalPrecision = 2,
                Notes            = string.Empty,
                CreatedAt        = DateTime.Now
            },
            new()
            {
                TestName         = "Hematocrit (PCV)",
                Category         = "Hematology / CBC",
                SampleType       = "Whole Blood (EDTA)",
                Unit             = "%",
                MaleMinValue     = 40.0m,
                MaleMaxValue     = 54.0m,
                FemaleMinValue   = 36.0m,
                FemaleMaxValue   = 48.0m,
                ChildMinValue    = 33.0m,
                ChildMaxValue    = 43.0m,
                DecimalPrecision = 1,
                Notes            = "Packed cell volume; mirrors Hgb trends.",
                CreatedAt        = DateTime.Now
            },
            new()
            {
                TestName         = "Platelets",
                Category         = "Hematology / CBC",
                SampleType       = "Whole Blood (EDTA)",
                Unit             = "×10³/µL",
                MaleMinValue     = 150m,
                MaleMaxValue     = 400m,
                FemaleMinValue   = 150m,
                FemaleMaxValue   = 400m,
                ChildMinValue    = 150m,
                ChildMaxValue    = 400m,
                DecimalPrecision = 0,
                Notes            = "< 50 requires urgent review; > 1000 (thrombocytosis) requires investigation.",
                CreatedAt        = DateTime.Now
            },
            new()
            {
                TestName         = "MCV - Mean Corpuscular Volume",
                Category         = "Hematology / CBC",
                SampleType       = "Whole Blood (EDTA)",
                Unit             = "fL",
                MaleMinValue     = 80.0m,
                MaleMaxValue     = 100.0m,
                FemaleMinValue   = 80.0m,
                FemaleMaxValue   = 100.0m,
                ChildMinValue    = 70.0m,
                ChildMaxValue    = 90.0m,
                DecimalPrecision = 1,
                Notes            = "Low MCV → microcytic; High MCV → macrocytic anaemia.",
                CreatedAt        = DateTime.Now
            },
            new()
            {
                TestName         = "MCH - Mean Corpuscular Haemoglobin",
                Category         = "Hematology / CBC",
                SampleType       = "Whole Blood (EDTA)",
                Unit             = "pg",
                MaleMinValue     = 27.0m,
                MaleMaxValue     = 32.0m,
                FemaleMinValue   = 27.0m,
                FemaleMaxValue   = 32.0m,
                ChildMinValue    = 25.0m,
                ChildMaxValue    = 31.0m,
                DecimalPrecision = 1,
                Notes            = string.Empty,
                CreatedAt        = DateTime.Now
            },
            new()
            {
                TestName         = "MCHC - Mean Corpuscular Haemoglobin Concentration",
                Category         = "Hematology / CBC",
                SampleType       = "Whole Blood (EDTA)",
                Unit             = "g/dL",
                MaleMinValue     = 32.0m,
                MaleMaxValue     = 36.0m,
                FemaleMinValue   = 32.0m,
                FemaleMaxValue   = 36.0m,
                ChildMinValue    = 32.0m,
                ChildMaxValue    = 36.0m,
                DecimalPrecision = 1,
                Notes            = string.Empty,
                CreatedAt        = DateTime.Now
            },

            // ── Biochemistry ──────────────────────────────────────────────────
            new()
            {
                TestName         = "Blood Sugar Fasting (BSF)",
                Category         = "Biochemistry",
                SampleType       = "Serum / Plasma",
                Unit             = "mg/dL",
                MaleMinValue     = 70m,
                MaleMaxValue     = 100m,
                FemaleMinValue   = 70m,
                FemaleMaxValue   = 100m,
                ChildMinValue    = 60m,
                ChildMaxValue    = 100m,
                DecimalPrecision = 0,
                Notes            = "Pre-diabetes: 100–125 mg/dL. Diabetes ≥ 126 mg/dL (fasting, confirmed).",
                CreatedAt        = DateTime.Now
            },
            new()
            {
                TestName         = "Creatinine (Serum)",
                Category         = "Biochemistry",
                SampleType       = "Serum",
                Unit             = "mg/dL",
                MaleMinValue     = 0.70m,
                MaleMaxValue     = 1.30m,
                FemaleMinValue   = 0.50m,
                FemaleMaxValue   = 1.10m,
                ChildMinValue    = 0.30m,
                ChildMaxValue    = 0.70m,
                DecimalPrecision = 2,
                Notes            = "Elevated in renal impairment; interpret with eGFR and BUN.",
                CreatedAt        = DateTime.Now
            },

            // ── Thyroid ───────────────────────────────────────────────────────
            new()
            {
                TestName         = "TSH - Thyroid Stimulating Hormone",
                Category         = "Thyroid",
                SampleType       = "Serum",
                Unit             = "mIU/L",
                MaleMinValue     = 0.400m,
                MaleMaxValue     = 4.000m,
                FemaleMinValue   = 0.400m,
                FemaleMaxValue   = 4.000m,
                ChildMinValue    = 0.500m,
                ChildMaxValue    = 5.000m,
                DecimalPrecision = 3,
                Notes            = "Elevated TSH → hypothyroidism. Suppressed TSH → hyperthyroidism. Pregnancy ranges differ.",
                CreatedAt        = DateTime.Now
            }
        };

        db.TestDefinitions.AddRange(tests);
        db.SaveChanges();
    }
}
