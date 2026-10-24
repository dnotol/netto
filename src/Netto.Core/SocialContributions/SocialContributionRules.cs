using Netto.Core.Rules;

namespace Netto.Core.SocialContributions;

// Final INASTI contributions of a self-employed person in main occupation, general case, for one
// income year, and the texts of the social insurance fund's management fee on them. The fee's rate is
// each fund's own, set every year by the minister after hearing the fund (FundFee), so the user gives it
// and the rule set holds none: FundFeeRate cites that text for the rate given. The director deducts the
// fee with the contributions as their own (FundFeeDeduction, read with FundFeeCommentary: every
// contribution the law makes them pay to their fund), Netto's reading where no text names the fee as
// deductible.
public sealed record SocialContributionRules(
    int IncomeYear,
    Source Source,
    RuleAmount MinimumIncome,
    RuleAmount FirstBandRate,
    RuleAmount FirstBandCeiling,
    RuleAmount SecondBandRate,
    RuleAmount SecondBandCeiling,
    Source FundFee,
    Source FundFeeDeduction,
    Source FundFeeCommentary)
{
    private static readonly Source Inasti2026 = new(
        "INASTI, « Cotisations 2026 » (January 2026)",
        "§1 B.2 « Cotisations définitives, en général », per note to the funds P741/25/9 of 11.12.2025",
        new Uri("https://www.inasti.be/sites/rsvz/files/2024-01/folder_cotisations_sociales.pdf"),
        "social-contributions/inasti_cotisations-sociales-2026_2026-01-05.pdf");

    // Cited by the decree's name alone, which a file's citations share; its reference says the article.
    private static readonly Source RoyalDecree38FundFee = SharedSources.RoyalDecree38 with
    {
        Reference = "Art. 20 §4: the funds' management costs are borne by their members, whose share is a contribution set for each fund every year by the minister, "
            + "after hearing the fund, and perceived and recovered as the contributions are",
    };

    // The record the forager read, the text in force from 01.01.2021, until the coordinated article for
    // income year 2026 is filed.
    private static readonly Source Article52Income2026 = Source.Fisconet("personal-tax", 52, 2026, new DateOnly(2025, 11, 17), "265ddbb6-3664-4499-944d-8d5177e98d98",
        "7°: the personal contributions due under social legislation are deductible professional costs");

    private static readonly Source Commentary52 = new(
        "Commentaire du CIR 92, n° 52/124, on Fisconetplus, last modified 09.11.2020",
        "Those subject to the social status of the self-employed deduct as professional costs every contribution the law makes them pay to the social insurance fund of their choice",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/588446ce-27fa-4638-94b4-8fcbe94d1510"),
        "personal-tax/spf-finances_commentaire-cir92-art-52_2020-11-09.html") { Label = "Com.IR 92, n° 52/124" };

    public static SocialContributionRules IncomeYear2026 { get; } = new(
        IncomeYear: 2026,
        Source: Inasti2026,
        MinimumIncome: new RuleAmount("minimum_income", 17_374.08m, Inasti2026),
        FirstBandRate: new RuleAmount("first_band_rate", 0.205m, Inasti2026),
        FirstBandCeiling: new RuleAmount("first_band_ceiling", 75_024.54m, Inasti2026),
        SecondBandRate: new RuleAmount("second_band_rate", 0.1416m, Inasti2026),
        SecondBandCeiling: new RuleAmount("second_band_ceiling", 110_562.42m, Inasti2026),
        FundFee: RoyalDecree38FundFee,
        FundFeeDeduction: Article52Income2026,
        FundFeeCommentary: Commentary52);

    public static SocialContributionRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };

    public IReadOnlyList<RuleAmount> Amounts =>
        [MinimumIncome, FirstBandRate, FirstBandCeiling, SecondBandRate, SecondBandCeiling];

    // The rate of their fund's management fee the user gives, a fraction (3.05% is 0.0305): it has no
    // published amount, so it cites the text that sets each fund's fee.
    public RuleAmount FundFeeRate(decimal rate) => new("fund_fee_rate", rate, FundFee);
}
