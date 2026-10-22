using Netto.Core.Car;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

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

        (view.HasCar, view.Details.Count, view.Drains.Single()).ShouldBe((false, 0, new Drain("profit_before_pay", DrainKind.PassedOn, 100_000m, "split")));
    }

    // Its costs go to the muddy pocket, the director's contribution comes back from the pocket, and the
    // profit before pay, 100,000 less 8,000 plus 600, goes on to the split.
    [Fact]
    public void Car_withAContribution_drainsItsCostsToTheMuddyPocketAndPassesTheRestOn()
    {
        Lines(Car(ElectricCar.WithCarContribution(600m)).Drains).ShouldBe(new[]
        {
            new Drain("car_costs", DrainKind.PassedOn, 8_000m, "muddy"),
            new Drain("car_contribution", DrainKind.Back, 600m, "pocket"),
            new Drain("profit_before_pay", DrainKind.PassedOn, 92_600m, "split"),
        });
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
            () => (comparison.PocketWith, comparison.PocketWithout, comparison.Muddy).ShouldBe((with.NetInPocket, without.NetInPocket, 8_000m)),
            () => comparison.CashCost.ShouldBe(without.NetInPocket - with.NetInPocket),
            () => comparison.Gap.ShouldBe(8_000m - (without.NetInPocket - with.NetInPocket)),
            () => comparison.TaxesOnPayAdded.ShouldBe(TaxesOnPay(with) - TaxesOnPay(without)));
    }

    // The benefit raises the taxes on pay; the car costs cash, less than it puts in the muddy pocket.
    [Fact]
    public void Comparison_car_costsLessCashThanItsCosts()
    {
        var comparison = CarComparison.From(ElectricCar, ElectricCar.Run()).ShouldNotBeNull();

        (comparison.TaxesOnPayAdded > 0m, comparison.CashCost > 0m, comparison.Gap > 0m).ShouldBe((true, true, true));
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

    [Theory]
    [InlineData(CarCalculator.CatalogueValueNegative)]
    [InlineData(CarCalculator.YearlyCostsNegative)]
    [InlineData(CarCalculator.ContributionNegative)]
    public void RiverErrors_carRefusal_isShownOnTheCarCard(string error)
    {
        RiverErrors.Owner(error, companySkipped: false).ShouldBe(RiverModule.Car);
    }

    private static CarView Car(RiverInputs inputs) => CarView.From(inputs.Run().Year.ShouldNotBeNull());

    private static decimal TaxesOnPay(YearResult year) =>
        year.Steps.Where(step => step.Event == TaxableEvent.DirectorPay && step.Track == Track.PaidOut).Sum(step => step.Amount);

    // The lines as amounts and destinations, their details and shares aside.
    private static IEnumerable<Drain> Lines(IEnumerable<Drain> drains) => drains.Select(drain => drain with { Details = [], Share = null });
}
