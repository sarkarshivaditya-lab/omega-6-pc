using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Services;

public interface IAuthService
{
    User? CurrentUser { get; }
    bool IsUsingDefaultPassword { get; }
    Task<bool> LoginAsync(string username, string password);
    void Logout();
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
}
