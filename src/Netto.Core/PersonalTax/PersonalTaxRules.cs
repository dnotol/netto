using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// Personal income tax of a resident of one region whose income is director pay and what the levers
// hand it, taxed alone or jointly with a spouse or legal cohabitant, for one income year: Wallonia alone, whose regional additional rate it holds, so the
// rules of another region are none (For). Income year 2025 is here for the tests only: it is the latest
// year Tax-Calc covers, so it proves the method and the rounding against the administration's own
// simulator.
// The special social security contribution collected with the tax is not here: a self-employed
// person in main occupation does not owe it (law of 30.03.1994, art. 106 §4 and art. 125, 2°,
// still so after the law of 15.07.2026 makes it per person from income year 2028). The tax credit
// for low earned income is, as a self-employed director's pay is activity income (art. 289ter, §1,
// al. 2, which leaves out only directors under an employment contract and side activities).
// IncomeTaxScale, DirectorPayDeductions and AssessmentBalance are the year's texts the pay's steps
// and its assessment cite as their rule. TaxFreeAmountScale is art. 134 §2's scale of the tax on the
// tax-free amount, whose first bracket covers a single person's amount and whose next ones a joint
// assessment reaches when one partner's unused part passes to the other (§4); TaxFreeAmountSharing is
// that §4, which a joint assessment's step cites besides the scale and the marital quotient.
// Supplements are the dependants' supplements to the tax-free amount and the cap of the refundable credit
// for children (art. 132 to 134 §3).
public sealed record PersonalTaxRules(
    int IncomeYear,
    Region Region,
    IReadOnlyList<TaxBracket> Scale,
    RuleAmount TaxFreeAmount,
    IReadOnlyList<TaxBracket> TaxFreeAmountScale,
    RuleAmount FlatRateCostsRate,
    RuleAmount FlatRateCostsMaximum,
    RuleAmount AutonomyFactor,
    RuleAmount RegionalAdditionalRate,
    LowEarnedIncomeCreditRules LowEarnedIncomeCredit,
    MaritalQuotientRules MaritalQuotient,
    TaxFreeAmountSupplementRules Supplements,
    IncomeTaxAdvancePaymentRules AdvancePayments,
    Source IncomeTaxScale,
    Source DirectorPayDeductions,
    Source AssessmentBalance,
    Source TaxFreeAmountSharing)
{
    // Each amount cites the administration's coordinated article of its own income year, which carries
    // that year's indexed amounts (filed in official-docs/personal-tax/).
    private static readonly Source Article130Income2025 = Source.Fisconet("personal-tax", "130", 2025, new DateOnly(2025, 1, 24), "68216320-026d-4a35-a81d-1221bfe21b53",
        "The scale: 25% up to 16,320, 40% up to 28,800, 45% up to 49,840, 50% above");

    private static readonly Source Article130Income2026 = Source.Fisconet("personal-tax", "130", 2026, new DateOnly(2026, 2, 18), "aff72054-004a-4858-8747-1d70964c8e3d",
        "The scale: 25% up to 16,720, 40% up to 29,510, 45% up to 51,070, 50% above");

    private static readonly Source Article131Income2025 = Source.Fisconet("personal-tax", "131", 2025, new DateOnly(2025, 1, 23), "d888718e-4593-44a2-bb28-7e26d17baa94",
        "Tax-free amount of 10,910");

    private static readonly Source Article131Income2026 = Source.Fisconet("personal-tax", "131", 2026, new DateOnly(2026, 8, 3), "2fb89ed0-b93e-47d7-816c-85f1ac679b8a",
        "Tax-free amount of 11,550, as raised by the law of 15.07.2026");

    private static readonly Source Article134Income2025 = Source.Fisconet("personal-tax", "134", 2025, new DateOnly(2026, 1, 14), "09b708c4-3d54-468c-a652-9053bd16e2eb",
        "§2: the tax on the tax-free amount, deducted from the tax on the scale, is 25% of it up to 11,460, 30% to 16,320, 40% to 27,190, 45% to 49,840, 50% above. "
            + "§4: in a joint assessment it is fixed per partner, and what one partner's taxable income leaves unused passes to the other");

    private static readonly Source Article134Income2026 = Source.Fisconet("personal-tax", "134", 2026, new DateOnly(2026, 8, 5), "56f7c2a3-94ab-45dd-85d0-2a0c7f6ddebc",
        "§2: the tax on the tax-free amount, deducted from the tax on the scale, is 25% of it up to 11,750, 30% to 16,720, 40% to 27,860, 45% to 51,070, 50% above. "
            + "§4: in a joint assessment it is fixed per partner, and what one partner's taxable income leaves unused passes to the other");

    private static readonly Source Article51Income2025 = Source.Fisconet("personal-tax", "51", 2025, new DateOnly(2026, 1, 14), "1c87b91e-3a27-418d-9743-bfa0fecdc5e2",
        "A director's pay, less the own social contributions of art. 52, 7°: flat-rate costs of 3%, at most 3,130");

    private static readonly Source Article51Income2026 = Source.Fisconet("personal-tax", "51", 2026, new DateOnly(2026, 2, 18), "21dd9b61-2989-4125-a2e0-6142c607eec8",
        "A director's pay, less the own social contributions of art. 52, 7°: flat-rate costs of 3%, at most 3,200");

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
    private static readonly Source Article289terIncome2025 = Source.Fisconet("personal-tax", "289ter", 2025, new DateOnly(2025, 1, 30), "e8b166ab-07c3-4964-aa70-716fb1dae0be",
        "Tax credit for low earned income: none above 28,420 of total net income; on the activity income, from 6,550 rising to 880 at 8,740, 880 up to 21,860, falling to none at 28,420. Refunded beyond the tax (art. 304, §2)");

    private static readonly Source Article289terIncome2026 = Source.Fisconet("personal-tax", "289ter", 2026, new DateOnly(2026, 2, 19), "af20504b-71e6-4daa-89d6-4d3c000c8718",
        "Tax credit for low earned income: none above 29,120 of total net income; on the activity income, from 6,710 rising to 910 at 8,960, 910 up to 22,400, falling to none at 29,120. Refunded beyond the tax (art. 304, §2)");

    // The marital quotient. Fisconetplus was down when it was researched, so art. 87 and 88 are not filed
    // yet: income year 2026's rate cites the law of 15.07.2026 that sets it from tax year 2027, and its
    // caps the indexation notice of that tax year; income year 2025's rate and cap cite the notice of
    // tax year 2026, which gives the cap under art. 87 al. 2 and 88 al. 1, the rate being the article's.
    // Each turns into Source.Fisconet once the article is filed. The law's explanatory memorandum (DOC
    // 56 1243/001, p. 16 and 17) reads the cap as including the partner's own professional income.
    private static readonly Source PersonalTaxReformLaw = new(
        "Loi du 15 juillet 2026 portant réforme de l'impôt des personnes physiques (MB 29.07.2026)",
        "Art. 27 and 30, A, from tax year 2027: art. 87, al. 2 attributes 30% of the professional income of the one partner who has any to the other, "
            + "at most 5,860, or 6,365 when both partners are 66 on 1 January of the tax year; art. 88 tops up a partner below 30% of the two together, within the same maximum",
        new Uri("https://www.ejustice.just.fgov.be/eli/loi/2026/07/15/2026005724/justel"),
        "personal-tax/ejustice_loi-reforme-impot-personnes-physiques-2026_2026-07-29.html") { Label = "Law of 15.07.2026" };

    private static readonly Source QuotientCapsTaxYear2027 = SharedSources.IndexationNotice2027 with
    {
        Reference = "Art. 87, al. 2 and art. 88, al. 1 and 2: the most the marital quotient attributes, base 6,365, indexed 12,790, when both partners are 66 "
            + "on 1 January of the tax year, otherwise base 5,860, indexed 11,780",
    };

    private static readonly Source QuotientTaxYear2026 = SharedSources.IndexationNotice2026 with
    {
        Reference = "Art. 87, al. 2 and art. 88, al. 1: the most the marital quotient, 30% of the professional income, attributes, base 6,700, indexed 13,460",
    };

    // The dependants' supplements. Art. 132 and 133 are not filed: each year's amounts cite the indexation
    // notice of its tax year, the table that lists them, table II of the notice for tax year 2027 (the child
    // supplements frozen at the coefficient 2.2793 of art. 178 §2/1) and table I of the notice for tax year
    // 2026, and turn into Source.Fisconet once the articles are filed. The credit and its cap cite art. 134
    // of the income year.
    private static readonly Source SupplementsTaxYear2027 = SharedSources.IndexationNotice2027 with
    {
        Reference = "Table II: art. 132 al. 1, the supplements to the tax-free amount for dependants: 1° one child 2,130, 2° two 5,130, 3° three 11,440, "
            + "4° four 18,510, 5° 7,070 more for each child beyond the fourth, 6° 740 for each child under 3 for whom no childcare costs are deducted, "
            + "7° 5,950 for each dependant aged 66 or more in a situation of dependence, 8° 1,980 for each other dependant; "
            + "art. 133 al. 1, 1°: 1,980 for a taxpayer taxed alone who has dependent children",
    };

    private static readonly Source SupplementsTaxYear2026 = SharedSources.IndexationNotice2026 with
    {
        Reference = "Table I: art. 132 al. 1, the supplements to the tax-free amount for dependants: 1° one child 1,980, 2° two 5,110, 3° three 11,440, "
            + "4° four 18,510, 5° 7,070 more for each child beyond the fourth, 6° 740 for each child under 3 for whom no childcare costs are deducted, "
            + "7° 5,950 for each dependant aged 66 or more in a situation of dependence, 8° 1,980 for each other dependant; "
            + "art. 133 al. 1, 1°: 1,980 for a taxpayer taxed alone who has dependent children",
    };

    private const string ChildCreditReference =
        "§3: the part of the tax on the tax-free amount that the basic tax cannot absorb, insofar as it is the child supplements of art. 132 al. 1, 1° to 6°, "
        + "counted after the base amount and the other supplements, the base and other supplements beyond the taxable income left out, is a refundable tax credit "
        + "of at most 550 per dependent child, not indexed; §4, 6°: in a joint assessment it is computed per partner, the cap for both together";

    private static readonly Source ChildCreditIncome2025 = Article134Income2025 with { Reference = ChildCreditReference };

    private static readonly Source ChildCreditIncome2026 = Article134Income2026 with { Reference = ChildCreditReference };

    // The balance on the assessment: what was withheld and paid in advance comes off the tax, and
    // what they leave over is refunded. §2, al. 1 is unchanged since tax year 2018, so the text holds
    // for income year 2025 as well (official-docs/INDEX.md).
    private static readonly Source Article304Income2026 = SharedSources.Article304Income2026 with
    {
        Reference = "§2, al. 1: the excess of the advance payments of art. 157 and 175, the withholding tax of art. 270 to 272 and the tax credit of art. 289ter "
            + "over the tax is set against the municipal surcharge, and the rest is refunded once it reaches 2.50",
    };

    public static PersonalTaxRules IncomeYear2025 { get; } = new(
        IncomeYear: 2025,
        Region: Region.Wallonia,
        Scale:
        [
            new TaxBracket(new RuleAmount("bracket_1_rate", 0.25m, Article130Income2025), new RuleAmount("bracket_1_upper_limit", 16_320m, Article130Income2025)),
            new TaxBracket(new RuleAmount("bracket_2_rate", 0.40m, Article130Income2025), new RuleAmount("bracket_2_upper_limit", 28_800m, Article130Income2025)),
            new TaxBracket(new RuleAmount("bracket_3_rate", 0.45m, Article130Income2025), new RuleAmount("bracket_3_upper_limit", 49_840m, Article130Income2025)),
            new TaxBracket(new RuleAmount("bracket_4_rate", 0.50m, Article130Income2025), null),
        ],
        new RuleAmount("tax_free_amount", 10_910m, Article131Income2025),
        TaxFreeAmountScale:
        [
            new TaxBracket(new RuleAmount("tax_free_amount_rate", 0.25m, Article134Income2025), new RuleAmount("tax_free_amount_bracket_1_upper_limit", 11_460m, Article134Income2025)),
            new TaxBracket(new RuleAmount("tax_free_amount_bracket_2_rate", 0.30m, Article134Income2025), new RuleAmount("tax_free_amount_bracket_2_upper_limit", 16_320m, Article134Income2025)),
            new TaxBracket(new RuleAmount("tax_free_amount_bracket_3_rate", 0.40m, Article134Income2025), new RuleAmount("tax_free_amount_bracket_3_upper_limit", 27_190m, Article134Income2025)),
            new TaxBracket(new RuleAmount("tax_free_amount_bracket_4_rate", 0.45m, Article134Income2025), new RuleAmount("tax_free_amount_bracket_4_upper_limit", 49_840m, Article134Income2025)),
            new TaxBracket(new RuleAmount("tax_free_amount_bracket_5_rate", 0.50m, Article134Income2025), null),
        ],
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
        new MaritalQuotientRules(
            QuotientTaxYear2026,
            new RuleAmount("marital_quotient_rate", 0.30m, QuotientTaxYear2026),
            new RuleAmount("marital_quotient_cap", 13_460m, QuotientTaxYear2026),
            new RuleAmount("marital_quotient_cap_both_aged_66", 13_460m, QuotientTaxYear2026)),
        new TaxFreeAmountSupplementRules(
            Source: SupplementsTaxYear2026,
            CreditSource: ChildCreditIncome2025,
            OneChild: new RuleAmount("supplement_one_child", 1_980m, SupplementsTaxYear2026),
            TwoChildren: new RuleAmount("supplement_two_children", 5_110m, SupplementsTaxYear2026),
            ThreeChildren: new RuleAmount("supplement_three_children", 11_440m, SupplementsTaxYear2026),
            FourChildren: new RuleAmount("supplement_four_children", 18_510m, SupplementsTaxYear2026),
            EachChildBeyondFour: new RuleAmount("supplement_each_child_beyond_four", 7_070m, SupplementsTaxYear2026),
            ChildUnder3: new RuleAmount("supplement_child_under_3", 740m, SupplementsTaxYear2026),
            SingleParent: new RuleAmount("supplement_single_parent", 1_980m, SupplementsTaxYear2026),
            DependantAged66InCare: new RuleAmount("supplement_dependant_aged_66_in_care", 5_950m, SupplementsTaxYear2026),
            OtherDependant: new RuleAmount("supplement_other_dependant", 1_980m, SupplementsTaxYear2026),
            ChildCreditCapPerChild: new RuleAmount("child_tax_credit_cap_per_child", 550m, ChildCreditIncome2025)),
        IncomeTaxAdvancePaymentRules.IncomeYear2025,
        IncomeTaxScale: Article130Income2025,
        DirectorPayDeductions: Article51Income2025,
        AssessmentBalance: Article304Income2026,
        TaxFreeAmountSharing: Article134Income2025);

    public static PersonalTaxRules IncomeYear2026 { get; } = new(
        IncomeYear: 2026,
        Region: Region.Wallonia,
        Scale:
        [
            new TaxBracket(new RuleAmount("bracket_1_rate", 0.25m, Article130Income2026), new RuleAmount("bracket_1_upper_limit", 16_720m, Article130Income2026)),
            new TaxBracket(new RuleAmount("bracket_2_rate", 0.40m, Article130Income2026), new RuleAmount("bracket_2_upper_limit", 29_510m, Article130Income2026)),
            new TaxBracket(new RuleAmount("bracket_3_rate", 0.45m, Article130Income2026), new RuleAmount("bracket_3_upper_limit", 51_070m, Article130Income2026)),
            new TaxBracket(new RuleAmount("bracket_4_rate", 0.50m, Article130Income2026), null),
        ],
        new RuleAmount("tax_free_amount", 11_550m, Article131Income2026),
        TaxFreeAmountScale:
        [
            new TaxBracket(new RuleAmount("tax_free_amount_rate", 0.25m, Article134Income2026), new RuleAmount("tax_free_amount_bracket_1_upper_limit", 11_750m, Article134Income2026)),
            new TaxBracket(new RuleAmount("tax_free_amount_bracket_2_rate", 0.30m, Article134Income2026), new RuleAmount("tax_free_amount_bracket_2_upper_limit", 16_720m, Article134Income2026)),
            new TaxBracket(new RuleAmount("tax_free_amount_bracket_3_rate", 0.40m, Article134Income2026), new RuleAmount("tax_free_amount_bracket_3_upper_limit", 27_860m, Article134Income2026)),
            new TaxBracket(new RuleAmount("tax_free_amount_bracket_4_rate", 0.45m, Article134Income2026), new RuleAmount("tax_free_amount_bracket_4_upper_limit", 51_070m, Article134Income2026)),
            new TaxBracket(new RuleAmount("tax_free_amount_bracket_5_rate", 0.50m, Article134Income2026), null),
        ],
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
        new MaritalQuotientRules(
            PersonalTaxReformLaw,
            new RuleAmount("marital_quotient_rate", 0.30m, PersonalTaxReformLaw),
            new RuleAmount("marital_quotient_cap", 11_780m, QuotientCapsTaxYear2027),
            new RuleAmount("marital_quotient_cap_both_aged_66", 12_790m, QuotientCapsTaxYear2027)),
        new TaxFreeAmountSupplementRules(
            Source: SupplementsTaxYear2027,
            CreditSource: ChildCreditIncome2026,
            OneChild: new RuleAmount("supplement_one_child", 2_130m, SupplementsTaxYear2027),
            TwoChildren: new RuleAmount("supplement_two_children", 5_130m, SupplementsTaxYear2027),
            ThreeChildren: new RuleAmount("supplement_three_children", 11_440m, SupplementsTaxYear2027),
            FourChildren: new RuleAmount("supplement_four_children", 18_510m, SupplementsTaxYear2027),
            EachChildBeyondFour: new RuleAmount("supplement_each_child_beyond_four", 7_070m, SupplementsTaxYear2027),
            ChildUnder3: new RuleAmount("supplement_child_under_3", 740m, SupplementsTaxYear2027),
            SingleParent: new RuleAmount("supplement_single_parent", 1_980m, SupplementsTaxYear2027),
            DependantAged66InCare: new RuleAmount("supplement_dependant_aged_66_in_care", 5_950m, SupplementsTaxYear2027),
            OtherDependant: new RuleAmount("supplement_other_dependant", 1_980m, SupplementsTaxYear2027),
            ChildCreditCapPerChild: new RuleAmount("child_tax_credit_cap_per_child", 550m, ChildCreditIncome2026)),
        IncomeTaxAdvancePaymentRules.IncomeYear2026,
        IncomeTaxScale: Article130Income2026,
        DirectorPayDeductions: Article51Income2026,
        AssessmentBalance: Article304Income2026,
        TaxFreeAmountSharing: Article134Income2026);

    public static PersonalTaxRules? For(int incomeYear, Region region) => (incomeYear, region) switch
    {
        (2025, Region.Wallonia) => IncomeYear2025,
        (2026, Region.Wallonia) => IncomeYear2026,
        _ => null,
    };

    // The rate of the scale's first bracket, which a single person's tax-free amount stays within.
    public RuleAmount TaxFreeAmountRate => TaxFreeAmountScale[0].Rate;

    public IReadOnlyList<RuleAmount> FlatRateCostsAmounts => [FlatRateCostsRate, FlatRateCostsMaximum];

    public IReadOnlyList<RuleAmount> TaxFreeAmountAmounts =>
        [.. TaxFreeAmountScale.SelectMany(bracket => bracket.UpperLimit is { } limit ? [bracket.Rate, limit] : new[] { bracket.Rate })];

    // What a joint assessment's income tax uses: a single person's amounts, the rest of the scale of the
    // tax on the tax-free amount, and the marital quotient's rate and the cap that applied.
    public IReadOnlyList<RuleAmount> JointIncomeTaxAmounts(bool bothAged66) =>
        [.. IncomeTaxAmounts, .. TaxFreeAmountAmounts.Except(IncomeTaxAmounts), MaritalQuotient.Rate, MaritalQuotient.CapFor(bothAged66)];

    // What the household's income tax uses: a joint assessment's amounts or a single person's, the rest of
    // the scale of the tax on the tax-free amount once dependants' supplements climb it, and the
    // supplements the dependants took; the credit's cap is the credit's step's.
    public IReadOnlyList<RuleAmount> IncomeTaxAmountsFor(Household household)
    {
        ArgumentNullException.ThrowIfNull(household);

        var supplements = Supplements.Used(household.Dependants, household.TaxedJointly).Where(amount => amount != Supplements.ChildCreditCapPerChild).ToList();
        IReadOnlyList<RuleAmount> amounts = household.TaxedJointly
            ? JointIncomeTaxAmounts(household.BothAged66)
            : supplements.Count > 0 ? [.. IncomeTaxAmounts, .. TaxFreeAmountAmounts.Except(IncomeTaxAmounts)] : IncomeTaxAmounts;

        return [.. amounts, .. supplements];
    }

    // The texts the household's income tax rests on besides the scale and its amounts': in a joint
    // assessment the marital quotient and art. 134 §4, and the source of the dependants' supplements.
    public IReadOnlyList<Source> IncomeTaxAlsoCites(Household household)
    {
        ArgumentNullException.ThrowIfNull(household);

        return
        [
            .. household.TaxedJointly ? [MaritalQuotient.Source, TaxFreeAmountSharing] : Array.Empty<Source>(),
            .. Supplements.Of(household.Dependants, household.TaxedJointly).Total > 0m ? [Supplements.Source] : Array.Empty<Source>(),
        ];
    }

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
