using UrbanDiagnosticCentre.Data;
using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    // Known seeded defaults keyed by username — used only for first-login warning.
    private static readonly Dictionary<string, string> _knownDefaults = new()
    {
        { "admin",  "admin123" },
        { "tech01", "tech123"  }
    };

    public User? CurrentUser { get; private set; }

    // True only if the user logged in with a known seeded default password.
    // Cleared to false immediately when ChangePasswordAsync succeeds.
    public bool IsUsingDefaultPassword { get; private set; }

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        // UI thread: query DB.
        var user = _db.Users.FirstOrDefault(u =>
            u.Username == username && u.IsActive);

        if (user is null) return false;

        // Capture hash as a plain string so the entity is never touched inside Task.Run.
        var hash = user.PasswordHash;

        // Thread pool: BCrypt.Verify only.
        var verified = await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, hash));
        if (!verified) return false;

        // UI thread: state mutation after await.
        CurrentUser = user;
        // Plaintext is available here at no extra cost — check against known defaults.
        IsUsingDefaultPassword = _knownDefaults.TryGetValue(username, out var def) && password == def;
        return true;
    }

    public void Logout()
    {
        CurrentUser = null;
        IsUsingDefaultPassword = false;
    }

    // Verifies the current password, re-hashes, and persists the new hash.
    // Returns false if the user is not logged in or the current password is wrong.
    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        // UI thread: null check.
        if (CurrentUser is null) return false;

        // Capture hash as a plain string so the entity is never touched inside Task.Run.
        var existingHash = CurrentUser.PasswordHash;

        // Thread pool: BCrypt.Verify only.
        var verified = await Task.Run(() => BCrypt.Net.BCrypt.Verify(currentPassword, existingHash));
        if (!verified) return false;

        // Thread pool: BCrypt.HashPassword only.
        var newHash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(newPassword));

        // UI thread: update tracked entity and persist.
        CurrentUser.PasswordHash = newHash;
        _db.SaveChanges();
        IsUsingDefaultPassword = false;
        return true;
    }
}
