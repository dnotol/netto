using Netto.Core.Rules;

namespace Netto.Core.CompanyCar;

// A car the company puts at its director's disposal for private use, for one income year. The
// director is taxed on its benefit in kind as pay (art. 31 and 32): 6/7 of its catalogue value times
// a CO2 percentage, taken at the share of the catalogue value its age leaves, at least the year's
// minimum, less the director's own contribution (art. 36 §2), each period of the year weighed by its
// calendar days (the administration's FAQ). The company deducts the car's costs at the rate its
// order date, fuel and CO2 give (art. 66), and adds back to its taxable income a share of the benefit
// before the contribution, 17%, or 40% when it pays the fuel for private use (art. 198 §1, 9° and
// 9°bis), which for an electric car is its charging (the SPF course's Module 5.3). This income year
// holds an electric car ordered or leased before 2027: the CO2 percentage is the minimum, and its
// costs stay deductible in full for its whole life (circular 2024/C/50). A fuel, hybrid or plug-in
// car adds its CO2 percentage and its deduction schedule beside these. The benefit taxed is pay
// (BenefitAsPay), so it is in the income INASTI's contributions are owed on (BenefitInContributions).
public sealed record CarRules(
    int IncomeYear,
    Source Benefit,
    Source BenefitMethod,
    Source BenefitAsPay,
    Source BenefitInContributions,
    Source Deduction,
    Source Acquisition,
    Source AddBack,
    Source ChargingReading,
    RuleAmount CatalogueShare,
    RuleAmount MinimumCo2Percentage,
    IReadOnlyList<CarAgeBand> AgeBands,
    RuleAmount MinimumBenefit,
    RuleAmount AddBackRate,
    RuleAmount AddBackRateWithPrivateEnergy,
    RuleAmount ZeroEmissionDeduction,
    IReadOnlyList<LeverCondition> Conditions)
{
    private static readonly Source Article36Income2026 = new(
        "CIR 92, art. 36 (revenus 2026), coordinated text on Fisconetplus, last modified 15.06.2026",
        "§2: the benefit of a car put at a person's disposal for private use is a CO2 percentage of 6/7 of its catalogue value, as sold new to a private buyer, options and VAT paid included, discounts excluded; "
        + "the catalogue value is taken at 100% up to 12 months from the first registration, then 94, 88, 82 and 76% by twelve months, 70% from the 61st, a month begun counting whole; "
        + "the percentage is at least 4%, which a car that emits no CO2 takes; the benefit is at least 1,690 a year (indexed amount), less the beneficiary's own contribution",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/7f4492e1-5ef2-42a9-a179-04fc3bcc5d55"),
        "personal-tax/spf-finances_cir92-art-36-revenus-2026_2026-06-15.html") { Label = "CIR 92, art. 36" };

    private static readonly Source CarBenefitFaq = new(
        "FAQ détaillées sur les avantages de toute nature résultant de l'utilisation à des fins personnelles d'un véhicule mis à disposition, Fisconetplus, last modified 15.03.2022",
        "The age is counted from the first day of the month of the first registration; the year's benefit weighs each period at its percentage by its calendar days out of the year's, each part rounded to the cent",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/8c873a40-df98-4d8d-832a-6baff8743303"),
        "personal-tax/spf-finances_faq-avantage-vehicule-mis-a-disposition_2022-03-15.html") { Label = "SPF FAQ on car benefits" };

    private static readonly Source Article31Income2026 = new(
        "CIR 92, art. 31 (revenus 2026), coordinated text on Fisconetplus, last modified 30.07.2026",
        "Al. 2, 2°: pay includes the benefits in kind obtained because of the activity, which art. 32 applies to a director's pay",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/72ee79ba-3c3f-47a6-83a5-f0a1ec0e5209"),
        "personal-tax/spf-finances_cir92-art-31-revenus-2026_2026-07-30.html") { Label = "CIR 92, art. 31" };

    private static readonly Source RoyalDecree38 = new(
        "Arrêté royal n° 38 du 27.07.1967 organisant le statut social des travailleurs indépendants, consolidated on Justel, updated to 01.06.2026",
        "Art. 11 §1 and §2: the contributions are a percentage of the professional income as the income tax law sets it, gross less the professional costs",
        new Uri("https://www.ejustice.just.fgov.be/eli/arrete/1967/07/27/1967072702/justel"),
        "social-contributions/ejustice_arrete-royal-38-statut-social-independants_2026-06-01.html") { Label = "Royal decree n° 38, art. 11" };

    private static readonly Source Article66Income2026 = new(
        "CIR 92, art. 66 (revenus 2026), coordinated text on Fisconetplus, last modified 15.06.2026",
        "§1: the costs of a car of art. 65 are not deductible unless it emits no CO2; §3, 3°: the part of the costs equal to the benefit taxed and to the beneficiary's contribution stays deductible",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/f233da76-42c8-4f81-a098-33aef1b6e8d8"),
        "corporate-tax/spf-finances_cir92-art-66-revenus-2026_2026-06-15.html") { Label = "CIR 92, art. 66" };

    private static readonly Source Circular2024C50 = new(
        "Circulaire 2024/C/50 avec la FAQ relative au verdissement fiscal de la mobilité, Fisconetplus, last modified 23.07.2024",
        "Question 4: a car that emits no CO2 bought, leased or rented up to 2026 stays deductible at 100% for its whole life; a car ordered and not delivered counts as bought, a lease or rental from its contract's date",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/45e88e8f-fbd0-402d-8c2d-92356e8f4075"),
        "corporate-tax/spf-finances_circulaire-2024-c-50-verdissement-mobilite_2024-07-23.html") { Label = "Circular 2024/C/50" };

    private static readonly Source Article198Income2026 = new(
        "CIR 92, art. 198 (revenus 2026), coordinated text on Fisconetplus, last modified 10.02.2026",
        "§1, 9°: the costs of a car put at someone's disposal for private use are not professional costs up to 17% of the benefit as art. 36 §2, al. 1 to 12 sets it; 9°bis: 40% when the company bears all or part of the fuel for that use",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/5cf99cca-101c-4b02-924e-35e149217648"),
        "corporate-tax/spf-finances_cir92-art-198-revenus-2026_2026-02-10.html") { Label = "CIR 92, art. 198" };

    private static readonly Source Module53 = new(
        "SPF Finances, Cours de base impôt des sociétés, MODULE 5.3 : Dépenses non admises et autres éléments du résultat, Fisconetplus, last modified 17.03.2023",
        "Point 8.2, p. 80: for an electric car, 40% when the company provides a charging point whose costs it bears, or a charging card, unless it re-invoices and is repaid the electricity for private use; "
        + "the beneficiary's contribution does not lower the amount added back",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/8cfb00cb-901d-4252-82f1-b1b00c7e026d"),
        "corporate-tax/spf-finances_cours-isoc-module-5-3-depenses-non-admises_2023-03-17.pdf") { Label = "SPF course, Module 5.3" };

    public static CarRules IncomeYear2026 { get; } = new(
        2026,
        Article36Income2026,
        CarBenefitFaq,
        Article31Income2026,
        RoyalDecree38,
        Article66Income2026,
        Circular2024C50,
        Article198Income2026,
        Module53,
        new RuleAmount("catalogue_share_rate", 6m / 7m, Article36Income2026),
        new RuleAmount("minimum_co2_rate", 0.04m, Article36Income2026),
        [
            new CarAgeBand(1, new RuleAmount("age_up_to_12_months_rate", 1.00m, Article36Income2026)),
            new CarAgeBand(13, new RuleAmount("age_13_to_24_months_rate", 0.94m, Article36Income2026)),
            new CarAgeBand(25, new RuleAmount("age_25_to_36_months_rate", 0.88m, Article36Income2026)),
            new CarAgeBand(37, new RuleAmount("age_37_to_48_months_rate", 0.82m, Article36Income2026)),
            new CarAgeBand(49, new RuleAmount("age_49_to_60_months_rate", 0.76m, Article36Income2026)),
            new CarAgeBand(61, new RuleAmount("age_from_61_months_rate", 0.70m, Article36Income2026)),
        ],
        new RuleAmount("minimum_car_benefit", 1_690m, Article36Income2026),
        new RuleAmount("car_add_back_rate", 0.17m, Article198Income2026),
        new RuleAmount("car_add_back_with_private_energy_rate", 0.40m, Module53),
        new RuleAmount("zero_emission_deduction_rate", 1.00m, Article66Income2026),
        [
            new LeverCondition("electric_ordered_before_2027", [Article66Income2026, Circular2024C50]),
            new LeverCondition("private_use_all_year", [Article36Income2026, CarBenefitFaq]),
        ]);

    public static CarRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };
}

// The share of the catalogue value the benefit takes from the month of the car's age FromMonth on,
// the month of its first registration being the first.
public sealed record CarAgeBand(int FromMonth, RuleAmount Coefficient);
