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
    // Cleared to false immediately when ChangePassword succeeds.
    public bool IsUsingDefaultPassword { get; private set; }

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public bool Login(string username, string password)
    {
        var user = _db.Users.FirstOrDefault(u =>
            u.Username == username && u.IsActive);

        if (user is null) return false;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return false;

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
    public bool ChangePassword(string currentPassword, string newPassword)
    {
        if (CurrentUser is null) return false;
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, CurrentUser.PasswordHash)) return false;

        CurrentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _db.SaveChanges();
        IsUsingDefaultPassword = false;
        return true;
    }
}
