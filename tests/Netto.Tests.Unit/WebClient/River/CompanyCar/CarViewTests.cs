using Netto.Core.CompanyCar;
using Netto.Core.Rules;
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

    // Added with nothing: no car yet, so no line, and no steps to list.
    [Fact]
    public void Car_addedWithNothing_drawsNoLine()
    {
        var view = Car(RiverInputs.Example.AddToPlan(OptionalModule.Car));

        (view.HasCar, view.Details.Count, view.Drains.Count).ShouldBe((false, 0, 0));
    }

    // Its costs end in the company's expenses, beside its running costs, and the director's contribution
    // comes back from the pocket. What the company has left after the car, on to the split, is the map's
    // company line, no line of the card.
    [Fact]
    public void Car_withAContribution_paysItsCostsOutAsACompanyCostAndTakesTheContributionBack()
    {
        Lines(Car(ElectricCar.WithCarContribution(600m)).Drains).ShouldBe(new[]
        {
            new Drain("car_costs", DrainKind.PaidOut, 8_000m, "expenses"),
            new Drain("car_contribution", DrainKind.Back, 600m, "pocket"),
        });
    }

    // The costs and the contribution are steps of the ledger: their lines carry them.
    [Fact]
    public void Car_eachLineOfAStepCarriesThatStep()
    {
        Car(ElectricCar.WithCarContribution(600m)).Drains.Select(drain => drain.Details.SingleOrDefault()?.RuleKey ?? "")
            .ShouldBe(["car_costs", "car_contribution"]);
    }

    // The card measures nothing against what the company has left before the car: its costs say no share of
    // it, whether they are less than it, more, or it is nothing.
    [Theory]
    [InlineData(8_000, 0)]
    [InlineData(150_000, 0)]
    [InlineData(8_000, 150_000)]
    public void Car_costs_sayNoShareOfWhatTheCompanyHasLeft(int costs, int runningCosts)
    {
        var view = Car(ElectricCar.WithCarCosts(costs).WithRunningCosts(runningCosts));

        view.Drains.Select(drain => drain.Share).ShouldAllBe(share => share == null);
    }

    // The hand-off stays what it is when the contribution exceeds the costs: the car takes less than
    // nothing off what the company earns, so the company's line after it is more than before it, by the 600
    // the director pays, and its lines still say the contribution coming back from the pocket.
    [Fact]
    public void Car_contributionAboveItsCosts_stillHandsOnWhatThePocketAdds()
    {
        var inputs = ElectricCar.WithCarCosts(0m).WithCarContribution(600m);
        var year = inputs.Run().Year.ShouldNotBeNull();

        year.ShouldSatisfyAllConditions(
            () => (HandOff.Cost(year, OptionalModule.Car), HandOff.After(year, OptionalModule.Car) - HandOff.Before(year, OptionalModule.Car)).ShouldBe((-600m, 600m)),
            () => Lines(Car(inputs).Drains).ShouldBe(new[]
            {
                new Drain("car_costs", DrainKind.PaidOut, 0m, "expenses"),
                new Drain("car_contribution", DrainKind.Back, 600m, "pocket"),
            }));
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

        (comparison.TaxesOnPayAdded > 0m, comparison.CashCost).ShouldBe((true, 6_162.66m));
    }

    // A pay set above what the company can pay once the car's costs are paid is held there in the plan with
    // the car, but the river without it would pay all of it (RiverInputs.PayUsed): the comparison sets the
    // plan without the car at the pay the plan with it runs at, so that what differs is the car's alone.
    // 100,000 before the car, 20,000 of costs, 90,000 typed: 80,000 with it, and 80,000 without it too.
    [Fact]
    public void Comparison_payAboveWhatTheCompanyCanPayWithTheCar_comparesTwoYearsAtThePayWithTheCarRuns()
    {
        var inputs = ElectricCar.WithCarCosts(20_000m).WithDirectorPay(90_000m);
        var with = inputs.Run().Year.ShouldNotBeNull();
        var withoutAtThePaySet = inputs.TakeOutOfPlan(OptionalModule.Car).Run().Year.ShouldNotBeNull();
        var without = inputs.TakeOutOfPlan(OptionalModule.Car).WithDirectorPay(with.Scenario.DirectorPay).Run().Year.ShouldNotBeNull();
        var atThatPay = ElectricCar.WithCarCosts(20_000m).WithDirectorPay(80_000m);

        var comparison = CarComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        comparison.ShouldSatisfyAllConditions(
            () => (with.Scenario.DirectorPay, withoutAtThePaySet.Scenario.DirectorPay, without.Scenario.DirectorPay).ShouldBe((80_000m, 90_000m, 80_000m)),
            () => (comparison.PocketWith, comparison.PocketWithout).ShouldBe((with.NetInPocket, without.NetInPocket)),
            () => comparison.PocketWithout.ShouldNotBe(withoutAtThePaySet.NetInPocket),
            () => comparison.PayHeld.ShouldBe(80_000m),
            () => comparison.CashCost.ShouldBe(CarComparison.From(atThatPay, atThatPay.Run()).ShouldNotBeNull().CashCost));
    }

    // The pay set within what the company can pay with the car, or exactly at it, is the pay of both plans,
    // as it was: no pay is held.
    public static TheoryData<string, RiverInputs> PayNotHeld => new()
    {
        { "the example's pay", ElectricCar.WithCarCosts(20_000m) },
        { "exactly the pay it allows", ElectricCar.WithCarCosts(20_000m).WithDirectorPay(80_000m) },
    };

    [Theory]
    [MemberData(nameof(PayNotHeld))]
    public void Comparison_payWithinWhatTheCompanyCanPayWithTheCar_holdsNoPay(string _, RiverInputs inputs)
    {
        var with = inputs.Run().Year.ShouldNotBeNull();
        var without = inputs.TakeOutOfPlan(OptionalModule.Car).Run().Year.ShouldNotBeNull();

        var comparison = CarComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        (comparison.PayHeld, comparison.PocketWith, comparison.PocketWithout).ShouldBe(((decimal?)null, with.NetInPocket, without.NetInPocket));
    }

    // Costs above what the car receives leave nothing to pay: the pay is held at nothing, and both plans
    // run at it.
    [Fact]
    public void Comparison_costsAboveWhatTheCarReceives_holdsThePayAtNothing()
    {
        var inputs = ElectricCar.WithCarCosts(120_000m);

        var comparison = CarComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        comparison.PayHeld.ShouldBe(0m);
    }

    // Until the user gives what the same car would cost privately, the comparison with a private car
    // waits for it: there is nothing to weigh the cash against.
    [Fact]
    public void Comparison_noPrivateCost_waitsForIt()
    {
        var comparison = CarComparison.From(ElectricCar, ElectricCar.Run()).ShouldNotBeNull();

        (comparison.PrivateCost, comparison.AgainstPrivate, comparison.Refused, CarCardText.Private(comparison))
            .ShouldBe(((decimal?)null, (decimal?)null, (Refusal?)null, "Private_Waiting"));
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
            .ShouldBe((CarCalculator.PrivateCostNegative, (decimal?)null, "Private_Waiting"));
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

    // The rent passes what it leaves on to the car once the car is in the plan, on the map's company line
    // (HandOff): the profit before pay with the car's costs still in it.
    [Fact]
    public void Rent_withACar_passesWhatItLeavesOnToTheCar()
    {
        var inputs = ElectricCar.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

        HandOff.Drain(inputs.Run().Year.ShouldNotBeNull(), OptionalModule.Rent, inputs.Composition)
            .ShouldBe(new Drain("left_after_rent", DrainKind.PassedOn, 98_620m, "car"));
    }

    [Fact]
    public void Allowances_withACar_passWhatTheyLeaveOnToTheCar()
    {
        var inputs = ElectricCar.AddToPlan(OptionalModule.Allowances).WithInternetAllowance(240m);

        HandOff.Drain(inputs.Run().Year.ShouldNotBeNull(), OptionalModule.Allowances, inputs.Composition)
            .ShouldBe(new Drain("left_after_allowances", DrainKind.PassedOn, 99_760m, "car"));
    }

    // Director pay says the benefit it is taxed with; the split, that the benefit counts in the pay
    // the reduced rate tests.
    [Fact]
    public void DirectorPayAndSplit_car_sayTheBenefitInThePay()
    {
        var run = ElectricCar.Run();

        (run.Pay.ShouldNotBeNull().BenefitInKind, SplitSearchView.From(ElectricCar, run, ElectricCar.SearchSplit())?.Benefits)
            .ShouldBe((1_714.29m, (decimal?)1_714.29m));
    }

    [Fact]
    public void DirectorPayAndSplit_noCar_sayNoBenefit()
    {
        var run = RiverInputs.Example.Run();

        (run.Pay.ShouldNotBeNull().BenefitInKind, SplitSearchView.From(RiverInputs.Example, run, RiverInputs.Example.SearchSplit())?.Benefits)
            .ShouldBe((0m, (decimal?)0m));
    }

    // A fuel car on the same plan: CarCalculatorTests' petrol car leased in January 2026, 45,000 at
    // 120 g/km, a benefit of 4,050.00 and 9,000 of costs, of which the company cannot deduct 4,950.00.
    private static readonly RiverInputs PetrolCar = ElectricCar.WithCatalogueValue(45_000m).WithCarCosts(9_000m)
        .WithCarEngine(CarEngine.Petrol).WithCarCo2(120).WithCarAcquired(new DateOnly(2026, 1, 5));

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

    // How the company deducts costs it does not deduct in full, said in the bubble of what it cannot deduct:
    // nothing for costs in full or no more than the benefit; the part equal to the benefit and the
    // contribution in full and the rest at its rate; that part alone.
    [Fact]
    public void Car_deduction_saysHowTheCompanyDeductsCostsItDoesNotDeductInFull()
    {
        var orderedIn2025 = PetrolCar.WithCatalogueValue(30_000m).WithCarCosts(10_000m).WithCarCo2(100).WithCarAcquired(new DateOnly(2025, 11, 1));
        var cheapDiesel = PetrolCar.WithCatalogueValue(60_000m).WithFirstRegistration(new DateOnly(2019, 3, 5)).WithCarCosts(3_500m)
            .WithCarEngine(CarEngine.Diesel).WithCarCo2(140).WithCarAcquired(new DateOnly(2019, 2, 1));

        new[] { ElectricCar, orderedIn2025, PetrolCar, cheapDiesel }
            .Select(inputs => CarCardText.DeductionHelp(Car(inputs).Result))
            .ShouldBe([null, "Help_Deduction_Rate", "Help_Deduction_None", null]);
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

    private static CarView Car(RiverInputs inputs) => CarView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition);

    private static decimal TaxesOnPay(YearResult year) =>
        year.Steps.Where(step => step.Event == TaxableEvent.DirectorPay && step.Track == Track.PaidOut).Sum(step => step.Amount);

    // The lines as amounts and destinations, their details and shares aside.
    private static IEnumerable<Drain> Lines(IEnumerable<Drain> drains) => drains.Select(drain => drain with { Details = [], Share = null });
}
