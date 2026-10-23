using Netto.Core.CompanyCar;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;

namespace Netto.Tests.WebClient.River.CompanyCar;

// The Company car card on the river's example (RiverInputsTests), 100,000 of profit before the
// director, with YearSimulationCarTests' electric car: 50,000 of catalogue value, first registered in
// January 2026, a benefit of 1,714.29, and 8,000 of yearly costs; the director pays 600 for its
// private use where a test says so.
public class CarViewTests
{
    private static readonly RiverInputs ElectricCar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    // Added with nothing: no car yet, so the profit goes on whole to the split, and there are no steps
    // to list.
    [Fact]
    public void Car_addedWithNothing_passesTheProfitOnToTheSplit()
    {
        var view = Car(RiverInputs.Example.AddToPlan(OptionalModule.Car));

        (view.HasCar, view.Details.Count, view.PassesOn, view.Drains.Single())
            .ShouldBe((false, 0, (decimal?)null, new Drain("profit_before_pay", DrainKind.PassedOn, 100_000m, "split")));
    }

    // Its costs leave the river, a cost of the company like its running costs, the director's
    // contribution comes back from the pocket, and the profit before pay, 100,000 less 8,000 plus 600,
    // goes on to the split.
    [Fact]
    public void Car_withAContribution_paysItsCostsOutAsACompanyCostAndPassesTheRestOn()
    {
        Lines(Car(ElectricCar.WithCarContribution(600m)).Drains).ShouldBe(new[]
        {
            new Drain("car_costs", DrainKind.PaidOut, 8_000m, "company_costs"),
            new Drain("car_contribution", DrainKind.Back, 600m, "pocket"),
            new Drain("profit_before_pay", DrainKind.PassedOn, 92_600m, "split"),
        });
    }

    // What the split shares once the car is paid, 92,000, stays the same at every pay with a royalty that
    // follows it, which the card's summary states; the profit before pay alone falls as the royalty
    // rises with the pay.
    [Theory]
    [InlineData(50_000)]
    [InlineData(60_000)]
    public void Car_withAFollowingRoyalty_saysWhatTheSplitSharesAtEveryPay(int pay)
    {
        Car(ElectricCar.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m).WithDirectorPay(pay)).SplitShares.ShouldBe(92_000m);
    }

    // With the royalty in the plan, what the car leaves goes on to the split, which shares it between the
    // pay, the royalty and the company: the royalty is still in it.
    [Fact]
    public void Car_withTheRoyaltyInThePlan_passesWhatThePayAndTheRoyaltyShareOnToTheSplit()
    {
        var view = Car(ElectricCar.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m));

        (view.Received, view.Drains[^1], view.PassesOn)
            .ShouldBe((100_000m, new Drain("profit_before_pay_and_royalty", DrainKind.PassedOn, 92_000m, "split"), (decimal?)0.92m));
    }

    // The costs and the contribution are steps of the ledger: their lines carry them.
    [Fact]
    public void Car_eachLineOfAStepCarriesThatStep()
    {
        Car(ElectricCar.WithCarContribution(600m)).Drains.Select(drain => drain.Details.SingleOrDefault()?.RuleKey ?? "")
            .ShouldBe(["car_costs", "car_contribution", ""]);
    }

    // What it receives is what the levers above leave of what the company earns; the costs line says
    // its share of it, and the card what it passes on.
    [Fact]
    public void Car_saysItsSharesOfWhatItReceives()
    {
        var view = Car(ElectricCar);

        (view.Received, view.Drains[0].Share, view.PassesOn).ShouldBe((100_000m, (decimal?)0.08m, (decimal?)0.92m));
    }

    [Fact]
    public void Car_listsItsStepsAndNoneOfTheOtherLevers()
    {
        Car(ElectricCar).Details.Select(detail => detail.RuleKey).ShouldBe(["car_costs", "car_benefit", "car_contribution", "car_benefit_taxed", "car_add_back"]);
    }

    // The benefit as the engine took it: its periods, the minimum and the contribution.
    [Fact]
    public void Car_saysTheBenefitTaxedAsPay()
    {
        var view = Car(ElectricCar.WithCarContribution(600m));

        (view.Result.BenefitOnTheScale, view.Result.BenefitBeforeContribution, view.Result.Benefit, view.Result.Periods.Count)
            .ShouldBe((1_714.29m, 1_714.29m, 1_114.29m, 1));
    }

    // 17% added back, or 40% when the company pays the private charging, which is the administration's
    // reading of « fuel » for an electric car.
    [Fact]
    public void Car_companyPaysPrivateCharging_saysTheFortyPercentIsTheAdministrationsReading()
    {
        var (seventeen, forty) = (Car(ElectricCar), Car(ElectricCar.WithPrivateEnergyPaid(true)));

        (seventeen.Result.AddBack, seventeen.ChargingReading, forty.Result.AddBack, forty.ChargingReading).ShouldBe((291.43m, false, 685.72m, true));
    }

    // The plan with the car against the same plan without it, at the same pay: two runs of the year.
    [Fact]
    public void Comparison_car_isTheDifferenceOfTheYearWithItAndWithout()
    {
        var with = ElectricCar.Run().Year.ShouldNotBeNull();
        var without = ElectricCar.TakeOutOfPlan(OptionalModule.Car).Run().Year.ShouldNotBeNull();

        var comparison = CarComparison.From(ElectricCar, ElectricCar.Run()).ShouldNotBeNull();

        comparison.ShouldSatisfyAllConditions(
            () => (comparison.PocketWith, comparison.PocketWithout).ShouldBe((with.NetInPocket, without.NetInPocket)),
            () => comparison.CashCost.ShouldBe(without.NetInPocket - with.NetInPocket),
            () => comparison.TaxesOnPayAdded.ShouldBe(TaxesOnPay(with) - TaxesOnPay(without)));
    }

    // The benefit raises the taxes on pay, and the car takes cash from the pocket: 6,162.66 of its
    // 8,000 of costs, the cash the card states.
    [Fact]
    public void Comparison_car_takesCashFromThePocket()
    {
        var comparison = CarComparison.From(ElectricCar, ElectricCar.Run()).ShouldNotBeNull();

        (comparison.TaxesOnPayAdded > 0m, comparison.CashCost, CarCardText.Cash(comparison)).ShouldBe((true, 6_162.66m, "Figure_Cash_Takes"));
    }

    // Until the user gives what the same car would cost privately, the comparison with a private car
    // waits for it: there is nothing to weigh the cash against.
    [Fact]
    public void Comparison_noPrivateCost_waitsForIt()
    {
        var comparison = CarComparison.From(ElectricCar, ElectricCar.Run()).ShouldNotBeNull();

        (comparison.PrivateCost, comparison.AgainstPrivate, comparison.Refused, CarCardText.Private(comparison))
            .ShouldBe(((decimal?)null, (decimal?)null, (string?)null, "Private_Waiting"));
    }

    // Against the same car bought privately, the company car gains what that car would cost less the
    // 6,162.66 of cash it takes from the pocket: a gain, a loss, or the same to the cent.
    public static TheoryData<decimal, decimal, string> PrivateCosts => new()
    {
        { 10_000m, 3_837.34m, "Private_Gain" },
        { 1_000m, -5_162.66m, "Private_Loss" },
        { 6_162.66m, 0m, "Private_Tie" },
    };

    [Theory]
    [MemberData(nameof(PrivateCosts))]
    public void Comparison_privateCost_isWhatItWouldCostLessTheCashTheCarTakes(decimal privateCost, decimal gain, string key)
    {
        var inputs = ElectricCar.WithCarPrivateCost(privateCost);

        var comparison = CarComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        (comparison.PrivateCost, comparison.AgainstPrivate, CarCardText.Private(comparison)).ShouldBe(((decimal?)privateCost, (decimal?)gain, key));
    }

    // A tie is a plain zero, never a negative one a sentence would read as a loss.
    [Fact]
    public void Comparison_privateCostEqualToTheCash_isAPlainZero()
    {
        var inputs = ElectricCar.WithCarPrivateCost(6_162.66m);

        var against = CarComparison.From(inputs, inputs.Run()).ShouldNotBeNull().AgainstPrivate.ShouldNotBeNull();

        decimal.IsNegative(against).ShouldBeFalse();
    }

    // What the same car would cost privately cannot be negative: the card refuses it, and the comparison
    // waits for a cost it can weigh.
    [Fact]
    public void Comparison_negativePrivateCost_isRefused()
    {
        var inputs = ElectricCar.WithCarPrivateCost(-0.01m);

        var comparison = CarComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        (comparison.Refused, comparison.AgainstPrivate, CarCardText.Private(comparison))
            .ShouldBe((CarComparison.PrivateCostNegative, (decimal?)null, "Private_Waiting"));
    }

    [Fact]
    public void Comparison_noCarCounted_isNone()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Car);

        CarComparison.From(inputs, inputs.Run()).ShouldBeNull();
    }

    [Fact]
    public void Comparison_refusedRiver_isNone()
    {
        var inputs = ElectricCar.WithReserve(-1m);

        CarComparison.From(inputs, inputs.Run()).ShouldBeNull();
    }

    // The rent passes what it leaves on to the car once the car is in the plan: the profit before pay
    // with the car's costs still in it.
    [Fact]
    public void Rent_withACar_passesWhatItLeavesOnToTheCar()
    {
        var inputs = ElectricCar.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

        RentView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition).Drains[^1]
            .ShouldBe(new Drain("left_after_rent", DrainKind.PassedOn, 98_620m, "car"));
    }

    [Fact]
    public void Allowances_withACar_passWhatTheyLeaveOnToTheCar()
    {
        var inputs = ElectricCar.AddToPlan(OptionalModule.Allowances).WithInternetAllowance(240m);

        AllowanceView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition).Drains[^1]
            .ShouldBe(new Drain("left_after_allowances", DrainKind.PassedOn, 99_760m, "car"));
    }

    // Director pay says the benefit it is taxed with; the split, that the benefit counts in the pay
    // the reduced rate tests.
    [Fact]
    public void DirectorPayAndSplit_car_sayTheBenefitInThePay()
    {
        var run = ElectricCar.Run();

        (DirectorPayView.From(ElectricCar, run.Pay.ShouldNotBeNull()).BenefitInKind, SplitSearchView.From(ElectricCar, run, ElectricCar.SearchSplit())?.Benefits)
            .ShouldBe((1_714.29m, (decimal?)1_714.29m));
    }

    [Fact]
    public void DirectorPayAndSplit_noCar_sayNoBenefit()
    {
        var run = RiverInputs.Example.Run();

        (DirectorPayView.From(RiverInputs.Example, run.Pay.ShouldNotBeNull()).BenefitInKind, SplitSearchView.From(RiverInputs.Example, run, RiverInputs.Example.SearchSplit())?.Benefits)
            .ShouldBe((0m, (decimal?)0m));
    }

    // Corporate tax says what it adds back and why: the vouchers it cannot deduct, the car's share of
    // its benefit, or both.
    [Fact]
    public void CorporateTax_carAndVouchers_sayEachAddBackAndWhichSentenceSaysThem()
    {
        var both = CorporateTaxView.From(ElectricCar.AddToPlan(OptionalModule.Vouchers).WithEcoVouchers(250m).Run().Year.ShouldNotBeNull());
        var car = CorporateTaxView.From(ElectricCar.Run().Year.ShouldNotBeNull());

        (both.DisallowedVouchers, both.CarAddBack, CorporateTaxCardText.Disallowed(both), CorporateTaxCardText.Disallowed(car))
            .ShouldBe((250m, 291.43m, "Disallowed_VouchersAndCar", "Disallowed_Car"));
    }

    [Fact]
    public void CorporateTax_vouchersAlone_keepTheirSentence()
    {
        var view = CorporateTaxView.From(RiverInputs.Example.AddToPlan(OptionalModule.Vouchers).WithEcoVouchers(250m).Run().Year.ShouldNotBeNull());

        (view.CarAddBack, CorporateTaxCardText.Disallowed(view)).ShouldBe((0m, "Disallowed"));
    }

    // A fuel car on the same plan: CarCalculatorTests' petrol car leased in January 2026, 45,000 at
    // 120 g/km, a benefit of 4,050.00 and 9,000 of costs, of which the company cannot deduct 4,950.00.
    private static readonly RiverInputs PetrolCar = ElectricCar.WithCatalogueValue(45_000m).WithCarCosts(9_000m)
        .WithCarEngine(CarEngine.Petrol).WithCarCo2(120).WithCarAcquired(new DateOnly(2026, 1, 5));

    // Corporate tax says what it adds back of a car it cannot deduct in full: the costs beyond the
    // benefit, and the share of the benefit, alone or beside the vouchers.
    [Fact]
    public void CorporateTax_fuelCar_saysTheCostsItCannotDeductBesideItsAddBack()
    {
        var car = CorporateTaxView.From(PetrolCar.Run().Year.ShouldNotBeNull());
        var both = CorporateTaxView.From(PetrolCar.AddToPlan(OptionalModule.Vouchers).WithEcoVouchers(250m).Run().Year.ShouldNotBeNull());

        (car.CarCostsNotDeductible, car.CarAddBack, CorporateTaxCardText.Disallowed(car), CorporateTaxCardText.Disallowed(both))
            .ShouldBe((4_950m, 688.50m, "Disallowed_CarCosts", "Disallowed_VouchersAndCarCosts"));
    }

    [Fact]
    public void CorporateTax_electricCar_hasNoCostsItCannotDeduct()
    {
        var view = CorporateTaxView.From(ElectricCar.Run().Year.ShouldNotBeNull());

        (view.CarCostsNotDeductible, decimal.IsNegative(view.CarCostsNotDeductible)).ShouldBe((0m, false));
    }

    // The card says what its CO2 percentage counts: nothing for an electric car, the CO2 registered,
    // the texts' emission without data, a fake hybrid's matching car or its own × 2.5.
    [Fact]
    public void Car_co2_saysWhatThePercentageCounts()
    {
        var plugIn = PetrolCar.WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(45).WithCarAcquired(new DateOnly(2025, 12, 1));

        new[]
            {
                ElectricCar, PetrolCar, PetrolCar.WithNoCo2Data(true), plugIn.WithFakeHybrid(true).WithMatchingCarCo2(150),
                plugIn.WithFakeHybrid(true).WithNoMatchingCar(true),
            }
            .Select(inputs => CarCardText.Co2(Car(inputs).Result))
            .ShouldBe(["Co2_Electric", "Co2_Registered", "Co2_WithoutData", "Co2_MatchingCar", "Co2_OwnTimesMultiplier"]);
    }

    // And when the percentage is held to the lowest or the highest, which an electric car's always is.
    [Fact]
    public void Car_co2HeldToABound_saysSo()
    {
        var rules = CarRules.IncomeYear2026;

        new[] { ElectricCar, PetrolCar, PetrolCar.WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(20), PetrolCar.WithCarEngine(CarEngine.Diesel).WithCarCo2(250) }
            .Select(inputs => CarCardText.Co2Held(Car(inputs).Result, rules))
            .ShouldBe([null, null, "Co2_HeldToMinimum", "Co2_HeldToMaximum"]);
    }

    // How the company deducts the car's costs: in full; the part equal to the benefit and the
    // contribution in full and the rest at its rate; that part alone; or all of them, being no more
    // than that part.
    [Fact]
    public void Car_deduction_saysHowTheCompanyDeductsItsCosts()
    {
        var orderedIn2025 = PetrolCar.WithCatalogueValue(30_000m).WithCarCosts(10_000m).WithCarCo2(100).WithCarAcquired(new DateOnly(2025, 11, 1));
        var cheapDiesel = PetrolCar.WithCatalogueValue(60_000m).WithFirstRegistration(new DateOnly(2019, 3, 5)).WithCarCosts(3_500m)
            .WithCarEngine(CarEngine.Diesel).WithCarCo2(140).WithCarAcquired(new DateOnly(2019, 2, 1));

        new[] { ElectricCar, orderedIn2025, PetrolCar, cheapDiesel }
            .Select(inputs => CarCardText.Deduction(Car(inputs).Result))
            .ShouldBe(["Figure_Deduction_Full", "Figure_Deduction_Rate", "Figure_Deduction_None", "Figure_Deduction_AllCarvedOut"]);
    }

    [Fact]
    public void Car_fuelCar_listsWhatItCannotDeduct()
    {
        Car(PetrolCar).Details.Select(detail => detail.RuleKey)
            .ShouldBe(["car_costs", "car_benefit", "car_contribution", "car_benefit_taxed", "car_costs_not_deductible", "car_add_back"]);
    }

    // The private energy the company pays is the charging of an electric car, the fuel or the charging
    // of a plug-in hybrid (Module 5.3), and the fuel of any other.
    [Theory]
    [InlineData(CarEngine.Electric, "Charging")]
    [InlineData(CarEngine.PetrolPlugInHybrid, "FuelOrCharging")]
    [InlineData(CarEngine.DieselPlugInHybrid, "FuelOrCharging")]
    [InlineData(CarEngine.Petrol, "Fuel")]
    [InlineData(CarEngine.DieselHybrid, "Fuel")]
    [InlineData(CarEngine.NaturalGas, "Fuel")]
    public void PrivateEnergy_isWordedByWhatDrivesTheCar(CarEngine engine, string energy)
    {
        (CarCardText.PrivateEnergy(engine), CarCardText.PrivateEnergyHint(engine)).ShouldBe(($"PrivateEnergy_{energy}", $"PrivateEnergyHint_{energy}"));
    }

    // The 40% rests on the administration's reading for a car it charges: an electric car's charging,
    // a plug-in hybrid's too; for a fuel car the law's own word.
    [Fact]
    public void Car_fortyPercentOnACarItCharges_saysItIsTheAdministrationsReading()
    {
        var plugIn = PetrolCar.WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(45).WithCarAcquired(new DateOnly(2025, 12, 1)).WithPrivateEnergyPaid(true);

        (Car(plugIn).ChargingReading, Car(PetrolCar.WithPrivateEnergyPaid(true)).ChargingReading, CarCardText.Reading(CarEngine.Electric),
                CarCardText.Reading(CarEngine.PetrolPlugInHybrid))
            .ShouldBe((true, false, "AddBackReading", "AddBackReading_PlugIn"));
    }

    // The fake-hybrid condition only for a plug-in hybrid.
    [Theory]
    [InlineData(CarEngine.Electric, false)]
    [InlineData(CarEngine.Petrol, false)]
    [InlineData(CarEngine.PetrolPlugInHybrid, true)]
    [InlineData(CarEngine.DieselPlugInHybrid, true)]
    public void Conditions_fakeHybrid_isListedForAPlugInHybridOnly(CarEngine engine, bool listed)
    {
        var keys = CarView.Conditions(CarRules.IncomeYear2026, engine).Select(condition => condition.Key).ToList();

        (keys.Contains("fake_hybrid"), keys.Contains("ordered_or_leased"), keys.Contains("private_use_all_year")).ShouldBe((listed, true, true));
    }

    [Theory]
    [InlineData(CarCalculator.CatalogueValueNegative)]
    [InlineData(CarCalculator.YearlyCostsNegative)]
    [InlineData(CarCalculator.ContributionNegative)]
    [InlineData(CarCalculator.Co2Negative)]
    [InlineData(CarCalculator.Co2Missing)]
    [InlineData(CarCalculator.MatchingCarCo2Negative)]
    [InlineData(CarCalculator.MatchingCarCo2Missing)]
    [InlineData(CarCalculator.AcquiredAfterYear)]
    [InlineData(CarCalculator.PlugInFuelCapped)]
    public void RiverErrors_carRefusal_isShownOnTheCarCard(string error)
    {
        RiverErrors.Owner(error, companySkipped: false).ShouldBe(RiverModule.Car);
    }

    private static CarView Car(RiverInputs inputs) => CarView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition);

    private static decimal TaxesOnPay(YearResult year) =>
        year.Steps.Where(step => step.Event == TaxableEvent.DirectorPay && step.Track == Track.PaidOut).Sum(step => step.Amount);

    // The lines as amounts and destinations, their details and shares aside.
    private static IEnumerable<Drain> Lines(IEnumerable<Drain> drains) => drains.Select(drain => drain with { Details = [], Share = null });
}
