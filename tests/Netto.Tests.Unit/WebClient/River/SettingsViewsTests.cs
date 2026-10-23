using Netto.Core.DirectorPay;
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

    private static DirectorPaySettingsView DirectorPaySettings(RiverInputs inputs) => DirectorPaySettingsView.From(inputs, inputs.Run());

    private static CorporateTaxSettingsView CorporateTaxSettings(RiverInputs inputs) => CorporateTaxSettingsView.From(inputs, inputs.Run());

    private static List<(string Key, string Args, InputOrigin? Origin)> Phrases(IEnumerable<SettingPhrase> phrases) =>
        [.. phrases.Select(phrase => (phrase.Key, string.Join('|', phrase.Args), phrase.Origin))];
}
