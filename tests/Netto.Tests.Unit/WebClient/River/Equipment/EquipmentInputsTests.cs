using Netto.Core.Equipment;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.River.Equipment;

// The IT and phone as the river holds it: out of the example's plan with nothing in it, each input the user's once
// typed, handed to the engine only while it is in the plan, its values kept while it is out and back with it, and
// offered by « Add to plan », which says the values that wait for it.
public class EquipmentInputsTests
{
    private static readonly RiverInputs Set = EquipmentViewTests.Set;

    // The example holds no IT and phone: out of the plan, no piece, no money and no origin to show, the internet
    // connection not provided and the whole year, both assumed, and no private cost given.
    [Fact]
    public void Example_holdsNoneOutOfThePlan()
    {
        var set = RiverInputs.Example.Equipment;

        set.ShouldSatisfyAllConditions(
            () => RiverInputs.Example.InPlan(OptionalModule.Equipment).ShouldBeFalse(),
            () => new[] { set.Pcs, set.Tablets, set.MobilePhones, set.PhoneSubscriptions }.ShouldAllBe(count => count == new Input<int>(0, InputOrigin.None)),
            () => (set.YearlyCosts, set.Contribution).ShouldBe((new Input<decimal>(0m, InputOrigin.None), new Input<decimal>(0m, InputOrigin.None))),
            () => (set.Internet, set.ProvidedFrom, set.ReturnedOn, set.PrivateCost)
                .ShouldBe((new Input<bool>(false, InputOrigin.Default), EquipmentInputs.AllYear, EquipmentInputs.AllYear, EquipmentInputs.NoPrivateCost)));
    }

    // Each value typed is the user's, and reaches the engine as typed while the set is in the plan.
    [Fact]
    public void Scenario_inThePlan_handsTheEngineTheSetAsTyped()
    {
        var inputs = Set.WithTablets(2).WithEquipmentContribution(20m).WithEquipmentProvidedFrom(new DateOnly(2026, 3, 1)).WithEquipmentReturnedOn(new DateOnly(2026, 11, 30));

        (inputs.Equipment.Tablets.Origin, inputs.Equipment.Internet.Origin, inputs.Equipment.ProvidedFrom.Origin, inputs.Run().Year!.Scenario.Equipment).ShouldBe((
            InputOrigin.Yours, InputOrigin.Yours, InputOrigin.Yours,
            new EquipmentFacts(1, 2, 1, true, 1, 1_500m, 20m) { ProvidedFrom = new DateOnly(2026, 3, 1), ReturnedOn = new DateOnly(2026, 11, 30) }));
    }

    // Taken out of the plan, the set neither shows nor counts: the engine sees none of it, the year holds nothing of
    // it, the bar's expenses and the map leave it out; its values are kept, and come back with it, the year as it was.
    [Fact]
    public void Scenario_outOfThePlan_neitherShowsNorCountsAndKeepsItsValues()
    {
        var takenOut = Set.TakeOutOfPlan(OptionalModule.Equipment);
        var year = takenOut.Run().Year.ShouldNotBeNull();
        var back = takenOut.AddToPlan(OptionalModule.Equipment);

        takenOut.ShouldSatisfyAllConditions(
            () => year.Scenario.Equipment.ShouldBe(EquipmentFacts.None),
            () => (year.Equipment.Pieces.Count, year.Equipment.Benefit, year.Equipment.Costs).ShouldBe((0, 0m, 0m)),
            () => BucketsView.ExpensesOf(year).ShouldBe(year.CompanyContribution),
            () => MapView.ModulesOf(takenOut).ShouldNotContain(RiverModule.Equipment),
            () => takenOut.Equipment.ShouldBe(Set.Equipment),
            () => (back, back.Run().Year!.NetInPocket).ShouldBe((Set, Set.Run().Year!.NetInPocket)));
    }

    // In the plan, the bar's expenses hold what the set costs the company, beside INASTI's company contribution.
    [Fact]
    public void Expenses_inThePlan_holdWhatTheSetCostsTheCompany()
    {
        var year = Set.Run().Year.ShouldNotBeNull();

        BucketsView.ExpensesOf(year).ShouldBe(year.CompanyContribution + 1_500m);
    }

    // « Add to plan » offers the IT and phone between the rent and the car; out of the plan with values of the user's,
    // it says they wait; in the plan, it brings what it costs the company a year.
    [Fact]
    public void Composition_offersItBetweenTheRentAndTheCar_andSaysItsValuesWait()
    {
        var takenOut = Set.TakeOutOfPlan(OptionalModule.Equipment);

        var (outOfPlan, inPlan) = (CompositionView.From(takenOut, takenOut.Run()), CompositionView.From(Set, Set.Run()));

        outOfPlan.ShouldSatisfyAllConditions(
            () => outOfPlan.Modules.Select(choice => choice.Module).SkipWhile(module => module != OptionalModule.Rent).Take(3)
                .ShouldBe([OptionalModule.Rent, OptionalModule.Equipment, OptionalModule.Car]),
            () => outOfPlan.Modules.Single(choice => choice.Module == OptionalModule.Equipment).ShouldBe(new ModuleChoice(OptionalModule.Equipment, false, null, true)),
            () => inPlan.Modules.Single(choice => choice.Module == OptionalModule.Equipment).ShouldBe(new ModuleChoice(OptionalModule.Equipment, true, 1_500m, false)));
    }
}
