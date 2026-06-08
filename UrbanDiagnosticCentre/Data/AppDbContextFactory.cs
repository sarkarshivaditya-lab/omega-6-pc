using Microsoft.EntityFrameworkCore.Design;

namespace UrbanDiagnosticCentre.Data;

/// <summary>
/// Allows the EF Core CLI tools to instantiate AppDbContext at design time
/// (e.g. when running "dotnet ef migrations add").
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new();
}
