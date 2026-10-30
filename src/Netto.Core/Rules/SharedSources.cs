namespace Netto.Core.Rules;

// The official documents two or more rule sets cite, each written once. A rule set takes one with the
// passage it rests on, SharedSources.X with { Passage = "…" }, and keeps it as a source of its own;
// a document only one rule set cites stays in that rule set. Fields, and internal: a step cites a rule
// set's source, never one of these directly.
internal static class SharedSources
{
    internal static readonly Source IndexationNotice2026 = new(
        "indexed_amounts",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/934106e5-36bd-4f61-8928-ba9ca100727f"),
        "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html");

    internal static readonly Source IndexationNotice2027 = new(
        "indexed_amounts",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/042cb526-403e-4f40-9a45-dbccd0a12583"),
        "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html");

    internal static readonly Source RoyalDecree38 = new(
        "contributions_base",
        new Uri("https://www.ejustice.just.fgov.be/eli/arrete/1967/07/27/1967072702/justel"),
        "social-contributions/ejustice_arrete-royal-38-statut-social-independants_2026-06-01.html");

    internal static readonly Source Circular2021C20 = new(
        "telework",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/c4b263eb-091c-4a11-8ad0-24e2b11bb7db"),
        "personal-tax/spf-finances_circulaire-2021-c-20-teletravail_2026-09-09.html");

    internal static readonly Source Article32Income2025 = Source.Fisconet("personal-tax", "32", 2025, new DateOnly(2024, 12, 2), "adf7c228-083c-4468-b8d1-151c1e03a067", "director_pay");

    internal static readonly Source Article32Income2026 = Source.Fisconet("personal-tax", "32", 2026, new DateOnly(2026, 1, 29), "7628aa2c-168a-4bd8-aec4-288108a91757", "director_pay");

    internal static readonly Source Article49Income2026 = Source.Fisconet("corporate-tax", "49", 2026, new DateOnly(2026, 2, 4), "34fb9ab5-38a2-4f93-b73d-0727e67e54d3", "deductible_costs");

    internal static readonly Source Article53Income2026 = Source.Fisconet("corporate-tax", "53", 2026, new DateOnly(2026, 2, 18), "796aa044-5b54-4569-88a4-abe7be55e077", "non_deductible_costs");

    internal static readonly Source Article157Income2025 = Source.Fisconet("personal-tax", "157", 2025, new DateOnly(2025, 1, 16), "cdf69919-b665-491b-9a02-059fd68dd24f", "surcharged_tax");

    internal static readonly Source Article157Income2026 = Source.Fisconet("personal-tax", "157", 2026, new DateOnly(2026, 7, 30), "c6e47940-53f0-4608-b567-ae774014a621", "surcharged_tax");

    internal static readonly Source Article159Income2026 = Source.Fisconet("corporate-tax", "159", 2026, new DateOnly(2026, 2, 9), "ed182474-e88a-4931-91a3-51286d1dc47a", "surcharge");

    internal static readonly Source Article269Income2025 = Source.Fisconet("personal-tax", "269", 2025, new DateOnly(2025, 8, 1), "580606b1-7faa-4683-8861-8da64ff59d25", "withholding_rates");

    internal static readonly Source Article269Income2026 = Source.Fisconet("personal-tax", "269", 2026, new DateOnly(2026, 6, 2), "fb744b8d-7ddb-4067-903a-f84eae927052", "withholding_rates");

    internal static readonly Source Article304Income2026 = Source.Fisconet("personal-tax", "304", 2026, new DateOnly(2026, 2, 19), "bda433e6-7da6-4dc9-882c-0c9d92591877", "balance");
}
