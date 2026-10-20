using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// Personal income tax of a single resident of Wallonia whose only income is director pay, for one
// income year. Income year 2025 is here for the tests only: it is the latest year Tax-Calc covers,
// so it proves the method and the rounding against the administration's own simulator.
// The special social security contribution collected with the tax is not here: a self-employed
// person in main occupation does not owe it (law of 30.03.1994, art. 106 §4 and art. 125, 2°,
// still so after the law of 15.07.2026 makes it per person from income year 2028). The tax credit
// for low earned income is, as a self-employed director's pay is activity income (art. 289ter, §1,
// al. 2, which leaves out only directors under an employment contract and side activities).
public sealed record PersonalTaxRules(
    int IncomeYear,
    IReadOnlyList<TaxBracket> Scale,
    RuleAmount TaxFreeAmount,
    RuleAmount TaxFreeAmountRate,
    RuleAmount FlatRateCostsRate,
    RuleAmount FlatRateCostsMaximum,
    RuleAmount AutonomyFactor,
    RuleAmount RegionalAdditionalRate,
    LowEarnedIncomeCreditRules LowEarnedIncomeCredit,
    IncomeTaxAdvancePaymentRules AdvancePayments)
{
    // Each amount cites the administration's coordinated article of its own income year, which carries
    // that year's indexed amounts (filed in official-docs/personal-tax/).
    private static readonly Source Article130Income2025 = new(
        "CIR 92, art. 130 (revenus 2025), coordinated text on Fisconetplus, last modified 24.01.2025",
        "The scale: 25% up to 16,320, 40% up to 28,800, 45% up to 49,840, 50% above",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/68216320-026d-4a35-a81d-1221bfe21b53"),
        "personal-tax/spf-finances_cir92-art-130-revenus-2025_2025-01-24.html");

    private static readonly Source Article130Income2026 = new(
        "CIR 92, art. 130 (revenus 2026), coordinated text on Fisconetplus, last modified 18.02.2026",
        "The scale: 25% up to 16,720, 40% up to 29,510, 45% up to 51,070, 50% above",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/aff72054-004a-4858-8747-1d70964c8e3d"),
        "personal-tax/spf-finances_cir92-art-130-revenus-2026_2026-02-18.html");

    private static readonly Source Article131Income2025 = new(
        "CIR 92, art. 131 (revenus 2025), coordinated text on Fisconetplus, last modified 23.01.2025",
        "Tax-free amount of 10,910",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/d888718e-4593-44a2-bb28-7e26d17baa94"),
        "personal-tax/spf-finances_cir92-art-131-revenus-2025_2025-01-23.html");

    private static readonly Source Article131Income2026 = new(
        "CIR 92, art. 131 (revenus 2026), coordinated text on Fisconetplus, last modified 03.08.2026",
        "Tax-free amount of 11,550, as raised by the law of 15.07.2026",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/2fb89ed0-b93e-47d7-816c-85f1ac679b8a"),
        "personal-tax/spf-finances_cir92-art-131-revenus-2026_2026-08-03.html");

    private static readonly Source Article134Income2025 = new(
        "CIR 92, art. 134 (revenus 2025), coordinated text on Fisconetplus, last modified 14.01.2026",
        "§2: the tax on the tax-free amount, deducted from the tax on the scale, is 25% of it up to 11,460",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/09b708c4-3d54-468c-a652-9053bd16e2eb"),
        "personal-tax/spf-finances_cir92-art-134-revenus-2025_2026-01-14.html");

    private static readonly Source Article134Income2026 = new(
        "CIR 92, art. 134 (revenus 2026), coordinated text on Fisconetplus, last modified 05.08.2026",
        "§2: the tax on the tax-free amount, deducted from the tax on the scale, is 25% of it up to 11,750",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/56f7c2a3-94ab-45dd-85d0-2a0c7f6ddebc"),
        "personal-tax/spf-finances_cir92-art-134-revenus-2026_2026-08-05.html");

    private static readonly Source Article51Income2025 = new(
        "CIR 92, art. 51 (revenus 2025), coordinated text on Fisconetplus, last modified 14.01.2026",
        "A director's pay, less the own social contributions of art. 52, 7°: flat-rate costs of 3%, at most 3,130",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/1c87b91e-3a27-418d-9743-bfa0fecdc5e2"),
        "personal-tax/spf-finances_cir92-art-51-revenus-2025_2026-01-14.html");

    private static readonly Source Article51Income2026 = new(
        "CIR 92, art. 51 (revenus 2026), coordinated text on Fisconetplus, last modified 18.02.2026",
        "A director's pay, less the own social contributions of art. 52, 7°: flat-rate costs of 3%, at most 3,200",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/21dd9b61-2989-4125-a2e0-6142c607eec8"),
        "personal-tax/spf-finances_cir92-art-51-revenus-2026_2026-02-18.html");

    private static readonly Source AutonomyFactorDecree = new(
        "Royal decree of 19.12.2017 fixing the autonomy factor (MB 22.12.2017)",
        "Art. 1: definitive autonomy factor of 24.957% from assessment year 2018; special law of 16.01.1989, art. 5/2",
        new Uri("https://www.ejustice.just.fgov.be/eli/arrete/2017/12/19/2017032216/justel"),
        "personal-tax/ejustice_arrete-royal-facteur-autonomie-2017_2017-12-22.html");

    private static readonly Source WalloonTaxDecree = new(
        "Walloon decree of 13.12.2017 « portant diverses modifications fiscales » (MB 22.12.2017)",
        "Art. 33: regional additional tax of 33.257% of the reduced State tax, from assessment year 2018 (art. 39)",
        new Uri("https://www.ejustice.just.fgov.be/eli/decret/2017/12/13/2017032048/justel"),
        "personal-tax/ejustice_decret-wallon-modifications-fiscales-2017_2017-12-22.html");

    // The law of 15.07.2026 leaves art. 289ter as it was; its title 16 changes art. 289ter/1, the
    // employees' « bonus emploi fiscal ». What the credit leaves over after the tax is set against the
    // municipal surcharge and refunded (art. 304, §2), so it reaches the pocket in full.
    private static readonly Source Article289terIncome2025 = new(
        "CIR 92, art. 289ter (revenus 2025), coordinated text on Fisconetplus, last modified 30.01.2025",
        "Tax credit for low earned income: none above 28,420 of total net income; on the activity income, from 6,550 rising to 880 at 8,740, 880 up to 21,860, falling to none at 28,420. Refunded beyond the tax (art. 304, §2)",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/e8b166ab-07c3-4964-aa70-716fb1dae0be"),
        "personal-tax/spf-finances_cir92-art-289ter-revenus-2025_2025-01-30.html");

    private static readonly Source Article289terIncome2026 = new(
        "CIR 92, art. 289ter (revenus 2026), coordinated text on Fisconetplus, last modified 19.02.2026",
        "Tax credit for low earned income: none above 29,120 of total net income; on the activity income, from 6,710 rising to 910 at 8,960, 910 up to 22,400, falling to none at 29,120. Refunded beyond the tax (art. 304, §2)",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/af20504b-71e6-4daa-89d6-4d3c000c8718"),
        "personal-tax/spf-finances_cir92-art-289ter-revenus-2026_2026-02-19.html");

    // The rules the steps cite. Only income year 2026 runs produce steps (GrossToNet has no other
    // year's contributions), so these are the 2026 articles; they become per year with the next one.
    public static Source DirectorPayDeductions => Article51Income2026;

    public static Source IncomeTaxScale => Article130Income2026;

    public static Source MunicipalSurcharge { get; } = new(
        "CIR 92, art. 466 and 468 (revenus 2026), coordinated text on Fisconetplus, art. 466 last modified 23.04.2026",
        "Art. 466: the municipal additional tax is computed on the total tax, the federal share and the regional tax together. Art. 468: at one uniform rate per municipality, with at most one decimal",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/aa4241fb-2007-4bf8-aaf7-a5cb46dcb853"),
        "municipal-surcharge/spf-finances_cir92-art-466-revenus-2026_2026-04-23.html");

    // The balance on the assessment: what was withheld and paid in advance comes off the tax, and
    // what they leave over is refunded. §2, al. 1 is unchanged since tax year 2018, so the text holds
    // for income year 2025 as well (official-docs/INDEX.md).
    public static Source AssessmentBalance { get; } = new(
        "CIR 92, art. 304 (revenus 2026), coordinated text on Fisconetplus, last modified 19.02.2026",
        "§2, al. 1: the excess of the advance payments of art. 157 and 175, the withholding tax of art. 270 to 272 and the tax credit of art. 289ter "
        + "over the tax is set against the municipal surcharge, and the rest is refunded once it reaches 2.50",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/bda433e6-7da6-4dc9-882c-0c9d92591877"),
        "personal-tax/spf-finances_cir92-art-304-revenus-2026_2026-02-19.html");

    public static PersonalTaxRules IncomeYear2025 { get; } = new(
        2025,
        [
            new TaxBracket(new RuleAmount("bracket_1_rate", 0.25m, Article130Income2025), new RuleAmount("bracket_1_upper_limit", 16_320m, Article130Income2025)),
            new TaxBracket(new RuleAmount("bracket_2_rate", 0.40m, Article130Income2025), new RuleAmount("bracket_2_upper_limit", 28_800m, Article130Income2025)),
            new TaxBracket(new RuleAmount("bracket_3_rate", 0.45m, Article130Income2025), new RuleAmount("bracket_3_upper_limit", 49_840m, Article130Income2025)),
            new TaxBracket(new RuleAmount("bracket_4_rate", 0.50m, Article130Income2025), null),
        ],
        new RuleAmount("tax_free_amount", 10_910m, Article131Income2025),
        new RuleAmount("tax_free_amount_rate", 0.25m, Article134Income2025),
        new RuleAmount("flat_rate_costs_rate", 0.03m, Article51Income2025),
        new RuleAmount("flat_rate_costs_maximum", 3_130m, Article51Income2025),
        new RuleAmount("autonomy_factor", 0.24957m, AutonomyFactorDecree),
        new RuleAmount("regional_additional_rate", 0.33257m, WalloonTaxDecree),
        new LowEarnedIncomeCreditRules(
            Article289terIncome2025,
            new RuleAmount("low_earned_income_credit", 880m, Article289terIncome2025),
            new RuleAmount("low_earned_income_floor", 6_550m, Article289terIncome2025),
            new RuleAmount("low_earned_income_full_from", 8_740m, Article289terIncome2025),
            new RuleAmount("low_earned_income_full_up_to", 21_860m, Article289terIncome2025),
            new RuleAmount("low_earned_income_ceiling", 28_420m, Article289terIncome2025)),
        IncomeTaxAdvancePaymentRules.IncomeYear2025);

    public static PersonalTaxRules IncomeYear2026 { get; } = new(
        2026,
        [
            new TaxBracket(new RuleAmount("bracket_1_rate", 0.25m, Article130Income2026), new RuleAmount("bracket_1_upper_limit", 16_720m, Article130Income2026)),
            new TaxBracket(new RuleAmount("bracket_2_rate", 0.40m, Article130Income2026), new RuleAmount("bracket_2_upper_limit", 29_510m, Article130Income2026)),
            new TaxBracket(new RuleAmount("bracket_3_rate", 0.45m, Article130Income2026), new RuleAmount("bracket_3_upper_limit", 51_070m, Article130Income2026)),
            new TaxBracket(new RuleAmount("bracket_4_rate", 0.50m, Article130Income2026), null),
        ],
        new RuleAmount("tax_free_amount", 11_550m, Article131Income2026),
        new RuleAmount("tax_free_amount_rate", 0.25m, Article134Income2026),
        new RuleAmount("flat_rate_costs_rate", 0.03m, Article51Income2026),
        new RuleAmount("flat_rate_costs_maximum", 3_200m, Article51Income2026),
        new RuleAmount("autonomy_factor", 0.24957m, AutonomyFactorDecree),
        new RuleAmount("regional_additional_rate", 0.33257m, WalloonTaxDecree),
        new LowEarnedIncomeCreditRules(
            Article289terIncome2026,
            new RuleAmount("low_earned_income_credit", 910m, Article289terIncome2026),
            new RuleAmount("low_earned_income_floor", 6_710m, Article289terIncome2026),
            new RuleAmount("low_earned_income_full_from", 8_960m, Article289terIncome2026),
            new RuleAmount("low_earned_income_full_up_to", 22_400m, Article289terIncome2026),
            new RuleAmount("low_earned_income_ceiling", 29_120m, Article289terIncome2026)),
        IncomeTaxAdvancePaymentRules.IncomeYear2026);

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
