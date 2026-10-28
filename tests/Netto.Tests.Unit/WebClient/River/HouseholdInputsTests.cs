using Netto.Core.PersonalTax;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The partner as the user enters them on the Household card: their net professional income, money with
// no default, none until typed; whether both partners are 66 on 1 January of the tax year, and whether
// the year is one the couple is taxed separately in, statuses assumed false until the user says otherwise.
public class HouseholdInputsTests
{
    [Fact]
    public void Example_holdsNoPartnerIncomeAndAssumesNeitherStatus()
    {
        var household = RiverInputs.Example.Household;

        (household.PartnerIncome, household.BothAged66, household.TaxedSeparately).ShouldBe((
            new Input<decimal>(0m, InputOrigin.None), new Input<bool>(false, InputOrigin.Default), new Input<bool>(false, InputOrigin.Default)));
    }

    [Fact]
    public void WithEachFact_makesItTheUsers()
    {
        var household = RiverInputs.Example.WithPartnerIncome(12_000m).WithBothAged66(true).WithTaxedSeparately(true).Household;

        (household.PartnerIncome, household.BothAged66, household.TaxedSeparately).ShouldBe((
            new Input<decimal>(12_000m, InputOrigin.Yours), new Input<bool>(true, InputOrigin.Yours), new Input<bool>(true, InputOrigin.Yours)));
    }

    // A partner is a spouse or a legal cohabitant: the one status the engine taxes jointly.
    [Fact]
    public void Values_areAMarriedOrLegallyCohabitingPartnersFacts()
    {
        var household = RiverInputs.Example.WithPartnerIncome(12_000m).WithBothAged66(true).Household;

        household.Values.ShouldBe(new Household(HouseholdStatus.MarriedOrLegalCohabitant, 12_000m, BothAged66: true, TaxedSeparately: false));
    }
}
