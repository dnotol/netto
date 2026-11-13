using Netto.Core.Rules;

namespace Netto.Core.CompanyCar;

// A car the company puts at its director's disposal for private use, for one income year. The
// director is taxed on its benefit in kind as pay (art. 31 and 32): 6/7 of its catalogue value times
// a CO2 percentage, taken at the share of the catalogue value its age leaves, at least the year's
// minimum, less the director's own contribution (art. 36 §2), each period of the year weighed by its
// calendar days (the administration's FAQ). The CO2 percentage is 5.5% at the year's reference
// emission for the car's fuel (art. 18 AR/CIR 92), a tenth of a point more or less per gram, between
// 4%, which a car that emits no CO2 takes, and 18%. The company deducts the car's costs at the rate
// its order date, fuel and CO2 give (art. 66 and 550, as art. 198bis leaves them to companies), and
// adds back to its taxable income a share of the benefit before the contribution, 17%, or 40% when it
// pays the fuel for private use (art. 198 §1, 9° and 9°bis), which for an electric car is its
// charging (the SPF course's Module 5.3). Every car is deducted by the one of its DeductionPeriods its
// order or lease date falls in; one that emits no CO2 at ZeroEmissionDeduction unless the period sets
// a rate of its own, which no 2026 period does: an electric car ordered or leased before 2027 stays
// deductible in full for its whole life (circular 2024/C/50). The benefit taxed is pay (BenefitAsPay),
// so it is in the income INASTI's contributions are owed on (BenefitInContributions).
public sealed record CarRules(
    int IncomeYear,
    Source Benefit,
    Source BenefitMethod,
    Source BenefitAsPay,
    Source BenefitInContributions,
    Source ReferenceDecree,
    Source FakeHybrid,
    Source Deduction,
    Source Acquisition,
    Source FormulaDeduction,
    Source CompanyExclusions,
    Source PlugInDeduction,
    Source AddBack,
    RuleAmount CatalogueShare,
    RuleAmount BaseCo2Percentage,
    RuleAmount Co2PercentagePerGram,
    RuleAmount MinimumCo2Percentage,
    RuleAmount MaximumCo2Percentage,
    RuleAmount PetrolReferenceEmission,
    RuleAmount DieselReferenceEmission,
    RuleAmount PetrolEmissionWithoutData,
    RuleAmount DieselEmissionWithoutData,
    RuleAmount FakeHybridWithoutMatch,
    IReadOnlyList<CarAgeBand> AgeBands,
    RuleAmount MinimumBenefit,
    RuleAmount AddBackRate,
    RuleAmount AddBackRateWithPrivateEnergy,
    RuleAmount ZeroEmissionDeduction,
    RuleAmount FormulaBase,
    RuleAmount FormulaPerGram,
    RuleAmount DieselCoefficient,
    RuleAmount OtherFuelCoefficient,
    RuleAmount DieselHybridCoefficient,
    RuleAmount SmallNaturalGasCoefficient,
    IReadOnlyList<CarDeductionPeriod> DeductionPeriods,
    DateOnly FakeHybridFrom,
    DateOnly PlugInFuelCappedFrom,
    DateOnly PlugInFuelCappedUntil,
    IReadOnlyList<LeverCondition> Conditions)
{
    private static readonly Source Article36Income2026 = Source.Fisconet("personal-tax", "36", 2026, new DateOnly(2026, 6, 15), "7f4492e1-5ef2-42a9-a179-04fc3bcc5d55", "car_benefit");

    private static readonly Source Article18ArIncome2026 = SharedSources.Article18ArIncome2026 with { Passage = "reference_emission" };

    private static readonly Source RoyalDecree20251217 = new(
        "reference_emission",
        new Uri("https://www.ejustice.just.fgov.be/eli/arrete/2025/12/17/2025009788/justel"),
        "personal-tax/ejustice_arrete-royal-emission-reference-co2-2026_2025-12-24.html");

    private static readonly Source Circular2026C10 = new(
        "fake_hybrid",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/e401b7f3-0081-4aba-9166-a07580353318"),
        "personal-tax/spf-finances_circulaire-2026-c-10-faux-hybride_2026-01-07.html");

    private static readonly Source CarBenefitFaq = new(
        "age_and_periods",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/8c873a40-df98-4d8d-832a-6baff8743303"),
        "personal-tax/spf-finances_faq-avantage-vehicule-mis-a-disposition_2022-03-15.html");

    private static readonly Source Article31Income2026 = SharedSources.Article31Income2026;

    private static readonly Source RoyalDecree38 = SharedSources.RoyalDecree38;

    private static readonly Source Article66Income2026 = Source.Fisconet("corporate-tax", "66", 2026, new DateOnly(2026, 6, 15), "f233da76-42c8-4f81-a098-33aef1b6e8d8", "car_costs_deduction");

    private static readonly Source Circular2024C50 = new(
        "zero_emission_deduction",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/45e88e8f-fbd0-402d-8c2d-92356e8f4075"),
        "corporate-tax/spf-finances_circulaire-2024-c-50-verdissement-mobilite_2024-07-23.html");

    private static readonly Source Article550Income2026 = Source.Fisconet("corporate-tax", "550", 2026, new DateOnly(2026, 3, 2), "8a670bab-e5e4-4b38-81a9-89ac35f0c65f", "deduction_formula");

    private static readonly Source Article198bisIncome2026 = Source.Fisconet("corporate-tax", "198bis", 2026, new DateOnly(2026, 3, 2), "8e743626-5dcc-4073-8a53-5323ae46ab9b", "corporate_tax_exceptions");

    private static readonly Source Circular2026C11 = new(
        "plug_in_hybrids",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/d96a332f-059e-4b97-957c-66c5bd50624f"),
        "corporate-tax/spf-finances_circulaire-2026-c-11-hybrides-rechargeables_2026-01-09.html");

    private static readonly Source Question819 = new(
        "deduction_2023_to_2025",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/a8939a34-ce6e-4308-b28a-859ca41e82c9"),
        "corporate-tax/spf-finances_question-parlementaire-819_2026-08-05.html");

    private static readonly Source Question96 = new(
        "diesel_hybrid_coefficient",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/584a3a58-ea1a-42e7-8dd0-577da42f2204"),
        "corporate-tax/spf-finances_question-parlementaire-96_2022-02-10.html");

    private static readonly Source Article198Income2026 = Source.Fisconet("corporate-tax", "198", 2026, new DateOnly(2026, 2, 10), "5cf99cca-101c-4b02-924e-35e149217648", "car_costs_added_back");

    private static readonly Source Module53 = new(
        "electric_car_charging",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/8cfb00cb-901d-4252-82f1-b1b00c7e026d"),
        "corporate-tax/spf-finances_cours-isoc-module-5-3-depenses-non-admises_2023-03-17.pdf");

    // From 200 g/km, or without CO2 data, a car acquired before 01.07.2023 is deducted at 40%.
    private static readonly RuleAmount HighEmissionDeduction2026 = new("high_emission_deduction_rate", 0.40m, Article550Income2026);

    public static CarRules IncomeYear2026 { get; } = new(
        IncomeYear: 2026,
        Benefit: Article36Income2026,
        BenefitMethod: CarBenefitFaq,
        BenefitAsPay: Article31Income2026,
        BenefitInContributions: RoyalDecree38,
        ReferenceDecree: RoyalDecree20251217,
        FakeHybrid: Circular2026C10,
        Deduction: Article66Income2026,
        Acquisition: Circular2024C50,
        FormulaDeduction: Article550Income2026,
        CompanyExclusions: Article198bisIncome2026,
        PlugInDeduction: Circular2026C11,
        AddBack: Article198Income2026,
        CatalogueShare: new RuleAmount("catalogue_share_rate", 6m / 7m, Article36Income2026),
        BaseCo2Percentage: new RuleAmount("base_co2_rate", 0.055m, Article36Income2026),
        Co2PercentagePerGram: new RuleAmount("co2_per_gram_rate", 0.001m, Article36Income2026),
        MinimumCo2Percentage: new RuleAmount("minimum_co2_rate", 0.04m, Article36Income2026),
        MaximumCo2Percentage: new RuleAmount("maximum_co2_rate", 0.18m, Article36Income2026),
        PetrolReferenceEmission: new RuleAmount("petrol_reference_grams", 70m, Article18ArIncome2026),
        DieselReferenceEmission: new RuleAmount("diesel_reference_grams", 58m, Article18ArIncome2026),
        PetrolEmissionWithoutData: new RuleAmount("petrol_without_data_grams", 205m, Article36Income2026),
        DieselEmissionWithoutData: new RuleAmount("diesel_without_data_grams", 195m, Article36Income2026),
        FakeHybridWithoutMatch: new RuleAmount("fake_hybrid_without_match_multiplier", 2.5m, Article36Income2026),
        AgeBands:
        [
            new CarAgeBand(1, new RuleAmount("age_up_to_12_months_rate", 1.00m, Article36Income2026)),
            new CarAgeBand(13, new RuleAmount("age_13_to_24_months_rate", 0.94m, Article36Income2026)),
            new CarAgeBand(25, new RuleAmount("age_25_to_36_months_rate", 0.88m, Article36Income2026)),
            new CarAgeBand(37, new RuleAmount("age_37_to_48_months_rate", 0.82m, Article36Income2026)),
            new CarAgeBand(49, new RuleAmount("age_49_to_60_months_rate", 0.76m, Article36Income2026)),
            new CarAgeBand(61, new RuleAmount("age_from_61_months_rate", 0.70m, Article36Income2026)),
        ],
        MinimumBenefit: new RuleAmount("minimum_car_benefit", 1_690m, Article36Income2026),
        AddBackRate: new RuleAmount("car_add_back_rate", 0.17m, Article198Income2026),
        AddBackRateWithPrivateEnergy: new RuleAmount("car_add_back_with_private_energy_rate", 0.40m, Module53),
        ZeroEmissionDeduction: new RuleAmount("zero_emission_deduction_rate", 1.00m, Article66Income2026),
        FormulaBase: new RuleAmount("deduction_formula_base_rate", 1.20m, Article550Income2026),
        FormulaPerGram: new RuleAmount("deduction_formula_per_gram_rate", 0.005m, Article550Income2026),
        DieselCoefficient: new RuleAmount("diesel_coefficient", 1m, Article550Income2026),
        OtherFuelCoefficient: new RuleAmount("other_fuel_coefficient", 0.95m, Article550Income2026),
        DieselHybridCoefficient: new RuleAmount("diesel_hybrid_coefficient", 0.95m, Question96),
        SmallNaturalGasCoefficient: new RuleAmount("small_natural_gas_coefficient", 0.90m, Article550Income2026),
        DeductionPeriods:
        [
            new CarDeductionPeriod(DateOnly.MinValue)
            {
                Floor = new RuleAmount("deduction_floor_rate", 0.50m, Article550Income2026),
                Ceiling = new RuleAmount("deduction_ceiling_rate", 1.00m, Article550Income2026),
                WithoutCo2Data = HighEmissionDeduction2026,
                HighEmissionFrom = new RuleAmount("high_emission_grams", 200m, Article550Income2026),
            },
            new CarDeductionPeriod(new DateOnly(2023, 7, 1))
            {
                Ceiling = new RuleAmount("transition_deduction_ceiling_rate", 0.50m, Article550Income2026),
                WithoutCo2Data = new RuleAmount("transition_without_data_deduction_rate", 0m, Article550Income2026),
                PlugInReading = Question819,
            },
            new CarDeductionPeriod(new DateOnly(2026, 1, 1)) { Fixed = new RuleAmount("emitting_from_2026_deduction_rate", 0m, Article66Income2026) },
        ],
        FakeHybridFrom: new DateOnly(2018, 1, 1),
        PlugInFuelCappedFrom: new DateOnly(2023, 1, 1),
        PlugInFuelCappedUntil: new DateOnly(2023, 6, 30),
        Conditions:
        [
            new LeverCondition("ordered_or_leased", [Article66Income2026, Article550Income2026, Circular2024C50]),
            new LeverCondition("private_use_while_held", [Article36Income2026, CarBenefitFaq]),
            new LeverCondition("fake_hybrid", [Article36Income2026, Article550Income2026, Circular2026C10]),
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

// How a company deducts the costs of a car ordered or leased from AcquiredFrom on, up to the next
// period. A car that emits no CO2 at ZeroEmission, when the period sets a rate of its own, or else at
// the year's CarRules.ZeroEmissionDeduction. A car that emits CO2 at a Fixed rate, or by the deduction
// formula between its Floor (none: nothing) and its Ceiling; WithoutCo2Data when the vehicle
// registration holds no CO2 for the car, which a car emitting HighEmissionFrom g/km or more takes too.
// PlugInReading is the text that says how a plug-in hybrid of the period is deducted, when one does.
public sealed record CarDeductionPeriod(DateOnly AcquiredFrom)
{
    public RuleAmount? ZeroEmission { get; init; }

    public Source? PlugInReading { get; init; }

    public RuleAmount? Fixed { get; init; }

    public RuleAmount? Floor { get; init; }

    public RuleAmount? Ceiling { get; init; }

    public RuleAmount? WithoutCo2Data { get; init; }

    public RuleAmount? HighEmissionFrom { get; init; }
}
