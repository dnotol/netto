using Netto.Core.Rules;

namespace Netto.Core.SocialContributions;

// INASTI's yearly flat contribution every company owes (« cotisation à charge des sociétés », law of
// 30.12.1992, title III, chapter II), for one income year, the company's accounting year being the calendar
// year: the lower amount while the balance-sheet total of its penultimate closed financial year is at most
// the threshold, the upper one above it (art. 91), both indexed each year by the law's own formula and
// published by INASTI (Source), and nothing for a company one of its two exemptions covers (art. 94, 9°
// and 92bis; Exemption), which the user declares as its band and whose conditions they confirm
// (Conditions). For the income taxes it is of the same nature as the contributions due under social
// legislation (art. 100), so the company deducts it as a professional cost (Deduction). Its due date and
// the increase for paying late (art. 92 and 93) are not modelled. A bill pending in the Chamber would
// change the 2026 amounts retroactively; the rules are the law in force.
public sealed record CompanyContributionRules(
    int IncomeYear,
    Source Source,
    RuleAmount BalanceSheetThreshold,
    RuleAmount LowerBand,
    RuleAmount UpperBand,
    RuleAmount Exemption,
    Source Law,
    Source Deduction,
    IReadOnlyList<LeverCondition> Conditions)
{
    private static readonly Source Faq2026 = new(
        "contribution_bands",
        new Uri("https://www.inasti.be/fr/faq/quelle-cotisation-charge-des-societes-dois-je-payer"),
        "social-contributions/inasti_faq-cotisation-a-charge-des-societes_2026-10-25.html");

    private static readonly Source Law1992 = new(
        "flat_contribution",
        new Uri("https://www.ejustice.just.fgov.be/eli/loi/1992/12/30/1993021370/justel"),
        "social-contributions/ejustice_loi-dispositions-sociales-diverses-1992_2022-12-30.html");

    public static CompanyContributionRules IncomeYear2026 { get; } = new(
        IncomeYear: 2026,
        Source: Faq2026,
        BalanceSheetThreshold: new RuleAmount("balance_sheet_threshold", 879_163.44m, Faq2026),
        LowerBand: new RuleAmount("company_contribution_lower_band", 409.30m, Faq2026),
        UpperBand: new RuleAmount("company_contribution_upper_band", 1_022.38m, Faq2026),
        Exemption: new RuleAmount("company_contribution_exempt", 0m, Law1992 with { Passage = "exemptions" }),
        Law: Law1992,
        Deduction: SharedSources.Article49Income2026 with { Passage = "contribution_deductible" },
        Conditions:
        [
            new LeverCondition("not_a_starting_partnership",
            [
                Law1992 with { Passage = "starting_partnership" },
            ]),
            new LeverCondition("active_all_year",
            [
                Law1992 with { Passage = "inactive_company" },
            ]),
        ]);

    public static CompanyContributionRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };

    public IReadOnlyList<RuleAmount> Amounts => [BalanceSheetThreshold, LowerBand, UpperBand, Exemption];

    // The year whose balance-sheet total sets the band: the penultimate financial year closed as it
    // stood on 1 January of the contribution year (Law, art. 91), two years before it for a company
    // whose accounting year is the calendar year, as Netto assumes.
    public int BalanceSheetYear => IncomeYear - 2;

    // What the company owes for the year in the band its balance-sheet total sets, or nothing when the
    // law exempts it.
    public RuleAmount Contribution(CompanyContributionBand band) => band switch
    {
        CompanyContributionBand.Lower => LowerBand,
        CompanyContributionBand.Upper => UpperBand,
        CompanyContributionBand.Exempt => Exemption,
        _ => throw new ArgumentOutOfRangeException(nameof(band), band, "INASTI's company contribution has two bands and an exemption."),
    };

    // The amounts the contribution's step cites: the band's and the balance-sheet total that sets it, or
    // the exemption alone, which no balance sheet sets.
    public IReadOnlyList<RuleAmount> Used(CompanyContributionBand band) =>
        band == CompanyContributionBand.Exempt ? [Exemption] : [Contribution(band), BalanceSheetThreshold];
}
