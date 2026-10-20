using Netto.Core.Rules;

namespace Netto.Core.DirectorPay;

// The monthly withholding tax (précompte professionnel) on the periodic pay of a director subject to
// the self-employed social status, single and resident in Belgium, for one income year: the rules of
// annex III to the AR/CIR 92 that the administration's key formula applies in five steps. Source is
// the key formula, the method the withholding step cites as its rule. The rules hold for pay from
// RulesFrom. From UnmodelledRulesFrom, rules exist that Netto does not model (for 2026, the royal
// decree of 27.09.2026 changes the tax on the tax-free amount for pay from 1 November): a month
// from that date still uses these rules, and the caller says so (decided 2026-10-18).
public sealed record WithholdingRules(
    int IncomeYear,
    Source Source,
    RuleDate RulesFrom,
    RuleDate UnmodelledRulesFrom,
    IReadOnlyList<WithholdingBand> Reduction,
    RuleAmount FlatRateCostsRate,
    RuleAmount FlatRateCostsMaximum,
    IReadOnlyList<WithholdingBand> Scale,
    RuleAmount TaxFreeAmountReduction)
{
    // The key formula is the method; the amounts it applies are annex III as the royal decree of
    // 11.12.2025 replaced it (filed as « Règles 1er janvier 2026 »); the coordinated annex III dates
    // each decree, the one of 27.09.2026 among them.
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
        + "the royal decree of 27.09.2026 (MB 02.10.2026) changes n° 32 to 3,089.63, the tax on a tax-free amount of 11,550, for pay from 01.11.2026, "
        + "and nothing else that reaches a single director's monthly pay",
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

    public static WithholdingRules IncomeYear2026 { get; } = new(
        2026,
        KeyFormula2026,
        new RuleDate("rules_from", new DateOnly(2026, 1, 1), CoordinatedAnnex2026),
        new RuleDate("unmodelled_rules_from", new DateOnly(2026, 11, 1), CoordinatedAnnex2026),
        Reduction2026(),
        new RuleAmount("flat_rate_costs_rate", 0.03m, AnnexFlatRateCosts2026),
        new RuleAmount("flat_rate_costs_maximum", 3_200m, AnnexFlatRateCosts2026),
        [
            new WithholdingBand(null, Scale2026("withholding_bracket_1_rate", 0.2675m), Scale2026("withholding_bracket_1_upper_limit", 16_710m)),
            new WithholdingBand(Scale2026("withholding_bracket_2_base", 4_469.93m), Scale2026("withholding_bracket_2_rate", 0.428m), Scale2026("withholding_bracket_2_upper_limit", 29_500m)),
            new WithholdingBand(Scale2026("withholding_bracket_3_base", 9_944.05m), Scale2026("withholding_bracket_3_rate", 0.4815m), Scale2026("withholding_bracket_3_upper_limit", 51_050m)),
            new WithholdingBand(Scale2026("withholding_bracket_4_base", 20_320.38m), Scale2026("withholding_bracket_4_rate", 0.535m), null),
        ],
        new RuleAmount("tax_free_amount_reduction", 2_987.98m, AnnexTaxFreeAmount2026));

    public static WithholdingRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };

    // Each figure of the annex once, in the annex's order: the 385 the first two bands of the
    // reduction share is listed once.
    public IReadOnlyList<RuleAmount> Amounts =>
    [
        .. Reduction.SelectMany(band => band.Amounts).Distinct(),
        FlatRateCostsRate,
        FlatRateCostsMaximum,
        .. Scale.SelectMany(band => band.Amounts),
        TaxFreeAmountReduction,
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
