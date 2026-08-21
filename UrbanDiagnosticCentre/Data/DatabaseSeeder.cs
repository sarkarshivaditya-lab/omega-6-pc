using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Data;

public static class DatabaseSeeder
{
    public static void Seed(AppDbContext db)
    {
        SeedUsers(db);
        SeedSyncIdentity(db);
    }

    private static void SeedSyncIdentity(AppDbContext db)
    {
        if (db.SyncIdentities.Any()) return;
        db.SyncIdentities.Add(new SyncIdentity
        {
            Id = 1,
            MachineId = Guid.NewGuid(),
            MachineCode = "ADM",
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static void SeedUsers(AppDbContext db)
    {
        if (db.Users.Any()) return;
        db.Users.AddRange(
            new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                FullName = "System Administrator",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new User
            {
                Username = "tech01",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("tech123"),
                Role = "Operator",
                FullName = "Workspace Operator",
                CreatedAt = DateTime.Now,
                IsActive = true
            }
        );
        db.SaveChanges();
    }
}