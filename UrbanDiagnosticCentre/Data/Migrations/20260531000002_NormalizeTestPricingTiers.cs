#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace UrbanDiagnosticCentre.Data.Migrations;

public partial class NormalizeTestPricingTiers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Step 1: Trim and lowercase all TierName values ────────────────────
        // SQLite has no built-in INITCAP / Title Case function. Lowercasing is the
        // maximum normalisation achievable in pure SQL. The application's
        // NormalizeTierName() helper (which produces Title Case) is applied
        // automatically on the next write through UpsertPrice(); existing rows
        // that are never edited will remain lowercase and still sort/match correctly
        // because all comparisons now use exact equality against a canonical key.
        migrationBuilder.Sql(
            "UPDATE TestPrices SET TierName = trim(lower(TierName));");

        // Normalize the stored default tier name in AppSettings by the same rule.
        migrationBuilder.Sql(
            "UPDATE AppSettings SET DefaultPriceTier = trim(lower(DefaultPriceTier));");

        // ── Step 2: Deduplicate ───────────────────────────────────────────────
        // After Step 1, formerly case-variant rows (e.g. "Regular" / "regular")
        // now share an identical TierName. For each (TestDefinitionId, TierName)
        // group, retain the row with the lowest SortOrder (earliest display
        // position). Among rows with equal SortOrder, retain the lowest Id
        // (earliest insertion). All other rows are deleted.
        migrationBuilder.Sql(@"
DELETE FROM TestPrices
WHERE Id NOT IN (
    SELECT MIN(t.Id)
    FROM TestPrices AS t
    INNER JOIN (
        SELECT TestDefinitionId,
               TierName,
               MIN(SortOrder) AS MinSort
        FROM   TestPrices
        GROUP  BY TestDefinitionId, TierName
    ) AS g
        ON  t.TestDefinitionId = g.TestDefinitionId
        AND t.TierName         = g.TierName
        AND t.SortOrder        = g.MinSort
    GROUP BY t.TestDefinitionId, t.TierName
);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentional no-op.
        //
        // This migration performs two irreversible data operations:
        //   1. TierName casing is collapsed — the original mixed-case values are not stored.
        //   2. Duplicate rows are deleted — their Price values are not recoverable.
        //
        // Rolling back to the previous migration state is therefore not possible
        // without restoring from a database backup taken before this migration ran.
    }
}
