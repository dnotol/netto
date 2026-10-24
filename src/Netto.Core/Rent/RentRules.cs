using Netto.Core.Rules;

namespace Netto.Core.Rent;

// The rent a company pays its director for the part of their home it uses as an office, unfurnished,
// for one income year. Income year 2025 is here for the Tax-Calc tests only. The company deducts the
// rent like any other cost; the director declares it as income from immovable property, a built
// property let to a company: the rent, never less than the indexed cadastral income plus 40% (art. 7,
// §1, 2°, c), less flat charges of 40%, at most 2/3 of the cadastral income revalued by art. 13's
// coefficient (art. 13). What the rent exceeds 5/3 of that revalued cadastral income by is director
// pay, with no charges (art. 32, al. 2, 3°): Netto keeps the rent at or below that cap. The cadastral
// income is indexed by art. 518's coefficient, which the year's indexation notice publishes.
public sealed record RentRules(
    int IncomeYear,
    Source RentAsIncome,
    Source NetRent,
    Source Cap,
    RuleAmount CapMultiplier,
    RuleAmount RevaluationCoefficient,
    RuleAmount FloorUplift,
    RuleAmount IndexationCoefficient,
    RuleAmount ChargesRate,
    RuleAmount ChargesLimitMultiplier,
    IReadOnlyList<LeverCondition> Conditions)
{
    private static readonly Source Article7Income2025 = new(
        "CIR 92, art. 7 (revenus 2025), coordinated text on Fisconetplus, last modified 25.11.2024",
        "§1, 2°, c: a built property let otherwise than to a natural person for housing, to a company, yields the total of the rent and the rental benefits, never less than the cadastral income plus 40%",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/90f0344b-4877-42fd-a0d5-761d3d974c88"),
        "personal-tax/spf-finances_cir92-art-7-revenus-2025_2024-11-25.html") { Label = "CIR 92, art. 7" };

    private static readonly Source Article7Income2026 = new(
        "CIR 92, art. 7 (revenus 2026), coordinated text on Fisconetplus, last modified 02.03.2026",
        "§1, 2°, c: a built property let otherwise than to a natural person for housing, to a company, yields the total of the rent and the rental benefits, never less than the cadastral income plus 40%",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/583b6854-60c4-42fc-83bf-5476d1fd3e33"),
        "personal-tax/spf-finances_cir92-art-7-revenus-2026_2026-03-02.html") { Label = "CIR 92, art. 7" };

    private static readonly Source Article13Income2025 = new(
        "CIR 92, art. 13 (revenus 2025), coordinated text on Fisconetplus, last modified 23.01.2025",
        "The net rent is the rent less 40% for maintenance and repairs, for a property let under art. 7, §1, 2°, c at most 2/3 of the cadastral income revalued by the coefficient 5.63",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/4a6920bd-c5d9-42b6-a300-4a70c584a27d"),
        "personal-tax/spf-finances_cir92-art-13-revenus-2025_2025-01-23.html");

    private static readonly Source Article13Income2026 = new(
        "CIR 92, art. 13 (revenus 2026), coordinated text on Fisconetplus, last modified 02.03.2026",
        "The net rent is the rent less 40% for maintenance and repairs, for a property let under art. 7, §1, 2°, c at most 2/3 of the cadastral income revalued by the coefficient 5.75",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/649627a2-38e4-424a-8421-bbae6332266f"),
        "personal-tax/spf-finances_cir92-art-13-revenus-2026_2026-03-02.html");

    private static readonly Source Article32Income2025 = SharedSources.Article32Income2025 with
    {
        Reference = "Al. 2, 3°: the rent a director receives from their company for a built property is director pay where it exceeds 5/3 of the cadastral income revalued by art. 13's coefficient, with no costs deducted from that part",
    };

    private static readonly Source Article32Income2026 = SharedSources.Article32Income2026 with
    {
        Reference = "Al. 2, 3°: the rent a director receives from their company for a built property is director pay where it exceeds 5/3 of the cadastral income revalued by art. 13's coefficient, with no costs deducted from that part",
    };

    private static readonly Source IndexationNotice2026 = SharedSources.IndexationNotice2026 with
    {
        Reference = "H: the coefficient of art. 518 CIR 92, which indexes the cadastral income, is 2.2446 for income 2025",
    };

    private static readonly Source IndexationNotice2027 = SharedSources.IndexationNotice2027 with
    {
        Reference = "J: the coefficient of art. 518 CIR 92, which indexes the cadastral income, is 2.3000 for income 2026",
    };

    private static readonly Source Circular2021C20 = SharedSources.Circular2021C20 with
    {
        Reference = "§31: the flat-rate office allowance covers, among others, the use of an office space in the home, its rent included",
        Label = "Circular 2021/C/20, §31",
    };

    public static RentRules IncomeYear2025 { get; } = Of(
        incomeYear: 2025,
        article7: Article7Income2025,
        article13: Article13Income2025,
        article32: Article32Income2025,
        revaluation: 5.63m,
        indexation: new RuleAmount("cadastral_income_indexation_coefficient", 2.2446m, IndexationNotice2026));

    public static RentRules IncomeYear2026 { get; } = Of(
        incomeYear: 2026,
        article7: Article7Income2026,
        article13: Article13Income2026,
        article32: Article32Income2026,
        revaluation: 5.75m,
        indexation: new RuleAmount("cadastral_income_indexation_coefficient", 2.3000m, IndexationNotice2027));

    public static RentRules? For(int incomeYear) => incomeYear switch
    {
        2025 => IncomeYear2025,
        2026 => IncomeYear2026,
        _ => null,
    };

    // The fractions and rates are the same both years; the coefficients and the texts are the year's.
    private static RentRules Of(int incomeYear, Source article7, Source article13, Source article32, decimal revaluation, RuleAmount indexation) => new(
        incomeYear,
        article7,
        article13,
        article32,
        new RuleAmount("rent_cap_multiplier", 5m / 3m, article32),
        new RuleAmount("cadastral_income_revaluation_coefficient", revaluation, article13),
        new RuleAmount("rent_floor_uplift_rate", 0.40m, article7),
        indexation,
        new RuleAmount("rent_charges_rate", 0.40m, article13),
        new RuleAmount("rent_charges_limit_multiplier", 2m / 3m, article13),
        [
            new LeverCondition("share_yours_to_justify", [article7, article32]),
            new LeverCondition("not_with_office_allowance", [Circular2021C20]),
        ]);
}
