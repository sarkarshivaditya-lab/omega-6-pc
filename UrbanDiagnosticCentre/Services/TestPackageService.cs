using Microsoft.EntityFrameworkCore;
using UrbanDiagnosticCentre.Data;
using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Services;

public class TestPackageService
{
    private readonly AppDbContext _db;

    public TestPackageService(AppDbContext db) => _db = db;

    public List<TestPackage> GetAll() =>
        _db.TestPackages
           .Include(p => p.Items).ThenInclude(i => i.TestDefinition)
           .OrderBy(p => p.Name)
           .ToList();

    public List<TestPackage> GetActive() =>
        _db.TestPackages
           .Include(p => p.Items).ThenInclude(i => i.TestDefinition)
           .Where(p => p.IsActive)
           .OrderBy(p => p.Name)
           .ToList();

    public void Add(TestPackage package)
    {
        _db.TestPackages.Add(package);
        _db.SaveChanges();
    }

    public void UpdateMetadata(int id, string name, string description, decimal price, bool isActive)
    {
        var pkg = _db.TestPackages.Find(id);
        if (pkg is null) return;
        pkg.Name        = name;
        pkg.Description = description;
        pkg.Price       = price;
        pkg.IsActive    = isActive;
        _db.SaveChanges();
    }

    public void Delete(int id)
    {
        var pkg = _db.TestPackages.Find(id);
        if (pkg is null) return;
        _db.TestPackages.Remove(pkg);
        _db.SaveChanges();
    }

    public TestPackage Duplicate(TestPackage source)
    {
        // Reload with items to be safe
        var loaded = _db.TestPackages
                        .Include(p => p.Items)
                        .First(p => p.Id == source.Id);

        var copy = new TestPackage
        {
            Name        = $"{loaded.Name} (Copy)",
            Description = loaded.Description,
            Price       = loaded.Price,
            IsActive    = true,
            CreatedAt   = DateTime.Now
        };
        foreach (var item in loaded.Items.OrderBy(i => i.SortOrder))
        {
            copy.Items.Add(new TestPackageItem
            {
                TestDefinitionId = item.TestDefinitionId,
                SortOrder        = item.SortOrder
            });
        }
        _db.TestPackages.Add(copy);
        _db.SaveChanges();
        return copy;
    }

    // Replaces all items for a package with the supplied ordered list of TestDefinitionIds.
    public void SetItems(int packageId, List<int> testDefinitionIds)
    {
        var existing = _db.TestPackageItems
                          .Where(i => i.PackageId == packageId)
                          .ToList();
        _db.TestPackageItems.RemoveRange(existing);

        for (int i = 0; i < testDefinitionIds.Count; i++)
        {
            _db.TestPackageItems.Add(new TestPackageItem
            {
                PackageId        = packageId,
                TestDefinitionId = testDefinitionIds[i],
                SortOrder        = i
            });
        }
        _db.SaveChanges();
    }
}
