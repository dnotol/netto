using Netto.Core.PersonalTax;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The household as the user enters it on the Household card: whether they have a partner, a status assumed
// so until the user says otherwise; the partner's net professional income, money with no default, none
// until typed; whether both partners are 66 on 1 January of the tax year, and whether the year is one the
// couple is taxed separately in, statuses assumed false; and the dependants, counts the example holds none
// of, and whether the director raises their children alone, a status assumed false.
public class HouseholdInputsTests
{
    [Fact]
    public void Example_holdsNoPartnerIncomeAndAssumesNeitherStatus()
    {
        var household = RiverInputs.Example.Household;

        (household.PartnerIncome, household.BothAged66, household.TaxedSeparately).ShouldBe((
            new Input<decimal>(0m, InputOrigin.None), new Input<bool>(false, InputOrigin.Default), new Input<bool>(false, InputOrigin.Default)));
    }

    // Adding the household starts from a partner, as adding it did when it held only the partner.
    [Fact]
    public void Example_assumesAPartnerAndHoldsNoDependants()
    {
        var household = RiverInputs.Example.Household;

        (household.Status, household.Dependants).ShouldBe((
            new Input<HouseholdStatus>(HouseholdStatus.MarriedOrLegalCohabitant, InputOrigin.Default),
            new DependantInputs(None(0), None(0), new Input<bool>(false, InputOrigin.Default), None(0), None(0))));
    }

    [Fact]
    public void WithEachFact_makesItTheUsers()
    {
        var household = RiverInputs.Example.WithPartnerIncome(12_000m).WithBothAged66(true).WithTaxedSeparately(true).Household;

        (household.PartnerIncome, household.BothAged66, household.TaxedSeparately).ShouldBe((
            new Input<decimal>(12_000m, InputOrigin.Yours), new Input<bool>(true, InputOrigin.Yours), new Input<bool>(true, InputOrigin.Yours)));
    }

    [Fact]
    public void WithEachDependant_makesItTheUsers()
    {
        var household = RiverInputs.Example.WithPartner(false).WithChildren(3).WithChildrenUnder3(1).WithSingleParent(true).WithDependantsInCare(1)
            .WithOtherDependants(2).Household;

        (household.Status, household.Dependants).ShouldBe((
            new Input<HouseholdStatus>(HouseholdStatus.Alone, InputOrigin.Yours),
            new DependantInputs(Yours(3), Yours(1), new Input<bool>(true, InputOrigin.Yours), Yours(1), Yours(2))));
    }

    // A partner is a spouse or a legal cohabitant: the one status the engine taxes jointly.
    [Fact]
    public void Values_areAMarriedOrLegallyCohabitingPartnersFacts()
    {
        var household = RiverInputs.Example.WithPartnerIncome(12_000m).WithBothAged66(true).Household;

        household.Values.ShouldBe(new Household(HouseholdStatus.MarriedOrLegalCohabitant, 12_000m, BothAged66: true, TaxedSeparately: false));
    }

    [Fact]
    public void Values_partnerWithDependants_holdsBoth()
    {
        var household = RiverInputs.Example.WithPartnerIncome(12_000m).WithChildren(2).WithChildrenUnder3(1).Household;

        household.Values.ShouldBe(new Household(HouseholdStatus.MarriedOrLegalCohabitant, 12_000m) { Dependants = new Dependants(2, 1) });
    }

    // With no partner the director is taxed alone with their dependants, the partner's facts kept unread.
    [Fact]
    public void Values_noPartner_isTheDirectorAloneWithTheirDependants()
    {
        var household = RiverInputs.Example.WithPartnerIncome(12_000m).WithTaxedSeparately(true).WithPartner(false).WithChildren(2).WithSingleParent(true)
            .WithDependantsInCare(1).WithOtherDependants(1).Household;

        (household.Values, household.PartnerIncome.Value).ShouldBe((
            new Household(HouseholdStatus.Alone) { Dependants = new Dependants(2, 0, SingleParent: true, DependantsAged66InCare: 1, OtherDependants: 1) }, 12_000m));
    }

    private static Input<int> None(int value) => new(value, InputOrigin.None);

    private static Input<int> Yours(int value) => new(value, InputOrigin.Yours);
}
