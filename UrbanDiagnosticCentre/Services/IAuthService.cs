using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Services;

public interface IAuthService
{
    User? CurrentUser { get; }
    bool IsUsingDefaultPassword { get; }
    bool Login(string username, string password);
    void Logout();
    bool ChangePassword(string currentPassword, string newPassword);
}
