using Netto.Core.Rules;

namespace Netto.Core.Dividends;

// The withholding on a dividend the director receives from their company, which is the final tax on
// it, and the exemption that gives back the withholding on its first part through the personal
// return, for one income year. Income year 2025 is here for the Tax-Calc tests only.
// In steady state the year's dividend comes out of the year's own profit and is paid the next year,
// so a 2026 profit's VVPRbis dividend takes the rate for payments from 01.07.2026 (decided 2026-10-19).
public sealed record DividendRules(
    int IncomeYear,
    RuleAmount StandardRate,
    RuleAmount VvprbisSecondYearRate,
    RuleAmount VvprbisThirdYearOnwardsRate,
    RuleAmount ExemptAmount)
{
    private static readonly Source Article269Income2025 = SharedSources.Article269Income2025 with
    {
        Reference = "§1, 1°: 30%. §2, al. 2: VVPRbis shares, 1° 20% for the second accounting year's distribution after a contribution made by 31.12.2025, 2° 15% from the third",
    };

    private static readonly Source Article269Income2026 = SharedSources.Article269Income2026 with
    {
        Reference = "§1, 1°: 30%. §2, al. 2: VVPRbis shares, 1° 20% for the second accounting year's distribution after a contribution made by 31.12.2025, 2° 18% from the third for dividends paid or attributed from 01.07.2026 (programme law of 30.05.2026, art. 15, 4°, and 18)",
    };

    private static readonly Source Article21Income2025 = new(
        "CIR 92, art. 21 (revenus 2025), coordinated text on Fisconetplus, last modified 27.01.2026",
        "Al. 1, 14°: the first 833 of ordinary dividends are exempt from personal tax",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/2121eb14-ea6a-4fa1-b09e-ccc3bec9b2f4"),
        "personal-tax/spf-finances_cir92-art-21-revenus-2025_2026-01-27.html");

    private static readonly Source Article21Income2026 = new(
        "CIR 92, art. 21 (revenus 2026), coordinated text on Fisconetplus, last modified 08.06.2026",
        "Al. 1, 14°: the first 833 of ordinary dividends are exempt from personal tax",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/77a7ba86-4df1-4091-ac75-171cdd277a25"),
        "personal-tax/spf-finances_cir92-art-21-revenus-2026_2026-06-08.html");

    // The income year 2026 edition is not published yet; the exemption is claimed the same way.
    private static readonly Source ReturnExplanations2026Part1 = new(
        "SPF Finances, return explanations 2026, part 1, Wallonia (income year 2025)",
        "Cadre VII, c): exempt dividends are not declared; the withholding retained on them, for at most 833 of dividends, is declared and credited",
        new Uri("https://fin.belgium.be/sites/default/files/media/documents/explications-partie-1-rw-2026.pdf"),
        "personal-tax/spf-finances_explications-partie-1-rw-2026_2026-04-13.pdf");

    public static Source ExemptionRefund => ReturnExplanations2026Part1;

    public static DividendRules IncomeYear2025 { get; } = new(
        2025,
        new RuleAmount("dividend_standard_rate", 0.30m, Article269Income2025),
        new RuleAmount("vvprbis_second_year_rate", 0.20m, Article269Income2025),
        new RuleAmount("vvprbis_third_year_onwards_rate", 0.15m, Article269Income2025),
        new RuleAmount("exempt_dividends", 833m, Article21Income2025));

    public static DividendRules IncomeYear2026 { get; } = new(
        2026,
        new RuleAmount("dividend_standard_rate", 0.30m, Article269Income2026),
        new RuleAmount("vvprbis_second_year_rate", 0.20m, Article269Income2026),
        new RuleAmount("vvprbis_third_year_onwards_rate", 0.18m, Article269Income2026),
        new RuleAmount("exempt_dividends", 833m, Article21Income2026));

    public static DividendRules? For(int incomeYear) => incomeYear switch
    {
        2025 => IncomeYear2025,
        2026 => IncomeYear2026,
        _ => null,
    };

    public RuleAmount Rate(WithholdingCategory category) => category switch
    {
        WithholdingCategory.Standard => StandardRate,
        WithholdingCategory.VvprbisSecondYear => VvprbisSecondYearRate,
        WithholdingCategory.VvprbisThirdYearOnwards => VvprbisThirdYearOnwardsRate,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };
}
