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
    private static readonly Source Article7Income2025 = Source.Fisconet("personal-tax", "7", 2025, new DateOnly(2024, 11, 25), "90f0344b-4877-42fd-a0d5-761d3d974c88", "rent_to_a_company");

    private static readonly Source Article7Income2026 = Source.Fisconet("personal-tax", "7", 2026, new DateOnly(2026, 3, 2), "583b6854-60c4-42fc-83bf-5476d1fd3e33", "rent_to_a_company");

    private static readonly Source Article13Income2025 = Source.Fisconet("personal-tax", "13", 2025, new DateOnly(2025, 1, 23), "4a6920bd-c5d9-42b6-a300-4a70c584a27d", "net_rent");

    private static readonly Source Article13Income2026 = Source.Fisconet("personal-tax", "13", 2026, new DateOnly(2026, 3, 2), "649627a2-38e4-424a-8421-bbae6332266f", "net_rent");

    private static readonly Source Article32Income2025 = SharedSources.Article32Income2025 with { Passage = "rent_as_director_pay" };

    private static readonly Source Article32Income2026 = SharedSources.Article32Income2026 with { Passage = "rent_as_director_pay" };

    private static readonly Source IndexationNotice2026 = SharedSources.IndexationNotice2026 with { Passage = "cadastral_coefficient" };

    private static readonly Source IndexationNotice2027 = SharedSources.IndexationNotice2027 with { Passage = "cadastral_coefficient" };

    private static readonly Source Circular2021C20 = SharedSources.Circular2021C20 with { Passage = "office_allowance_covers_rent" };

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
