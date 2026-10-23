using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The line each disclosure of Director pay and Corporate tax folds its settings to, on the river's
// example (ModuleViewsTests): the director pays the contributions, the municipal rate is the
// default 7.0%, the director set up before 2024, the key formula withholds 10,691.28 on the pay, no
// advance payment is made on either tax, and the company, small and in its fifth financial year or
// later, is left a surcharge of 675.00 on its corporate tax. Namur's rate is 8.5% (MunicipalityChoiceTests).
// What the user gave says no origin; what Netto assumed says « example » or « default »; what comes
// from elsewhere says where in its words.
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

    [Fact]
    public void Prepaid_example_statesThePaySlipsWithholdingAndNoAdvancePaymentAsTheExamples()
    {
        Phrases(DirectorPaySettings(RiverInputs.Example).Prepaid).ShouldBe(
        [
            ("Prepaid_Withheld_PaySlip", "€10,691.28", null),
            ("Prepaid_None", "", InputOrigin.Example),
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

    [Theory]
    [InlineData(GrossToNet.MunicipalRateOutOfRange, true, false)]
    [InlineData(GrossToNet.TaxWithheldNegative, false, true)]
    [InlineData(GrossToNet.TaxAdvancePaymentNegative, false, true)]
    [InlineData(GrossToNet.GrossPayNegative, false, false)]
    [InlineData(null, false, false)]
    public void Holds_refusal_opensTheDisclosureHoldingItsField(string? error, bool situation, bool prepaid)
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
    public void Payments_example_statesNoPaymentAsTheExampleAndTheSurchargeLeft()
    {
        Phrases(CorporateTaxSettings(RiverInputs.Example).Payments).ShouldBe(
        [
            ("Payments_None", "", InputOrigin.Example),
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

        Phrases(CorporateTaxSettings(inputs).Payments).ShouldBe([("Payments_None", "", InputOrigin.Example)]);
    }

    [Theory]
    [InlineData(YearSimulation.AdvancePaymentNegative, true)]
    [InlineData(GrossToNet.TaxAdvancePaymentNegative, false)]
    [InlineData(null, false)]
    public void PaymentsHold_refusal_opensTheDisclosureHoldingItsField(string? error, bool payments)
    {
        CorporateTaxSettingsView.PaymentsHold(error).ShouldBe(payments);
    }

    // Home office rent's disclosure, « Your home »: the facts the rent is capped by, the cadastral
    // income of the home and the share of it let. On the example both are the example's nothing.
    [Fact]
    public void Home_example_statesTheCadastralIncomeAndTheShareAsTheExamples()
    {
        Phrases(RentSettingsView.From(RiverInputs.Example).Home).ShouldBe(
        [
            ("Home_CadastralIncome", "€0.00", InputOrigin.Example),
            ("Home_Share", "0%", InputOrigin.Example),
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
    [Theory]
    [InlineData(RentCalculator.CadastralIncomeNegative, true)]
    [InlineData(RentCalculator.ShareLetOutOfRange, true)]
    [InlineData(RentCalculator.RentNegative, false)]
    [InlineData(RentCalculator.RentAboveCap, false)]
    [InlineData(RentCalculator.RentWithOfficeAllowance, false)]
    [InlineData(GrossToNet.GrossPayNegative, false)]
    [InlineData(null, false)]
    public void HomeHolds_refusal_opensTheDisclosureHoldingItsField(string? error, bool home)
    {
        RentSettingsView.HomeHolds(error).ShouldBe(home);
    }

    // The company car's disclosure, « Your car »: what the car is, as its papers say. The yearly costs,
    // the contribution and the private energy the company pays stay in sight. On the example the car
    // is assumed new and electric, with no catalogue value yet.
    [Fact]
    public void Car_example_statesTheEngineAsADefaultAndTheValueAndDatesAsExamples()
    {
        Phrases(CarSettingsView.From(RiverInputs.Example).Car).ShouldBe(
        [
            ("Car_Engine_Electric", "", InputOrigin.Default),
            ("Car_Catalogue", "€0.00", InputOrigin.Example),
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
    [Theory]
    [InlineData(CarCalculator.CatalogueValueNegative, true)]
    [InlineData(CarCalculator.Co2Negative, true)]
    [InlineData(CarCalculator.Co2Missing, true)]
    [InlineData(CarCalculator.MatchingCarCo2Negative, true)]
    [InlineData(CarCalculator.MatchingCarCo2Missing, true)]
    [InlineData(CarCalculator.AcquiredAfterYear, true)]
    [InlineData(CarCalculator.PlugInFuelCapped, true)]
    [InlineData(CarCalculator.YearlyCostsNegative, false)]
    [InlineData(CarCalculator.ContributionNegative, false)]
    [InlineData(GrossToNet.GrossPayNegative, false)]
    [InlineData(null, false)]
    public void CarHolds_refusal_opensTheDisclosureHoldingItsField(string? error, bool car)
    {
        CarSettingsView.CarHolds(error).ShouldBe(car);
    }

    // The copyright royalty's disclosure, « Your copyright situation »: what Netto cannot see of the
    // director's copyright income and works. The royalty itself is the split's. The share of working
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

    // Only the share's refusal is on a field inside; the royalty's is on the split.
    [Theory]
    [InlineData(CopyrightCalculator.CreativeShareOutOfRange, true)]
    [InlineData(CopyrightCalculator.RoyaltyNegative, false)]
    [InlineData(GrossToNet.GrossPayNegative, false)]
    [InlineData(null, false)]
    public void SituationHolds_refusal_opensTheDisclosureHoldingItsField(string? error, bool situation)
    {
        CopyrightSettingsView.SituationHolds(error).ShouldBe(situation);
    }

    // The dividend's disclosure, « Your shares and earlier profits »: what the company kept from earlier
    // years, which it can still pay out, and the withholding rate the user declares for their shares.
    // What is kept this year, the slider, stays in sight. On the example, the 2026 rate of VVPRbis
    // shares from their third distribution is 18%.
    [Fact]
    public void Shares_example_statesNoEarlierProfitsAsTheExampleAndTheCategoryAsADefault()
    {
        Phrases(DividendSettingsView.From(RiverInputs.Example).Facts).ShouldBe(
        [
            ("Settings_Earlier_None", "", InputOrigin.Example),
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

    // A nothing the user typed is theirs: the line no longer calls it an example.
    [Fact]
    public void Shares_earlierProfitsSetToNothing_statesNoneAsYours()
    {
        var inputs = RiverInputs.Example.WithEarlierProfits(0m);

        Phrases(DividendSettingsView.From(inputs).Facts)[0].ShouldBe(("Settings_Earlier_None", "", null));
    }

    // The kept amount's refusal is on the slider's field, in sight.
    [Theory]
    [InlineData(YearSimulation.EarlierProfitsNegative, true)]
    [InlineData(YearSimulation.ReserveNegative, false)]
    [InlineData(null, false)]
    public void SharesHold_refusal_opensTheDisclosureHoldingItsField(string? error, bool shares)
    {
        DividendSettingsView.SharesHold(error).ShouldBe(shares);
    }

    private static DirectorPaySettingsView DirectorPaySettings(RiverInputs inputs) => DirectorPaySettingsView.From(inputs, inputs.Run());

    private static CorporateTaxSettingsView CorporateTaxSettings(RiverInputs inputs) => CorporateTaxSettingsView.From(inputs, inputs.Run());

    private static List<(string Key, string Args, InputOrigin? Origin)> Phrases(IEnumerable<SettingPhrase> phrases) =>
        [.. phrases.Select(phrase => (phrase.Key, string.Join('|', phrase.Args), phrase.Origin))];
}
