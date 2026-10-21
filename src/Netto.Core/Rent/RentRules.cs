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
    Source TeleworkCircular,
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
        "personal-tax/spf-finances_cir92-art-7-revenus-2025_2024-11-25.html");

    private static readonly Source Article7Income2026 = new(
        "CIR 92, art. 7 (revenus 2026), coordinated text on Fisconetplus, last modified 02.03.2026",
        "§1, 2°, c: a built property let otherwise than to a natural person for housing, to a company, yields the total of the rent and the rental benefits, never less than the cadastral income plus 40%",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/583b6854-60c4-42fc-83bf-5476d1fd3e33"),
        "personal-tax/spf-finances_cir92-art-7-revenus-2026_2026-03-02.html");

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

    private static readonly Source Article32Income2025 = new(
        "CIR 92, art. 32 (revenus 2025), coordinated text on Fisconetplus, last modified 02.12.2024",
        "Al. 2, 3°: the rent a director receives from their company for a built property is director pay where it exceeds 5/3 of the cadastral income revalued by art. 13's coefficient, with no costs deducted from that part",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/adf7c228-083c-4468-b8d1-151c1e03a067"),
        "personal-tax/spf-finances_cir92-art-32-revenus-2025_2024-12-02.html");

    private static readonly Source Article32Income2026 = new(
        "CIR 92, art. 32 (revenus 2026), coordinated text on Fisconetplus, last modified 29.01.2026",
        "Al. 2, 3°: the rent a director receives from their company for a built property is director pay where it exceeds 5/3 of the cadastral income revalued by art. 13's coefficient, with no costs deducted from that part",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/7628aa2c-168a-4bd8-aec4-288108a91757"),
        "personal-tax/spf-finances_cir92-art-32-revenus-2026_2026-01-29.html");

    private static readonly Source IndexationNotice2026 = new(
        "Indexation notice for assessment year 2026, version 19.01.2026",
        "H: the coefficient of art. 518 CIR 92, which indexes the cadastral income, is 2.2446 for income 2025",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/934106e5-36bd-4f61-8928-ba9ca100727f"),
        "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html");

    private static readonly Source IndexationNotice2027 = new(
        "Indexation notice for assessment year 2027, version 03.08.2026, adapted to the law of 15.07.2026",
        "J: the coefficient of art. 518 CIR 92, which indexes the cadastral income, is 2.3000 for income 2026",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/042cb526-403e-4f40-9a45-dbccd0a12583"),
        "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html");

    private static readonly Source Circular2021C20 = new(
        "Circulaire 2021/C/20 du 26.02.2021 relative aux interventions de l'employeur pour le télétravail, on Fisconetplus, last modified 09.09.2026",
        "§31: the flat-rate office allowance covers, among others, the use of an office space in the home, its rent included",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/c4b263eb-091c-4a11-8ad0-24e2b11bb7db"),
        "personal-tax/spf-finances_circulaire-2021-c-20-teletravail_2026-09-09.html");

    public static RentRules IncomeYear2025 { get; } = Of(2025, Article7Income2025, Article13Income2025, Article32Income2025, 5.63m,
        new RuleAmount("cadastral_income_indexation_coefficient", 2.2446m, IndexationNotice2026));

    public static RentRules IncomeYear2026 { get; } = Of(2026, Article7Income2026, Article13Income2026, Article32Income2026, 5.75m,
        new RuleAmount("cadastral_income_indexation_coefficient", 2.3000m, IndexationNotice2027));

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
        Circular2021C20,
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
