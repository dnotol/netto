using Netto.Core.Rules;

namespace Netto.Core.DirectorPay;

// The monthly withholding tax (précompte professionnel) on the periodic pay of a director subject to
// the self-employed social status and resident in Belgium, for one income year: the rules of annex III
// to the AR/CIR 92 that the administration's key formula applies in five steps, for a single person
// (n° 32) and for a spouse or legal cohabitant whose partner has no professional income, the marital
// quotient (n° 34), whose maximum for partners both 66 or over the annex sets only from some dates
// (none before: the one maximum holds whatever their ages). Source is
// the key formula, the method the withholding step cites as its rule. The rules hold for pay paid or
// attributed from RulesFrom, the first of a month, until Later's: the rules a decree puts in their
// place from a later month of the same income year, none when these hold to December. For(year)
// gives the year's first rules, those of 1 January, whose Later holds the ones after them.
public sealed record WithholdingRules(
    int IncomeYear,
    Source Source,
    RuleDate RulesFrom,
    IReadOnlyList<WithholdingBand> Reduction,
    RuleAmount FlatRateCostsRate,
    RuleAmount FlatRateCostsMaximum,
    IReadOnlyList<WithholdingBand> Scale,
    RuleAmount TaxFreeAmountReduction,
    RuleAmount QuotientRate,
    RuleAmount QuotientMaximum,
    RuleAmount? QuotientMaximumBothAged66,
    RuleAmount JointTaxFreeAmountReduction)
{
    public WithholdingRules? Later { get; init; }

    // The key formula is the method; the amounts it applies are annex III as the royal decree of
    // 11.12.2025 replaced it (filed as « Règles 1er janvier 2026 »); the coordinated annex III dates
    // each decree, the one of 27.09.2026 among them, and holds n° 32 as that decree changes it.
    private static readonly Source KeyFormula2026 = new(
        "Key formula for the withholding tax on pay paid from 1 January 2026 (ESS-SR/2025-0928), Fisconetplus, version 17.12.2025",
        "N° 6 to 16, the monthly pay in five steps: the gross less the director's reduction (n° 7, 1°), times 12 (n° 7, 2°); "
        + "less the flat-rate costs (n° 8, 2°); the basic tax on the scale of annexe 1, with the 7% municipal surcharge built in (n° 5), "
        + "less the tax on the tax-free amount of a single person (n° 11, a); divided by 12 (n° 16); never below zero (n° 21). "
        + "N° 3: each step rounded to the cent, up from five thousandths",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/389035ca-432e-422b-859d-2d32fbdaa4cf"),
        "personal-tax/spf-finances_formule-cle-precompte-professionnel-2026-01-01_2025-12-17.pdf");

    private static readonly Source CoordinatedAnnex2026 = new(
        "Annexe III, AR/CIR 92 (revenus 2026), coordinated text on Fisconetplus, last modified 13.10.2026",
        "Its header dates each decree: the rules of the royal decree of 11.12.2025 (MB 29.12.2025) apply to pay paid or attributed from 01.01.2026; "
        + "the royal decree of 27.09.2026 (MB 02.10.2026) changes n° 32 to 3,089.63, the tax on a tax-free amount of 11,550, and the marital quotient's "
        + "n° 34.1.1 and 34.2, for pay from 01.11.2026, and nothing else Netto applies to a director's monthly pay",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/0c174ec5-448b-4d1e-983b-fc457e5e7e28"),
        "personal-tax/spf-finances_arcir92-annexe-iii-revenus-2026_2026-10-13.html");

    private static readonly Source AnnexReduction2026 = DecreedAnnex2026(
        "N° 25.2, the monthly reduction, in place of social deductions, on the pay of a director subject to the self-employed social status: "
        + "385 up to 1,465; 385 + 21.50% of the slice over 1,465 up to 6,310; 1,426.68 + 14.50% of the slice over 6,310 up to 9,290; 1,858.78 above");

    private static readonly Source AnnexFlatRateCosts2026 = DecreedAnnex2026(
        "N° 29, the flat-rate costs on a director's periodic pay: 3% of the annual gross, 3,200 at most (reached at 106,666.67)");

    private static readonly Source AnnexScale2026 = DecreedAnnex2026(
        "N° 31, the basic scale: 26.75% up to 16,710; 4,469.93 + 42.80% of the slice over 16,710 up to 29,500; "
        + "9,944.05 + 48.15% of the slice over 29,500 up to 51,050; 20,320.38 + 53.50% of the slice over 51,050");

    private static readonly Source AnnexTaxFreeAmount2026 = DecreedAnnex2026(
        "N° 32, a single person: the tax on the scale less 2,987.98, the tax on the tax-free amount of 11,170");

    private static readonly Source AnnexQuotient2026 = DecreedAnnex2026(
        "N° 34, a partner without professional income of their own: 30% of the annual net taxable imputed to them (n° 34.1.1), "
        + "13,790 at most (reached at 45,966.67); the tax on the imputed income and the tax on the rest, each on the scale of n° 31, "
        + "summed less 5,975.96, twice the tax on the tax-free amount of 11,170 (n° 34.2)");

    private static readonly Source CoordinatedQuotientFromNovember2026 = CoordinatedAnnex2026 with
    {
        Reference = "N° 34.1.1 and 34.2, a partner without professional income of their own, as the royal decree of 27.09.2026 (MB 02.10.2026) "
            + "changes them for pay paid or attributed from 01.11.2026: the income imputed to the partner, 30% of the annual net taxable, "
            + "at most 12,790 when both partners have reached 66 on 1 January 2027 (reached at 42,633.33), in all other cases 11,780 "
            + "(reached at 39,266.67), also when the payer lacks the information; the two taxes summed less 6,179.26, "
            + "twice the tax on the tax-free amount of 11,550",
    };

    private static readonly Source CoordinatedTaxFreeAmountFromNovember2026 = CoordinatedAnnex2026 with
    {
        Reference = "N° 32, a single person, as the royal decree of 27.09.2026 (MB 02.10.2026) changes it for pay paid or attributed from 01.11.2026: "
            + "the tax on the scale less 3,089.63, the tax on the tax-free amount of 11,550",
    };

    private static readonly WithholdingRules From1January2026 = new(
        IncomeYear: 2026,
        Source: KeyFormula2026,
        RulesFrom: new RuleDate("rules_from", new DateOnly(2026, 1, 1), CoordinatedAnnex2026),
        Reduction: Reduction2026(),
        FlatRateCostsRate: new RuleAmount("flat_rate_costs_rate", 0.03m, AnnexFlatRateCosts2026),
        FlatRateCostsMaximum: new RuleAmount("flat_rate_costs_maximum", 3_200m, AnnexFlatRateCosts2026),
        Scale:
        [
            new WithholdingBand(null, Scale2026("withholding_bracket_1_rate", 0.2675m), Scale2026("withholding_bracket_1_upper_limit", 16_710m)),
            new WithholdingBand(Scale2026("withholding_bracket_2_base", 4_469.93m), Scale2026("withholding_bracket_2_rate", 0.428m), Scale2026("withholding_bracket_2_upper_limit", 29_500m)),
            new WithholdingBand(Scale2026("withholding_bracket_3_base", 9_944.05m), Scale2026("withholding_bracket_3_rate", 0.4815m), Scale2026("withholding_bracket_3_upper_limit", 51_050m)),
            new WithholdingBand(Scale2026("withholding_bracket_4_base", 20_320.38m), Scale2026("withholding_bracket_4_rate", 0.535m), null),
        ],
        TaxFreeAmountReduction: new RuleAmount("tax_free_amount_reduction", 2_987.98m, AnnexTaxFreeAmount2026),
        QuotientRate: new RuleAmount("withholding_quotient_rate", 0.30m, AnnexQuotient2026),
        QuotientMaximum: new RuleAmount("withholding_quotient_maximum", 13_790m, AnnexQuotient2026),
        QuotientMaximumBothAged66: null,
        JointTaxFreeAmountReduction: new RuleAmount("withholding_joint_tax_free_amount_reduction", 5_975.96m, AnnexQuotient2026));

    // From 1 November 2026, the royal decree of 27.09.2026 changes n° 32 and the marital quotient's
    // maximum and amount taken off (n° 34.1.1 and 34.2) of what reaches a director's monthly pay; no key
    // formula of that date is published, so the January one's method applies.
    public static WithholdingRules IncomeYear2026 { get; } = From1January2026 with
    {
        Later = From1January2026 with
        {
            RulesFrom = new RuleDate("rules_from", new DateOnly(2026, 11, 1), CoordinatedAnnex2026),
            TaxFreeAmountReduction = new RuleAmount("tax_free_amount_reduction", 3_089.63m, CoordinatedTaxFreeAmountFromNovember2026),
            QuotientMaximum = new RuleAmount("withholding_quotient_maximum", 11_780m, CoordinatedQuotientFromNovember2026),
            QuotientMaximumBothAged66 = new RuleAmount("withholding_quotient_maximum_both_aged_66", 12_790m, CoordinatedQuotientFromNovember2026),
            JointTaxFreeAmountReduction = new RuleAmount("withholding_joint_tax_free_amount_reduction", 6_179.26m, CoordinatedQuotientFromNovember2026),
        },
    };

    public static WithholdingRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };

    // The rules for pay paid or attributed in a month of the income year, 1 being January: those in
    // force on its first day.
    public WithholdingRules InMonth(int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, YearParts.Months);

        return Later is { } later && later.RulesFrom.Value <= new DateOnly(IncomeYear, month, 1) ? later.InMonth(month) : this;
    }

    // These rules, then each that replaces them later in the year, in order. A method, not a property:
    // the rules tests walk a rules record's properties, and one holding these rules again would never
    // end.
    public IReadOnlyList<WithholdingRules> Periods() => [this, .. Later?.Periods() ?? []];

    // Each figure of the annex the single method applies once, in the annex's order: the 385 the first
    // two bands of the reduction share is listed once.
    public IReadOnlyList<RuleAmount> Amounts =>
    [
        .. UpToTheScale,
        TaxFreeAmountReduction,
    ];

    // The marital quotient's own figures, in the annex's order, the maximum for partners both 66 or over
    // where the annex sets one.
    public IReadOnlyList<RuleAmount> QuotientAmounts =>
    [
        QuotientRate,
        QuotientMaximum,
        .. QuotientMaximumBothAged66 is { } bothAged66 ? [bothAged66] : Array.Empty<RuleAmount>(),
        JointTaxFreeAmountReduction,
    ];

    // The figures by the household's method: the reduction, the flat-rate costs and the scale, then the
    // single person's n° 32, or the quotient's rate, the one maximum that applies and its n° 34.2.
    public IReadOnlyList<RuleAmount> AmountsFor(WithholdingHousehold household) => household switch
    {
        WithholdingHousehold.AsSingle => Amounts,
        WithholdingHousehold.PartnerWithoutIncome or WithholdingHousehold.PartnerWithoutIncomeBothAged66 =>
            [.. UpToTheScale, QuotientRate, QuotientMaximumFor(household), JointTaxFreeAmountReduction],
        _ => throw new ArgumentOutOfRangeException(nameof(household), household, "No withholding method for this household."),
    };

    // The most the quotient imputes to the partner: the maximum for partners both 66 or over where the
    // annex sets one and they are, else the one for all other cases.
    public RuleAmount QuotientMaximumFor(WithholdingHousehold household) =>
        household == WithholdingHousehold.PartnerWithoutIncomeBothAged66 && QuotientMaximumBothAged66 is { } bothAged66 ? bothAged66 : QuotientMaximum;

    // The figures both methods apply, up to the scale.
    private IReadOnlyList<RuleAmount> UpToTheScale =>
    [
        .. Reduction.SelectMany(band => band.Amounts).Distinct(),
        FlatRateCostsRate,
        FlatRateCostsMaximum,
        .. Scale.SelectMany(band => band.Amounts),
    ];

    // The table gives 385 up to 1,465, then adds 21.50% of the slice over 1,465 to that same 385.
    private static IReadOnlyList<WithholdingBand> Reduction2026()
    {
        var minimum = Reduction2026("reduction_minimum", 385m);
        return
        [
            new WithholdingBand(minimum, null, Reduction2026("reduction_band_1_upper_limit", 1_465m)),
            new WithholdingBand(minimum, Reduction2026("reduction_band_2_rate", 0.215m), Reduction2026("reduction_band_2_upper_limit", 6_310m)),
            new WithholdingBand(Reduction2026("reduction_band_3_base", 1_426.68m), Reduction2026("reduction_band_3_rate", 0.145m), Reduction2026("reduction_band_3_upper_limit", 9_290m)),
            new WithholdingBand(Reduction2026("reduction_maximum", 1_858.78m), null, null),
        ];
    }

    private static RuleAmount Reduction2026(string key, decimal value) => new(key, value, AnnexReduction2026);

    private static RuleAmount Scale2026(string key, decimal value) => new(key, value, AnnexScale2026);

    private static Source DecreedAnnex2026(string reference) => new(
        "Annexe III to the AR/CIR 92 as replaced by the royal decree of 11.12.2025 (MB 29.12.2025), « Règles 1er janvier 2026 » on Fisconetplus, version 17.12.2025",
        reference,
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/536a741e-81d6-4605-ad11-8e07ebc4a440"),
        "personal-tax/spf-finances_regles-precompte-professionnel-2026-01-01_2025-12-17.pdf");
}
