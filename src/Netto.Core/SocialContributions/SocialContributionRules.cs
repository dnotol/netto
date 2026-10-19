using Netto.Core.Rules;

namespace Netto.Core.SocialContributions;

// Final INASTI contributions of a self-employed person in main occupation, general case, for one
// income year. The fund's management fee is not included.
public sealed record SocialContributionRules(
    int IncomeYear,
    Source Source,
    RuleAmount MinimumIncome,
    RuleAmount FirstBandRate,
    RuleAmount FirstBandCeiling,
    RuleAmount SecondBandRate,
    RuleAmount SecondBandCeiling)
{
    private static readonly Source Inasti2026 = new(
        "INASTI, « Cotisations 2026 » (January 2026)",
        "§1 B.2 « Cotisations définitives, en général », per note to the funds P741/25/9 of 11.12.2025",
        new Uri("https://www.inasti.be/sites/rsvz/files/2024-01/folder_cotisations_sociales.pdf"),
        "social-contributions/inasti_cotisations-sociales-2026_2026-01-05.pdf");

    public static SocialContributionRules IncomeYear2026 { get; } = new(
        2026,
        Inasti2026,
        new RuleAmount("minimum_income", 17_374.08m, Inasti2026),
        new RuleAmount("first_band_rate", 0.205m, Inasti2026),
        new RuleAmount("first_band_ceiling", 75_024.54m, Inasti2026),
        new RuleAmount("second_band_rate", 0.1416m, Inasti2026),
        new RuleAmount("second_band_ceiling", 110_562.42m, Inasti2026));

    public static SocialContributionRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };

    public IReadOnlyList<RuleAmount> Amounts =>
        [MinimumIncome, FirstBandRate, FirstBandCeiling, SecondBandRate, SecondBandCeiling];
}
