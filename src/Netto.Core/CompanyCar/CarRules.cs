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
// charging (the SPF course's Module 5.3). An electric car ordered or leased before 2027 stays
// deductible in full for its whole life (circular 2024/C/50); a car that emits CO2 is deducted by its
// DeductionPeriods. The benefit taxed is pay (BenefitAsPay), so it is in the income INASTI's
// contributions are owed on (BenefitInContributions).
public sealed record CarRules(
    int IncomeYear,
    Source Benefit,
    Source BenefitMethod,
    Source BenefitAsPay,
    Source BenefitInContributions,
    Source ReferenceEmission,
    Source ReferenceDecree,
    Source FakeHybrid,
    Source Deduction,
    Source Acquisition,
    Source FormulaDeduction,
    Source CompanyExclusions,
    Source PlugInDeduction,
    Source PlugInTransition,
    Source DieselHybrid,
    Source AddBack,
    Source ChargingReading,
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
    private static readonly Source Article36Income2026 = new(
        "CIR 92, art. 36 (revenus 2026), coordinated text on Fisconetplus, last modified 15.06.2026",
        "§2: the benefit of a car put at a person's disposal for private use is a CO2 percentage of 6/7 of its catalogue value, as sold new to a private buyer, options and VAT paid included, discounts excluded; "
        + "the catalogue value is taken at 100% up to 12 months from the first registration, then 94, 88, 82 and 76% by twelve months, 70% from the 61st, a month begun counting whole; "
        + "the percentage is 5.5% at the year's reference emission, 0.1% more or less for each gram above or below it, at least 4%, which a car that emits no CO2 takes, and at most 18%; "
        + "a car whose CO2 the vehicle registration does not hold counts as 205 g/km on petrol, LPG or natural gas and 195 g/km on diesel; "
        + "a plug-in hybrid bought or leased from 2018 whose battery holds less than 0.5 kWh per 100 kg, or that emits more than 50 g/km (75 g/km under Euro 6e-bis), takes the CO2 of the corresponding car running on its fuel alone, or its own × 2.5 when there is none; "
        + "the benefit is at least 1,690 a year (indexed amount), less the beneficiary's own contribution",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/7f4492e1-5ef2-42a9-a179-04fc3bcc5d55"),
        "personal-tax/spf-finances_cir92-art-36-revenus-2026_2026-06-15.html") { Label = "CIR 92, art. 36" };

    private static readonly Source Article18ArIncome2026 = new(
        "AR/CIR 92, art. 18 (revenus 2026), coordinated text on Fisconetplus, last modified 10.08.2026",
        "§3, 9: the reference CO2 emission for 2026 is 70 g/km for a car running on petrol, LPG or natural gas and 58 g/km for one running on diesel, for the benefits granted from 01.01.2026",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/f3ebfcea-9d61-4d4a-bd76-33f13ef93825"),
        "personal-tax/spf-finances_arcir92-art-18-revenus-2026_2026-08-10.html") { Label = "AR/CIR 92, art. 18" };

    private static readonly Source RoyalDecree20251217 = new(
        "Arrêté royal du 17.12.2025 modifiant, en ce qui concerne les avantages de toute nature, l'AR/CIR 92 (MB 24.12.2025), consolidated on Justel",
        "Art. 1: the reference CO2 emissions for 2026, 70 and 58 g/km; in force on 01.01.2026",
        new Uri("https://www.ejustice.just.fgov.be/eli/arrete/2025/12/17/2025009788/justel"),
        "personal-tax/ejustice_arrete-royal-emission-reference-co2-2026_2025-12-24.html") { Label = "Royal decree of 17.12.2025" };

    private static readonly Source Circular2026C10 = new(
        "Circulaire 2026/C/10 relative à l'avantage de toute nature résultant de l'utilisation à des fins personnelles d'un « faux hybride » conforme à la norme Euro 6e-bis, Fisconetplus, 07.01.2026",
        "N° 4, 11 to 13: a fake hybrid is a plug-in hybrid bought or leased from 2018 whose battery holds less than 0.5 kWh per 100 kg of its weight, or that emits more than 50 g/km, 75 g/km when measured under Euro 6e-bis or a later standard; "
        + "n° 5 and 14: its benefit takes the CO2 of the corresponding car running on its fuel alone, on the SPF's list, or its own × 2.5 when there is none",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/e401b7f3-0081-4aba-9166-a07580353318"),
        "personal-tax/spf-finances_circulaire-2026-c-10-faux-hybride_2026-01-07.html") { Label = "Circular 2026/C/10" };

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

    private static readonly Source Article550Income2026 = new(
        "CIR 92, art. 550 (revenus 2026), coordinated text on Fisconetplus, last modified 02.03.2026",
        "Al. 1: a car bought, leased or rented before 01.01.2026 is deductible at 120% − 0.5% × coefficient × g/km, the coefficient 1 for a car running on diesel alone, 0.95 for the others, 0.90 for natural gas under 12 fiscal horsepower, rounded to a tenth; "
        + "al. 2: acquired before 01.07.2023, between 50% and 100%, 40% from 200 g/km or without CO2 data; al. 4: acquired from 01.07.2023 to 31.12.2025, at most 50%, 0% without CO2 data; "
        + "al. 6: a fake hybrid takes the CO2 of its corresponding car, or its own × 2.5; al. 7: a plug-in hybrid acquired from 01.01.2023 to 30.06.2023 deducts its petrol or diesel at 50% at most",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/8a670bab-e5e4-4b38-81a9-89ac35f0c65f"),
        "corporate-tax/spf-finances_cir92-art-550-revenus-2026_2026-03-02.html") { Label = "CIR 92, art. 550" };

    private static readonly Source Article198bisIncome2026 = new(
        "CIR 92, art. 198bis (revenus 2026), coordinated text on Fisconetplus, last modified 02.03.2026",
        "Art. 66 §1/1 and art. 550 al. 3 and 5 do not apply in corporate tax: no relief for plug-in hybrids and no minimum for a car acquired before 2018",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/8e743626-5dcc-4073-8a53-5323ae46ab9b"),
        "corporate-tax/spf-finances_cir92-art-198bis-revenus-2026_2026-03-02.html") { Label = "CIR 92, art. 198bis" };

    private static readonly Source Circular2026C11 = new(
        "Circulaire 2026/C/11 relative à la déduction des frais professionnels des véhicules hybrides rechargeables, Fisconetplus, 07.01.2026",
        "N° 6: the relief for plug-in hybrids is personal tax only, the fake-hybrid threshold of 75 g/km under Euro 6e-bis applying to companies too; "
        + "n° 14: a plug-in hybrid acquired from 01.01.2023 to 30.06.2023 deducts its petrol or diesel at 50% at most; n° 16: no vehicle acquired from 01.01.2026 deducts its fuel",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/d96a332f-059e-4b97-957c-66c5bd50624f"),
        "corporate-tax/spf-finances_circulaire-2026-c-11-hybrides-rechargeables_2026-01-09.html") { Label = "Circular 2026/C/11" };

    private static readonly Source Question819 = new(
        "Question parlementaire n° 819 de monsieur Wouter Vermeersch du 12.01.2026, QRVA 56/42 du 19.02.2026, Fisconetplus",
        "The Minister: in corporate tax, a vehicle acquired from 01.07.2023 to 31.12.2025 is deducted for tax year 2027 at the CO2 formula, at most 50%, for its electricity, its petrol and its other costs alike (art. 550 al. 1 and 4)",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/a8939a34-ce6e-4308-b28a-859ca41e82c9"),
        "corporate-tax/spf-finances_question-parlementaire-819_2026-08-05.html") { Label = "Question 819" };

    private static readonly Source Question96 = new(
        "Question parlementaire n° 96 de madame Kathleen Depoorter du 27.11.2019, QRVA 55/008 du 07.01.2020, Fisconetplus",
        "The Minister: a hybrid with a diesel engine and an electric motor may take the coefficient 0.95 of the deduction formula, that of a vehicle with another engine",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/584a3a58-ea1a-42e7-8dd0-577da42f2204"),
        "corporate-tax/spf-finances_question-parlementaire-96_2022-02-10.html") { Label = "Question 96" };

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

    // From 200 g/km, or without CO2 data, a car acquired before 01.07.2023 is deducted at 40%.
    private static readonly RuleAmount HighEmissionDeduction2026 = new("high_emission_deduction_rate", 0.40m, Article550Income2026);

    public static CarRules IncomeYear2026 { get; } = new(
        2026,
        Article36Income2026,
        CarBenefitFaq,
        Article31Income2026,
        RoyalDecree38,
        Article18ArIncome2026,
        RoyalDecree20251217,
        Circular2026C10,
        Article66Income2026,
        Circular2024C50,
        Article550Income2026,
        Article198bisIncome2026,
        Circular2026C11,
        Question819,
        Question96,
        Article198Income2026,
        Module53,
        new RuleAmount("catalogue_share_rate", 6m / 7m, Article36Income2026),
        new RuleAmount("base_co2_rate", 0.055m, Article36Income2026),
        new RuleAmount("co2_per_gram_rate", 0.001m, Article36Income2026),
        new RuleAmount("minimum_co2_rate", 0.04m, Article36Income2026),
        new RuleAmount("maximum_co2_rate", 0.18m, Article36Income2026),
        new RuleAmount("petrol_reference_grams", 70m, Article18ArIncome2026),
        new RuleAmount("diesel_reference_grams", 58m, Article18ArIncome2026),
        new RuleAmount("petrol_without_data_grams", 205m, Article36Income2026),
        new RuleAmount("diesel_without_data_grams", 195m, Article36Income2026),
        new RuleAmount("fake_hybrid_without_match_multiplier", 2.5m, Article36Income2026),
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
        new RuleAmount("deduction_formula_base_rate", 1.20m, Article550Income2026),
        new RuleAmount("deduction_formula_per_gram_rate", 0.005m, Article550Income2026),
        new RuleAmount("diesel_coefficient", 1m, Article550Income2026),
        new RuleAmount("other_fuel_coefficient", 0.95m, Article550Income2026),
        new RuleAmount("diesel_hybrid_coefficient", 0.95m, Question96),
        new RuleAmount("small_natural_gas_coefficient", 0.90m, Article550Income2026),
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
            },
            new CarDeductionPeriod(new DateOnly(2026, 1, 1)) { Fixed = new RuleAmount("emitting_from_2026_deduction_rate", 0m, Article66Income2026) },
        ],
        new DateOnly(2018, 1, 1),
        new DateOnly(2023, 1, 1),
        new DateOnly(2023, 6, 30),
        [
            new LeverCondition("ordered_or_leased", [Article66Income2026, Article550Income2026, Circular2024C50]),
            new LeverCondition("private_use_all_year", [Article36Income2026, CarBenefitFaq]),
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

// How a company deducts the costs of a car that emits CO2 ordered or leased from AcquiredFrom on, up
// to the next period: a Fixed rate, or the deduction formula between its Floor (none: nothing) and
// its Ceiling; WithoutCo2Data when the vehicle registration holds no CO2 for the car, which a car
// emitting HighEmissionFrom g/km or more takes too.
public sealed record CarDeductionPeriod(DateOnly AcquiredFrom)
{
    public RuleAmount? Fixed { get; init; }

    public RuleAmount? Floor { get; init; }

    public RuleAmount? Ceiling { get; init; }

    public RuleAmount? WithoutCo2Data { get; init; }

    public RuleAmount? HighEmissionFrom { get; init; }
}
