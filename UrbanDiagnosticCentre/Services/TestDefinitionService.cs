using System.Globalization;
using Microsoft.EntityFrameworkCore;
using UrbanDiagnosticCentre.Data;
using UrbanDiagnosticCentre.Models;

namespace UrbanDiagnosticCentre.Services;

public class TestDefinitionService
{
    private readonly AppDbContext _db;

    public TestDefinitionService(AppDbContext db) => _db = db;

    public List<TestDefinition> GetAll() =>
        _db.TestDefinitions
           .Where(t => !t.IsArchived)
           .OrderBy(t => t.Category)
           .ThenBy(t => t.TestName)
           .ToList();

    public TestDefinition? GetById(int id) =>
        _db.TestDefinitions.FirstOrDefault(t => t.Id == id);

    public void Add(TestDefinition td)
    {
        _db.TestDefinitions.Add(td);
        _db.SaveChanges();
    }

    public void Update(TestDefinition td)
    {
        _db.TestDefinitions.Update(td);
        _db.SaveChanges();
    }

    public void Archive(TestDefinition td)
    {
        td.IsArchived = true;
        td.ArchivedAt = DateTime.Now;
        _db.TestDefinitions.Update(td);
        _db.SaveChanges();
    }

    // ── Pricing ───────────────────────────────────────────────────────────────

    // Canonical form for all stored tier names: trimmed Title Case.
    // "regular", "REGULAR", " Regular " all become "Regular".
    // Centralised here so every write path goes through the same transform.
    private static string NormalizeTierName(string name) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.Trim().ToLowerInvariant());

    public List<TestPrice> GetPrices(int testDefinitionId) =>
        _db.TestPrices
           .Where(tp => tp.TestDefinitionId == testDefinitionId)
           .OrderBy(tp => tp.SortOrder)
           .ThenBy(tp => tp.TierName)
           .ToList();

    // All stored TierName values are canonical, so a plain SQL GROUP BY is sufficient.
    public List<string> GetDistinctActiveTiers() =>
        _db.TestPrices
           .Where(tp => tp.IsActive)
           .GroupBy(tp => tp.TierName)
           .OrderBy(g => g.Min(tp => tp.SortOrder))
           .ThenBy(g => g.Key)
           .Select(g => g.Key)
           .ToList();

    // Normalizes the lookup key so callers never need to worry about casing.
    // Plain equality comparison is sargable — the unique index on (TestDefinitionId, TierName)
    // is used by the query engine.
    public decimal? GetPrice(int testDefinitionId, string tierName)
    {
        var canonical = NormalizeTierName(tierName);
        return _db.TestPrices
           .Where(tp => tp.TestDefinitionId == testDefinitionId
                     && tp.TierName == canonical
                     && tp.IsActive)
           .Select(tp => (decimal?)tp.Price)
           .FirstOrDefault();
    }

    // Normalizes TierName before any lookup or insert so the unique index always sees
    // canonical values, making DB-level duplicate prevention reliable.
    public void UpsertPrice(TestPrice price)
    {
        price.TierName = NormalizeTierName(price.TierName);

        var existing = _db.TestPrices.FirstOrDefault(tp =>
            tp.TestDefinitionId == price.TestDefinitionId &&
            tp.TierName == price.TierName);

        if (existing is null)
        {
            _db.TestPrices.Add(price);
        }
        else
        {
            existing.Price    = price.Price;
            existing.IsActive = price.IsActive;
            // SortOrder is preserved to maintain display order
        }

        _db.SaveChanges();
    }

    public void DeletePrice(int testPriceId)
    {
        var tp = _db.TestPrices.Find(testPriceId);
        if (tp is null) return;
        _db.TestPrices.Remove(tp);
        _db.SaveChanges();
    }
}
