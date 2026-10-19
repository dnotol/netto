using Netto.Core.Rules;

namespace Netto.Core.CorporateTax;

// Corporate tax of a company whose accounting year is the calendar year, for one income year (taxed
// in the following assessment year). No disallowed expenses and no losses carried forward: the
// taxable income is the year's profit after director pay.
public sealed record CorporateTaxRules(
    int IncomeYear,
    RuleAmount StandardRate,
    RuleAmount ReducedRate,
    RuleAmount ReducedRateCeiling,
    RuleAmount MinimumDirectorPay)
{
    private static readonly Source Article215Income2026 = new(
        "CIR 92, art. 215 (revenus 2026), coordinated text on Fisconetplus, last modified 03.08.2026",
        "Al. 1: 25%. Al. 2: 20% on the first 100,000 for a small company. Al. 3, 4°: from its fifth tax period, only if a director's pay reaches the indexed minimum, or the taxable income does not exceed that pay",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/7132fb4e-e229-4008-a585-4ad4853edca5"),
        "corporate-tax/spf-finances_cir92-art-215-revenus-2026_2026-08-03.html");

    private static readonly Source IndexationNotice2027 = new(
        "Indexation notice for assessment year 2027, version 03.08.2026, adapted to the law of 15.07.2026",
        "CIR 92 art. 215, al. 3, 4°: minimum director pay, base 25,000, indexed 51,000",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/042cb526-403e-4f40-9a45-dbccd0a12583"),
        "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html");

    public static Source Source => Article215Income2026;

    public static CorporateTaxRules IncomeYear2026 { get; } = new(
        2026,
        new RuleAmount("standard_rate", 0.25m, Article215Income2026),
        new RuleAmount("reduced_rate", 0.20m, Article215Income2026),
        new RuleAmount("reduced_rate_ceiling", 100_000m, Article215Income2026),
        new RuleAmount("minimum_director_pay", 51_000m, IndexationNotice2027));

    public static CorporateTaxRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };

    public IReadOnlyList<RuleAmount> Amounts => [StandardRate, ReducedRate, ReducedRateCeiling, MinimumDirectorPay];
}
