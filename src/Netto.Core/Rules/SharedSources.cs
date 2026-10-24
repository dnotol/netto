namespace Netto.Core.Rules;

// The official documents two or more rule sets cite, each written once. A rule set takes one with the
// passage it rests on, SharedSources.X with { Reference = "…" }, and keeps it as a source of its own;
// a document only one rule set cites stays in that rule set. Fields, and internal: a step cites a rule
// set's source, never one of these directly.
internal static class SharedSources
{
    internal static readonly Source IndexationNotice2026 = new(
        "Indexation notice for assessment year 2026, version 19.01.2026",
        "Every amount of CIR 92 indexed for income 2025",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/934106e5-36bd-4f61-8928-ba9ca100727f"),
        "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html");

    internal static readonly Source IndexationNotice2027 = new(
        "Indexation notice for assessment year 2027, version 03.08.2026, adapted to the law of 15.07.2026",
        "Every amount of CIR 92 indexed for income 2026",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/042cb526-403e-4f40-9a45-dbccd0a12583"),
        "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html");

    internal static readonly Source RoyalDecree38 = new(
        "Arrêté royal n° 38 du 27.07.1967 organisant le statut social des travailleurs indépendants, consolidated on Justel, updated to 01.06.2026",
        "Art. 11 §1 and §2: the contributions are a percentage of the professional income as the income tax law sets it, gross less the professional costs",
        new Uri("https://www.ejustice.just.fgov.be/eli/arrete/1967/07/27/1967072702/justel"),
        "social-contributions/ejustice_arrete-royal-38-statut-social-independants_2026-06-01.html") { Label = "Royal decree n° 38, art. 11" };

    internal static readonly Source Circular2021C20 = new(
        "Circulaire 2021/C/20 du 26.02.2021 relative aux interventions de l'employeur pour le télétravail, on Fisconetplus, last modified 09.09.2026",
        "The employer's contributions to telework: the office allowance, private internet and a private computer",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/c4b263eb-091c-4a11-8ad0-24e2b11bb7db"),
        "personal-tax/spf-finances_circulaire-2021-c-20-teletravail_2026-09-09.html") { Label = "Circular 2021/C/20" };

    internal static readonly Source Article32Income2025 = Source.Fisconet("personal-tax", 32, 2025, new DateOnly(2024, 12, 2), "adf7c228-083c-4468-b8d1-151c1e03a067",
        "A director's pay is what they obtain for their office or their functions in the company, whatever its name");

    internal static readonly Source Article32Income2026 = Source.Fisconet("personal-tax", 32, 2026, new DateOnly(2026, 1, 29), "7628aa2c-168a-4bd8-aec4-288108a91757",
        "A director's pay is what they obtain for their office or their functions in the company, whatever its name");

    internal static readonly Source Article53Income2026 = Source.Fisconet("corporate-tax", 53, 2026, new DateOnly(2026, 2, 18), "796aa044-5b54-4569-88a4-abe7be55e077",
        "The costs that are not professional costs");

    internal static readonly Source Article157Income2025 = Source.Fisconet("personal-tax", 157, 2025, new DateOnly(2025, 1, 16), "cdf69919-b665-491b-9a02-059fd68dd24f",
        "The total tax on profits, benefits and directors' pay not withheld or paid in advance during the year is surcharged");

    internal static readonly Source Article157Income2026 = Source.Fisconet("personal-tax", 157, 2026, new DateOnly(2026, 7, 30), "c6e47940-53f0-4608-b567-ae774014a621",
        "The total tax on directors' pay (art. 30, 2°) not withheld or paid in advance during the year is surcharged, as the law of 15.07.2026 leaves it");

    internal static readonly Source Article159Income2026 = Source.Fisconet("corporate-tax", 159, 2026, new DateOnly(2026, 2, 9), "ed182474-e88a-4931-91a3-51286d1dc47a",
        "The surcharge: the tax times 2.25 times the reference rate, less the advance payments times 3, 2.5, 2 and 1.5 times it, by the deadline each was paid by");

    internal static readonly Source Article269Income2025 = Source.Fisconet("personal-tax", 269, 2025, new DateOnly(2025, 8, 1), "580606b1-7faa-4683-8861-8da64ff59d25",
        "The rates of the withholding on movable income");

    internal static readonly Source Article269Income2026 = Source.Fisconet("personal-tax", 269, 2026, new DateOnly(2026, 6, 2), "fb744b8d-7ddb-4067-903a-f84eae927052",
        "The rates of the withholding on movable income");

    internal static readonly Source Article304Income2026 = Source.Fisconet("personal-tax", 304, 2026, new DateOnly(2026, 2, 19), "bda433e6-7da6-4dc9-882c-0c9d92591877",
        "§2: what the advance payments, the withholdings and the tax credits leave over after the tax is set against the other assessments, and the rest is refunded once it reaches 2.50");
}
