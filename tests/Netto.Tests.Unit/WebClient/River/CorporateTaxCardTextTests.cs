using Netto.Core.CompanyCar;
using Netto.Core.CorporateTax;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Corporate tax card builds resource keys from the engine's conditions and figures. A key without a
// text in its .resx would show on screen as the key itself.
public class CorporateTaxCardTextTests
{
    // The name IStringLocalizer<CorporateTaxCard> reads River/CorporateTaxCard.resx under.
    private static readonly ResxText Resources = ResxText.For<CorporateTaxCard>();

    [Fact]
    public void CorporateTaxCardResx_everyReducedRateCondition_hasAText()
    {
        bool[] withoutAndWithBenefits = [false, true];
        var keys = Enum.GetValues<ReducedRateCondition>().SelectMany(condition => withoutAndWithBenefits.Select(benefits => CorporateTaxCardText.Condition(condition, benefits)));

        Resources.Missing(keys).ShouldBeEmpty();
    }

    // Every label and bubble of what the taxable income adds back: the vouchers, a fuel car's costs the
    // company cannot deduct and the share of its benefit, beside the taxable income.
    [Fact]
    public void CorporateTaxCardResx_everyLabelAndBubbleOfItsLines_hasAText()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers).WithEcoVouchers(250m)
            .AddToPlan(OptionalModule.Car).WithCatalogueValue(45_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(9_000m)
            .WithCarEngine(CarEngine.Petrol).WithCarCo2(120).WithCarAcquired(new DateOnly(2026, 1, 5));
        var lines = CorporateTaxFigures.Of(CorporateTaxView.From(inputs.Run().Year.ShouldNotBeNull()));

        Resources.Missing(lines.SelectMany(line => new[] { line.Key, line.Help }).OfType<string>()).ShouldBeEmpty();
    }

    // With benefits in kind, the conditions the pay decides say the benefits are counted in it; the
    // others read the same.
    [Theory]
    [InlineData(ReducedRateCondition.MinimumPayMet, "Condition_MinimumPayMet_WithBenefits")]
    [InlineData(ReducedRateCondition.TaxableIncomeNotAbovePay, "Condition_TaxableIncomeNotAbovePay_WithBenefits")]
    [InlineData(ReducedRateCondition.MinimumPayNotMet, "Condition_MinimumPayNotMet_WithBenefits")]
    [InlineData(ReducedRateCondition.FirstFourTaxPeriods, "Condition_FirstFourTaxPeriods_WithBenefits")]
    [InlineData(ReducedRateCondition.ExcessBenefits, "Condition_ExcessBenefits")]
    [InlineData(ReducedRateCondition.NotSmallCompany, "Condition_NotSmallCompany")]
    public void CorporateTaxCardText_conditionWithBenefits_saysTheyAreCounted(ReducedRateCondition condition, string key)
    {
        CorporateTaxCardText.Condition(condition, true).ShouldBe(key);
    }

    [Fact]
    public void CorporateTaxCardResx_everySurchargeState_hasAText()
    {
        Resources.Missing(Enum.GetValues<SurchargeState>().Select(CorporateTaxCardText.Surcharge)).ShouldBeEmpty();
    }

    // Every way the lines of Corporate tax's disclosures read: a small company or not, in each of its
    // financial years, advance payments or none, and a surcharge left or none.
    [Fact]
    public void CorporateTaxCardResx_everyPhraseOfItsSettingsLines_hasAText()
    {
        RiverInputs[] variations =
        [
            RiverInputs.Example.WithSmallCompany(false).WithAdvancePayment(0, 7_500m),
            .. Enum.GetValues<FinancialYear>().Select(RiverInputs.Example.WithFinancialYear),
        ];
        var phrases = variations.Select(inputs => CorporateTaxSettingsView.From(inputs, inputs.Run()))
            .SelectMany(view => view.Company.Concat(view.Payments));

        Resources.Missing(phrases.Select(phrase => phrase.Key)).ShouldBeEmpty();
    }

    [Fact]
    public void CorporateTaxCardResx_everyFinancialYear_hasAText()
    {
        Resources.Missing(Enum.GetValues<FinancialYear>().Select(CorporateTaxCardText.FinancialYear)).ShouldBeEmpty();
    }

    // Each sentence reads only the arguments the card passes it, its placeholders counted from 0:
    // a condition the minimum pay, the benefits and their share of it, the surcharge what more by the
    // last deadline cancels it and that deadline.
    [Theory]
    [InlineData("Condition_", 3)]
    [InlineData("Surcharge_", 2)]
    public void CorporateTaxCardResx_sentence_readsOnlyTheArgumentsTheCardPasses(string prefix, int arguments)
    {
        InterfaceTexts.Of(typeof(CorporateTaxCard)).Where(text => text.Key.StartsWith(prefix, StringComparison.Ordinal) && InterfaceTexts.Placeholders(text.Value).Any(index => index >= arguments))
            .Select(text => text.Key).ShouldBeEmpty();
    }
}
