using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The line each disclosure of Director pay and Corporate tax folds its settings to, on the river's
// example (ModuleViewsTests): the director pays the contributions, the municipal rate is the
// default 7.0%, the director set up before 2024, the key formula withholds 10,674.34 on the pay, no
// advance payment is made on either tax, and the company, small and in its fifth financial year or
// later, is left a surcharge of 675.00 on its corporate tax. Namur's rate is 8.5% (MunicipalityChoiceTests).
// What the user gave says no origin, nor does what the example holds none of; what Netto assumed says
// « example » or « default »; what comes from elsewhere says where in its words.
public class SettingsViewsTests
{
    private static readonly Municipality Namur = MunicipalRates.IncomeYear2026.Municipalities.Single(municipality => municipality.Name == "Namur");

    [Fact]
    public void Situation_example_statesThePayerTheRateAndTheStartAsDefaults()
    {
        Phrases(DirectorPaySettings(RiverInputs.Example).Situation).ShouldBe(
        [
            ("Situation_PaidBy_Director", "", InputOrigin.Default),
            ("Situation_Rate", "7.0%", InputOrigin.Default),
            ("Situation_NotStarter", "2024", InputOrigin.Default),
        ]);
    }

    [Fact]
    public void Situation_companyPaysTheContributions_saysSoAsYours()
    {
        var inputs = RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company);

        Phrases(DirectorPaySettings(inputs).Situation)[0].ShouldBe(("Situation_PaidBy_Company", "", null));
    }

    [Fact]
    public void Situation_municipalityPicked_namesItsRate()
    {
        var inputs = RiverInputs.Example.WithMunicipality(RiverInputs.Example.Municipality.Pick(Namur));

        Phrases(DirectorPaySettings(inputs).Situation)[1].ShouldBe(("Situation_Rate_Official", "Namur|8.5%", null));
    }

    [Fact]
    public void Situation_rateEntered_statesItAsYours()
    {
        var inputs = RiverInputs.Example.WithMunicipality(RiverInputs.Example.Municipality.Enter(0.075m));

        Phrases(DirectorPaySettings(inputs).Situation)[1].ShouldBe(("Situation_Rate", "7.5%", null));
    }

    [Fact]
    public void Situation_rateEnteredOverAMunicipality_saysItReplacesItsRate()
    {
        var inputs = RiverInputs.Example.WithMunicipality(RiverInputs.Example.Municipality.Pick(Namur).Enter(0.09m));

        Phrases(DirectorPaySettings(inputs).Situation)[1].ShouldBe(("Situation_Rate_Overridden", "9.0%|Namur", null));
    }

    [Fact]
    public void Situation_starter_saysSinceWhenAsYours()
    {
        var inputs = RiverInputs.Example.WithStarter(true);

        Phrases(DirectorPaySettings(inputs).Situation)[2].ShouldBe(("Situation_Starter", "2024", null));
    }

    // The fund's fee is named once given, after who pays the contributions it rides on, as the user's.
    [Fact]
    public void Situation_fundFeeRateGiven_statesItAfterWhoPays()
    {
        var inputs = RiverInputs.Example.WithFundFeeRate(0.0305m);

        Phrases(DirectorPaySettings(inputs).Situation).Select(phrase => phrase.Key).ShouldBe(["Situation_PaidBy_Director", "Situation_FundFee", "Situation_Rate", "Situation_NotStarter"]);
        Phrases(DirectorPaySettings(inputs).Situation)[1].ShouldBe(("Situation_FundFee", "3.05%", null));
    }

    [Fact]
    public void Situation_fundFeeRateCleared_namesNoFeeAgain()
    {
        var inputs = RiverInputs.Example.WithFundFeeRate(0.0305m).WithFundFeeRate(null);

        Phrases(DirectorPaySettings(inputs).Situation).Select(phrase => phrase.Key).ShouldNotContain("Situation_FundFee");
    }

    [Fact]
    public void Prepaid_example_statesThePaySlipsWithholdingAndNoAdvancePaymentWithNoOrigin()
    {
        Phrases(DirectorPaySettings(RiverInputs.Example).Prepaid).ShouldBe(
        [
            ("Prepaid_Withheld_PaySlip", "€10,674.34", null),
            ("Prepaid_None", "", null),
        ]);
    }

    [Fact]
    public void Prepaid_amountWithheldEntered_statesItAsYours()
    {
        var inputs = RiverInputs.Example.WithTaxWithheld(5_000m);

        Phrases(DirectorPaySettings(inputs).Prepaid)[0].ShouldBe(("Prepaid_Withheld", "€5,000.00", null));
    }

    // Without a pay slip to derive it from, the line says where the amount will come from.
    [Fact]
    public void Prepaid_paySlipsWithholdingWhileAnotherInputIsRefused_statesNoAmount()
    {
        var inputs = RiverInputs.Example.WithReserve(-1m);

        Phrases(DirectorPaySettings(inputs).Prepaid)[0].ShouldBe(("Prepaid_Withheld_AsPaySlip", "", null));
    }

    [Fact]
    public void Prepaid_advancePaymentsEntered_statesTheirTotalAsYours()
    {
        var inputs = RiverInputs.Example.WithTaxAdvancePayment(0, 1_000m).WithTaxAdvancePayment(2, 500m);

        Phrases(DirectorPaySettings(inputs).Prepaid)[1].ShouldBe(("Prepaid_Paid", "€1,500.00", null));
    }

    // A deadline set to nothing by the user is theirs: the line no longer calls it an example.
    [Fact]
    public void Prepaid_oneDeadlineSetToNothing_statesNoAdvancePaymentAsYours()
    {
        var inputs = RiverInputs.Example.WithTaxAdvancePayment(1, 0m);

        Phrases(DirectorPaySettings(inputs).Prepaid)[1].ShouldBe(("Prepaid_None", "", null));
    }

    public static TheoryData<Refusal?, bool, bool> HoldsRefusals => new()
    {
        { GrossToNet.MunicipalRateOutOfRange, true, false },
        { GrossToNet.FundFeeRateOutOfRange, true, false },
        { GrossToNet.TaxWithheldNegative, false, true },
        { GrossToNet.TaxAdvancePaymentNegative, false, true },
        { GrossToNet.GrossPayNegative, false, false },
        { null, false, false },
    };

    [Theory]
    [MemberData(nameof(HoldsRefusals))]
    public void Holds_refusal_opensTheDisclosureHoldingItsField(Refusal? error, bool situation, bool prepaid)
    {
        (DirectorPaySettingsView.SituationHolds(error), DirectorPaySettingsView.PrepaidHolds(error)).ShouldBe((situation, prepaid));
    }

    [Fact]
    public void Company_example_statesTheStatusAndTheFinancialYearAsDefaults()
    {
        Phrases(CorporateTaxSettings(RiverInputs.Example).Company).ShouldBe(
        [
            ("Company_Small", "", InputOrigin.Default),
            ("Company_FinancialYear_FifthOrLater", "", InputOrigin.Default),
        ]);
    }

    [Fact]
    public void Company_notSmall_saysTheReducedRateIsNotOpenAsYours()
    {
        var inputs = RiverInputs.Example.WithSmallCompany(false);

        Phrases(CorporateTaxSettings(inputs).Company)[0].ShouldBe(("Company_NotSmall", "", null));
    }

    [Fact]
    public void Company_secondFinancialYear_statesItAsYours()
    {
        var inputs = RiverInputs.Example.WithFinancialYear(FinancialYear.Second);

        Phrases(CorporateTaxSettings(inputs).Company)[1].ShouldBe(("Company_FinancialYear_Second", "", null));
    }

    [Fact]
    public void Payments_example_statesNoPaymentWithNoOriginAndTheSurchargeLeft()
    {
        Phrases(CorporateTaxSettings(RiverInputs.Example).Payments).ShouldBe(
        [
            ("Payments_None", "", null),
            ("Payments_Surcharge", "€675.00", null),
        ]);
    }

    [Fact]
    public void Payments_enoughBy10April_statesThemAsYoursAndNoSurcharge()
    {
        var inputs = RiverInputs.Example.WithAdvancePayment(0, 7_500m);

        Phrases(CorporateTaxSettings(inputs).Payments).ShouldBe(
        [
            ("Payments_Paid", "€7,500.00", null),
            ("Payments_NoSurcharge", "", null),
        ]);
    }

    // While an amount is refused there is no year to say what surcharge the payments leave.
    [Fact]
    public void Payments_whileAnInputIsRefused_stateOnlyThePayments()
    {
        var inputs = RiverInputs.Example.WithReserve(-1m);

        Phrases(CorporateTaxSettings(inputs).Payments).ShouldBe([("Payments_None", "", null)]);
    }

    public static TheoryData<Refusal?, bool> PaymentsHoldRefusals => new()
    {
        { YearSimulation.AdvancePaymentNegative, true },
        { GrossToNet.TaxAdvancePaymentNegative, false },
        { null, false },
    };

    [Theory]
    [MemberData(nameof(PaymentsHoldRefusals))]
    public void PaymentsHold_refusal_opensTheDisclosureHoldingItsField(Refusal? error, bool payments)
    {
        CorporateTaxSettingsView.PaymentsHold(error).ShouldBe(payments);
    }

    // Home office rent's disclosure, « Your home »: the facts the rent is capped by, the cadastral
    // income of the home and the share of it let. The example holds none of either, which names no
    // origin.
    [Fact]
    public void Home_example_statesTheCadastralIncomeAndTheShareWithNoOrigin()
    {
        Phrases(RentSettingsView.From(RiverInputs.Example).Home).ShouldBe(
        [
            ("Home_CadastralIncome", "€0.00", null),
            ("Home_Share", "0%", null),
        ]);
    }

    [Fact]
    public void Home_factsGiven_stateThemWithNoOrigin()
    {
        var inputs = RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.125m);

        Phrases(RentSettingsView.From(inputs).Home).ShouldBe(
        [
            ("Home_CadastralIncome", "€1,200.00", null),
            ("Home_Share", "12.5%", null),
        ]);
    }

    // The rent has no cap, so cannot be given, until a part of the home is let: the facts open by
    // themselves when the card is first drawn.
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1_200, 0, true)]
    [InlineData(0, 0.12, true)]
    [InlineData(1_200, 0.12, false)]
    public void NeedsFacts_cadastralIncomeAndShare_isTrueUntilBothAreGiven(decimal cadastralIncome, decimal share, bool needed)
    {
        var inputs = RiverInputs.Example.WithCadastralIncome(cadastralIncome).WithShareLet(share);

        RentSettingsView.From(inputs).NeedsFacts.ShouldBe(needed);
    }

    // The rent's own refusals are on a field in sight: only the two facts open their disclosure.
    public static TheoryData<Refusal?, bool> HomeHoldsRefusals => new()
    {
        { RentCalculator.CadastralIncomeNegative, true },
        { RentCalculator.ShareLetOutOfRange, true },
        { RentCalculator.RentNegative, false },
        { RentCalculator.RentAboveCap, false },
        { RentCalculator.RentWithOfficeAllowance, false },
        { GrossToNet.GrossPayNegative, false },
        { null, false },
    };

    [Theory]
    [MemberData(nameof(HomeHoldsRefusals))]
    public void HomeHolds_refusal_opensTheDisclosureHoldingItsField(Refusal? error, bool home)
    {
        RentSettingsView.HomeHolds(error).ShouldBe(home);
    }

    // The company car's disclosure, « Your car »: what the car is, as its papers say. The yearly costs,
    // the contribution and the private energy the company pays stay in sight. On the example the car
    // is assumed new and electric, with no catalogue value yet, which names no origin.
    [Fact]
    public void Car_example_statesTheEngineAsADefaultTheDatesAsExamplesAndTheValueWithNoOrigin()
    {
        Phrases(CarSettingsView.From(RiverInputs.Example).Car).ShouldBe(
        [
            ("Car_Engine_Electric", "", InputOrigin.Default),
            ("Car_Catalogue", "€0.00", null),
            ("Car_Registered", "January|2026", InputOrigin.Example),
            ("Car_Acquired", "January|2026", InputOrigin.Example),
        ]);
    }

    [Fact]
    public void Car_factsGiven_stateThemWithNoOriginAndTheCo2AfterTheValue()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.Petrol).WithCatalogueValue(45_000m).WithCarCo2(118)
            .WithFirstRegistration(new DateOnly(2021, 3, 1)).WithCarAcquired(new DateOnly(2020, 9, 1));

        Phrases(CarSettingsView.From(inputs).Car).ShouldBe(
        [
            ("Car_Engine_Petrol", "", null),
            ("Car_Catalogue", "€45,000.00", null),
            ("Car_Co2", "118 g/km", null),
            ("Car_Registered", "March|2021", null),
            ("Car_Acquired", "September|2020", null),
        ]);
    }

    // Held all year says nothing; a day the car is had from or given back on is stated after its order.
    [Fact]
    public void Car_daysHeldGiven_stateThemAfterTheOrder()
    {
        var inputs = RiverInputs.Example.WithCarHeldFrom(new DateOnly(2026, 9, 17)).WithCarReturnedOn(new DateOnly(2027, 3, 1));

        Phrases(CarSettingsView.From(inputs).Car).Skip(4).ShouldBe(
        [
            ("Car_HeldFrom", "17 September 2026", null),
            ("Car_ReturnedOn", "1 March 2027", null),
        ]);
    }

    [Fact]
    public void Car_onlyReturnedOn_statesItAlone()
    {
        var inputs = RiverInputs.Example.WithCarReturnedOn(new DateOnly(2026, 8, 8));

        Phrases(CarSettingsView.From(inputs).Car).Skip(4).ShouldBe([("Car_ReturnedOn", "8 August 2026", null)]);
    }

    // An electric car emits none: a CO2 kept from another engine is not stated.
    [Fact]
    public void Car_electricWithACo2Kept_statesNoCo2()
    {
        var inputs = RiverInputs.Example.WithCarCo2(118);

        Phrases(CarSettingsView.From(inputs).Car).Select(phrase => phrase.Key).ShouldNotContain("Car_Co2");
    }

    [Fact]
    public void Car_combustionWithoutACo2_saysItIsNotGiven()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.Diesel);

        Phrases(CarSettingsView.From(inputs).Car)[2].ShouldBe(("Car_Co2_NotGiven", "", null));
    }

    [Fact]
    public void Car_combustionWithNoCo2Registered_saysSo()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.Petrol).WithCarCo2(118).WithNoCo2Data(true);

        Phrases(CarSettingsView.From(inputs).Car)[2].ShouldBe(("Car_Co2_NoData", "", null));
    }

    [Fact]
    public void Car_plugInHybridNotSaidToBeFake_statesItAsADefault()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(30);

        Phrases(CarSettingsView.From(inputs).Car)[^1].ShouldBe(("Car_NotFakeHybrid", "", InputOrigin.Default));
    }

    [Fact]
    public void Car_fakeHybridWithAMatchingCar_statesItsCo2()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.DieselPlugInHybrid).WithCarCo2(30).WithFakeHybrid(true).WithMatchingCarCo2(112);

        Phrases(CarSettingsView.From(inputs).Car)[^1].ShouldBe(("Car_FakeHybrid", "112 g/km", null));
    }

    [Fact]
    public void Car_fakeHybridWithNoMatchingCar_saysSo()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(30).WithFakeHybrid(true).WithNoMatchingCar(true);

        Phrases(CarSettingsView.From(inputs).Car)[^1].ShouldBe(("Car_FakeHybrid_NoMatching", "", null));
    }

    [Fact]
    public void Car_fakeHybridWithoutItsMatchingCarsCo2_saysItIsNotGiven()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(30).WithFakeHybrid(true);

        Phrases(CarSettingsView.From(inputs).Car)[^1].ShouldBe(("Car_FakeHybrid_NotGiven", "", null));
    }

    // The rule is the engine's from 1 January 2018: for an older plug-in hybrid the car counts at its own
    // CO2 whatever was ticked, so the line says that and no matching car's CO2, the values kept.
    [Fact]
    public void Car_plugInHybridBefore2018WithTheTickKept_saysItsOwnCo2CountsFromTheYearTheRuleStarts()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.PetrolPlugInHybrid).WithCarCo2(120).WithCarAcquired(new DateOnly(2017, 6, 1))
            .WithFakeHybrid(true).WithMatchingCarCo2(150);

        var phrases = Phrases(CarSettingsView.From(inputs).Car);

        (phrases[^1], phrases.Select(phrase => phrase.Key).Any(key => key.StartsWith("Car_FakeHybrid", StringComparison.Ordinal) && key != "Car_FakeHybrid_NotYet"))
            .ShouldBe((("Car_FakeHybrid_NotYet", "2018", null), false));
    }

    [Theory]
    [InlineData(CarEngine.PetrolPlugInHybrid, 2017, false)]
    [InlineData(CarEngine.DieselPlugInHybrid, 2017, false)]
    [InlineData(CarEngine.PetrolPlugInHybrid, 2018, true)]
    [InlineData(CarEngine.PetrolHybrid, 2024, false)]
    [InlineData(CarEngine.Electric, 2024, false)]
    public void FakeHybridApplies_engineAndAcquisitionDate_isTheEnginesDecisionForAPlugInHybrid(CarEngine engine, int acquiredIn, bool applies)
    {
        var inputs = RiverInputs.Example.WithCarEngine(engine).WithCarAcquired(new DateOnly(acquiredIn, 6, 1));

        CarSettingsView.From(inputs).FakeHybridApplies.ShouldBe(applies);
    }

    [Fact]
    public void Car_hybridWhichIsNotPlugIn_statesNothingAboutFakeHybrids()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.PetrolHybrid).WithCarCo2(100);

        Phrases(CarSettingsView.From(inputs).Car).Select(phrase => phrase.Key).ShouldNotContain(key => key.Contains("FakeHybrid"));
    }

    [Fact]
    public void Car_naturalGas_statesTheHorsepowerAsADefaultUntilSaid()
    {
        var inputs = RiverInputs.Example.WithCarEngine(CarEngine.NaturalGas).WithCarCo2(100);

        (Phrases(CarSettingsView.From(inputs).Car)[^1], Phrases(CarSettingsView.From(inputs.WithSmallNaturalGas(true)).Car)[^1]).ShouldBe(
            (("Car_NotSmallGas", "", InputOrigin.Default), ("Car_SmallGas", "", null)));
    }

    // No car counts until it has a catalogue value: the facts open by themselves when the card is
    // first drawn.
    [Theory]
    [InlineData(0, true)]
    [InlineData(45_000, false)]
    public void NeedsFacts_catalogueValue_isTrueWhileNoneIsGiven(decimal catalogueValue, bool needed)
    {
        CarSettingsView.From(RiverInputs.Example.WithCatalogueValue(catalogueValue)).NeedsFacts.ShouldBe(needed);
    }

    // The car's facts hold the refusals of their fields; its yearly costs and its contribution are in
    // sight.
    public static TheoryData<Refusal?, bool> CarHoldsRefusals => new()
    {
        { CarCalculator.CatalogueValueNegative, true },
        { CarCalculator.Co2Negative, true },
        { CarCalculator.Co2Missing, true },
        { CarCalculator.MatchingCarCo2Negative, true },
        { CarCalculator.MatchingCarCo2Missing, true },
        { CarCalculator.AcquiredAfterYear, true },
        { CarCalculator.PlugInFuelCapped, true },
        { CarCalculator.HeldAfterYear, true },
        { CarCalculator.ReturnedBeforeYear, true },
        { CarCalculator.ReturnedBeforeHeld, true },
        { CarCalculator.YearlyCostsNegative, false },
        { CarCalculator.ContributionNegative, false },
        { GrossToNet.GrossPayNegative, false },
        { null, false },
    };

    [Theory]
    [MemberData(nameof(CarHoldsRefusals))]
    public void CarHolds_refusal_opensTheDisclosureHoldingItsField(Refusal? error, bool car)
    {
        CarSettingsView.CarHolds(error).ShouldBe(car);
    }

    // The copyright royalty's disclosure, « Your copyright situation »: what Netto cannot see of the
    // director's copyright income and works. How the royalty is set is the card's own, outside. The share of working
    // time spent creating has no default: it is none until given, and names no origin; while the engine
    // says the royalty that follows the pay is none for want of it, as the example's added is, the line
    // says it is needed.
    [Fact]
    public void Situation_royaltyAddedToTheExample_statesTheStatusesAsDefaultsAndThatTheShareIsNeeded()
    {
        Phrases(CopyrightSettings(RiverInputs.Example.AddToPlan(OptionalModule.Copyright)).Situation).ShouldBe(
        [
            ("Settings_AverageWithin", "", InputOrigin.Default),
            ("Settings_Attestation_None", "", InputOrigin.Default),
            ("Settings_CreativeShare_Needed", "", null),
        ]);
    }

    // A fixed royalty does not read the share: none given is only none.
    [Fact]
    public void Situation_fixedRoyaltyWithoutAShare_saysNoneIsGiven()
    {
        Phrases(CopyrightSettings(RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m)).Situation)[2]
            .ShouldBe(("Settings_CreativeShare_None", "", null));
    }

    // A royalty that follows the pay is none without the share, as the engine says (NoCreativeShare): the
    // facts open by themselves.
    [Theory]
    [InlineData(true, null, true)]
    [InlineData(true, 0.4, false)]
    [InlineData(false, null, false)]
    public void NeedsFacts_creativeShare_isTrueWhileTheEngineSaysTheFollowingRoyaltyLacksIt(bool followsPay, double? share, bool needed)
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m).WithCreativeShare((decimal?)share);

        CopyrightSettings(followsPay ? inputs.WithRoyaltyFollowingPay() : inputs).NeedsFacts.ShouldBe(needed);
    }

    // With the four previous years above the cap, the engine says so first: a share would change
    // nothing, so it is neither needed nor opens the facts.
    [Fact]
    public void Situation_followingRoyaltyAboveTheCapWithoutAShare_saysNoneIsGivenAndStaysFolded()
    {
        var view = CopyrightSettings(RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithAverageWithinCap(false));

        (view.NeedsFacts, Phrases(view.Situation)[2]).ShouldBe((false, ("Settings_CreativeShare_None", "", (InputOrigin?)null)));
    }

    // While the river is refused there is no year to say why: a royalty that follows the pay without a
    // share is taken as needing it, which tests no rule.
    [Fact]
    public void NeedsFacts_riverRefused_isTrueWhileTheFollowingRoyaltyHasNoShare()
    {
        CopyrightSettings(RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithReserve(-1m)).NeedsFacts.ShouldBeTrue();
    }

    [Fact]
    public void Situation_statusesAndShareGiven_stateThemWithNoOrigin()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithAverageWithinCap(false).WithAttestation(true).WithCreativeShare(0.4m);

        Phrases(CopyrightSettings(inputs).Situation).ShouldBe(
        [
            ("Settings_AverageAbove", "", null),
            ("Settings_Attestation_Holds", "", null),
            ("Settings_CreativeShare", "40%", null),
        ]);
    }

    [Fact]
    public void Situation_creativeShareCleared_isNeededAgain()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.4m).WithCreativeShare(null);

        Phrases(CopyrightSettings(inputs).Situation)[2].ShouldBe(("Settings_CreativeShare_Needed", "", null));
    }

    private static CopyrightSettingsView CopyrightSettings(RiverInputs inputs) => CopyrightSettingsView.From(inputs, inputs.Run());

    // Only the share's refusal is on a field inside; the royalty's is on its own field outside.
    public static TheoryData<Refusal?, bool> SituationHoldsRefusals => new()
    {
        { CopyrightCalculator.CreativeShareOutOfRange, true },
        { CopyrightCalculator.RoyaltyNegative, false },
        { GrossToNet.GrossPayNegative, false },
        { null, false },
    };

    [Theory]
    [MemberData(nameof(SituationHoldsRefusals))]
    public void SituationHolds_refusal_opensTheDisclosureHoldingItsField(Refusal? error, bool situation)
    {
        CopyrightSettingsView.SituationHolds(error).ShouldBe(situation);
    }

    // The dividend's disclosure, « Your shares and earlier profits »: what the company kept from earlier
    // years, which it can still pay out, and the withholding rate the user declares for their shares.
    // What is kept this year, the slider, stays in sight. On the example, the 2026 rate of VVPRbis
    // shares from their third distribution is 18%.
    [Fact]
    public void Shares_example_statesNoEarlierProfitsWithNoOriginAndTheCategoryAsADefault()
    {
        Phrases(DividendSettingsView.From(RiverInputs.Example).Facts).ShouldBe(
        [
            ("Settings_Earlier_None", "", null),
            ("Settings_Category_VvprbisThirdYearOnwards", "18%", InputOrigin.Default),
        ]);
    }

    [Fact]
    public void Shares_earlierProfitsAndCategoryGiven_stateThemWithNoOrigin()
    {
        var inputs = RiverInputs.Example.WithEarlierProfits(80_000m).WithDividendCategory(WithholdingCategory.Standard);

        Phrases(DividendSettingsView.From(inputs).Facts).ShouldBe(
        [
            ("Settings_Earlier", "€80,000.00", null),
            ("Settings_Category_Standard", "30%", null),
        ]);
    }

    // A nothing the user typed is theirs, and names no origin either.
    [Fact]
    public void Shares_earlierProfitsSetToNothing_statesNoneAsYours()
    {
        var inputs = RiverInputs.Example.WithEarlierProfits(0m);

        Phrases(DividendSettingsView.From(inputs).Facts)[0].ShouldBe(("Settings_Earlier_None", "", null));
    }

    // The kept amount's refusal is on the slider's field, in sight.
    public static TheoryData<Refusal?, bool> SharesHoldRefusals => new()
    {
        { YearSimulation.EarlierProfitsNegative, true },
        { YearSimulation.ReserveNegative, false },
        { null, false },
    };

    [Theory]
    [MemberData(nameof(SharesHoldRefusals))]
    public void SharesHold_refusal_opensTheDisclosureHoldingItsField(Refusal? error, bool shares)
    {
        DividendSettingsView.SharesHold(error).ShouldBe(shares);
    }

    // Company year's disclosure states the band of INASTI's company contribution, with the year of the
    // balance sheet it reads, two years before the income year, and the threshold between the two bands.
    [Fact]
    public void BalanceSheet_example_statesTheLowerBandAsADefault()
    {
        Phrases(CompanyYearSettingsView.From(RiverInputs.Example).BalanceSheet).ShouldBe([("BandLine_Lower", "2024|€879,163.44", InputOrigin.Default)]);
    }

    [Fact]
    public void BalanceSheet_upperBandGiven_statesItAsYours()
    {
        var inputs = RiverInputs.Example.WithCompanyContributionBand(CompanyContributionBand.Upper);

        Phrases(CompanyYearSettingsView.From(inputs).BalanceSheet).ShouldBe([("BandLine_Upper", "2024|€879,163.44", null)]);
    }

    [Fact]
    public void BalanceSheet_exemptGiven_statesItAsYours()
    {
        var inputs = RiverInputs.Example.WithCompanyContributionBand(CompanyContributionBand.Exempt);

        Phrases(CompanyYearSettingsView.From(inputs).BalanceSheet).ShouldBe([("BandLine_Exempt", "2024|€879,163.44", null)]);
    }

    // A year without the contribution's rules has no band to state.
    [Fact]
    public void BalanceSheet_yearWithoutRules_statesNothing()
    {
        CompanyYearSettingsView.From(RiverInputs.Example with { IncomeYear = Fixtures.UnsupportedIncomeYear }).BalanceSheet.ShouldBeEmpty();
    }

    private static DirectorPaySettingsView DirectorPaySettings(RiverInputs inputs) => DirectorPaySettingsView.From(inputs, inputs.Run());

    private static CorporateTaxSettingsView CorporateTaxSettings(RiverInputs inputs) => CorporateTaxSettingsView.From(inputs, inputs.Run());

    // « About you two »: neither status given, both assumed; once ticked, the user's.
    [Fact]
    public void AboutYouTwo_example_statesBothStatusesAsDefaults()
    {
        Phrases(HouseholdSettingsView.From(RiverInputs.Example).Situation).ShouldBe(
        [
            ("Situation_NotBothAged66", "2027", InputOrigin.Default),
            ("Situation_TaxedJointly", "", InputOrigin.Default),
        ]);
    }

    [Fact]
    public void AboutYouTwo_bothTicked_statesThemAsYours()
    {
        var inputs = RiverInputs.Example.WithBothAged66(true).WithTaxedSeparately(true);

        Phrases(HouseholdSettingsView.From(inputs).Situation).ShouldBe([("Situation_BothAged66", "2027", null), ("Situation_TaxedSeparately", "", null)]);
    }

    private static List<(string Key, string Args, InputOrigin? Origin)> Phrases(IEnumerable<SettingPhrase> phrases) =>
        [.. phrases.Select(phrase => (phrase.Key, string.Join('|', phrase.Args), phrase.Origin))];
}
