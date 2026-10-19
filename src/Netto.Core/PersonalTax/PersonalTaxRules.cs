using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// Personal income tax of a single resident of Wallonia whose only income is director pay, for one
// income year. Income year 2025 is here for the tests only: it is the latest year Tax-Calc covers,
// so it proves the method and the rounding against the administration's own simulator.
public sealed record PersonalTaxRules(
    int IncomeYear,
    IReadOnlyList<TaxBracket> Scale,
    RuleAmount TaxFreeAmount,
    RuleAmount TaxFreeAmountRate,
    RuleAmount FlatRateCostsRate,
    RuleAmount FlatRateCostsMaximum,
    RuleAmount AutonomyFactor,
    RuleAmount RegionalAdditionalRate)
{
    private static readonly Source TaxRatesPage = new(
        "SPF Finances, « Taux d'imposition »",
        "Brackets for income years 2025 and 2026; tax-free amount for income year 2025",
        new Uri("https://fin.belgium.be/fr/particuliers/declaration-impot/revenus/taux-imposition"));

    private static readonly Source IndexationNotice2027 = new(
        "Indexation notice for assessment year 2027, version 03.08.2026, adapted to the law of 15.07.2026",
        "CIR 92 art. 131 (tax-free amount), art. 134 §2 (special scale, first slice 11,750 at 25%), art. 51 (director flat-rate maximum)",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/042cb526-403e-4f40-9a45-dbccd0a12583"));

    // The income year 2026 edition is not published yet; the 3% rate is unchanged.
    private static readonly Source ReturnExplanations2026Part2 = new(
        "SPF Finances, return explanations 2026, part 2 (income year 2025)",
        "Cadre XVI: own social contributions are deducted, then flat-rate costs of 3%, at most 3,130",
        new Uri("https://fin.belgium.be/sites/default/files/media/documents/explications-partie-2-2026.pdf"));

    private static readonly Source AutonomyFactorDecree = new(
        "Royal decree of 19.12.2017 fixing the autonomy factor (MB 22.12.2017)",
        "Art. 1: definitive autonomy factor of 24.957% from assessment year 2018; special law of 16.01.1989, art. 5/2",
        new Uri("https://www.ejustice.just.fgov.be/eli/arrete/2017/12/19/2017032216/justel"));

    private static readonly Source WalloonTaxDecree = new(
        "Walloon decree of 13.12.2017 « portant diverses modifications fiscales » (MB 22.12.2017)",
        "Art. 33: regional additional tax of 33.257% of the reduced State tax, from assessment year 2018 (art. 39)",
        new Uri("https://www.ejustice.just.fgov.be/eli/decret/2017/12/13/2017032048/justel"));

    public static Source DirectorPayDeductions => ReturnExplanations2026Part2;

    public static Source IncomeTaxScale => TaxRatesPage;

    public static Source MunicipalSurcharge { get; } = new(
        "Code des impôts sur les revenus 1992",
        "Art. 465 to 470: municipal additional tax on the State tax and the regional additional tax, at the municipality's rate",
        new Uri("https://www.ejustice.just.fgov.be/cgi_loi/change_lg.pl?language=fr&la=F&cn=1992041032&table_name=loi"));

    public static PersonalTaxRules IncomeYear2025 { get; } = new(
        2025,
        [
            new TaxBracket(new RuleAmount("bracket_1_rate", 0.25m, TaxRatesPage), new RuleAmount("bracket_1_upper_limit", 16_320m, TaxRatesPage)),
            new TaxBracket(new RuleAmount("bracket_2_rate", 0.40m, TaxRatesPage), new RuleAmount("bracket_2_upper_limit", 28_800m, TaxRatesPage)),
            new TaxBracket(new RuleAmount("bracket_3_rate", 0.45m, TaxRatesPage), new RuleAmount("bracket_3_upper_limit", 49_840m, TaxRatesPage)),
            new TaxBracket(new RuleAmount("bracket_4_rate", 0.50m, TaxRatesPage), null),
        ],
        new RuleAmount("tax_free_amount", 10_910m, TaxRatesPage),
        new RuleAmount("tax_free_amount_rate", 0.25m, TaxRatesPage),
        new RuleAmount("flat_rate_costs_rate", 0.03m, ReturnExplanations2026Part2),
        new RuleAmount("flat_rate_costs_maximum", 3_130m, ReturnExplanations2026Part2),
        new RuleAmount("autonomy_factor", 0.24957m, AutonomyFactorDecree),
        new RuleAmount("regional_additional_rate", 0.33257m, WalloonTaxDecree));

    public static PersonalTaxRules IncomeYear2026 { get; } = new(
        2026,
        [
            new TaxBracket(new RuleAmount("bracket_1_rate", 0.25m, TaxRatesPage), new RuleAmount("bracket_1_upper_limit", 16_720m, TaxRatesPage)),
            new TaxBracket(new RuleAmount("bracket_2_rate", 0.40m, TaxRatesPage), new RuleAmount("bracket_2_upper_limit", 29_510m, TaxRatesPage)),
            new TaxBracket(new RuleAmount("bracket_3_rate", 0.45m, TaxRatesPage), new RuleAmount("bracket_3_upper_limit", 51_070m, TaxRatesPage)),
            new TaxBracket(new RuleAmount("bracket_4_rate", 0.50m, TaxRatesPage), null),
        ],
        new RuleAmount("tax_free_amount", 11_550m, IndexationNotice2027),
        new RuleAmount("tax_free_amount_rate", 0.25m, IndexationNotice2027),
        new RuleAmount("flat_rate_costs_rate", 0.03m, ReturnExplanations2026Part2),
        new RuleAmount("flat_rate_costs_maximum", 3_200m, IndexationNotice2027),
        new RuleAmount("autonomy_factor", 0.24957m, AutonomyFactorDecree),
        new RuleAmount("regional_additional_rate", 0.33257m, WalloonTaxDecree));

    public static PersonalTaxRules? For(int incomeYear) => incomeYear switch
    {
        2025 => IncomeYear2025,
        2026 => IncomeYear2026,
        _ => null,
    };

    public IReadOnlyList<RuleAmount> FlatRateCostsAmounts => [FlatRateCostsRate, FlatRateCostsMaximum];

    public IReadOnlyList<RuleAmount> IncomeTaxAmounts
    {
        get
        {
            var amounts = new List<RuleAmount>();
            foreach (var bracket in Scale)
            {
                amounts.Add(bracket.Rate);
                if (bracket.UpperLimit is not null)
                    amounts.Add(bracket.UpperLimit);
            }

            amounts.AddRange([TaxFreeAmount, TaxFreeAmountRate, AutonomyFactor, RegionalAdditionalRate]);
            return amounts;
        }
    }
}
