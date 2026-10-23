using System.Globalization;
using System.Resources;
using Netto.Core.CorporateTax;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Corporate tax card builds resource keys from the engine's conditions and figures. A key without a
// text in its .resx would show on screen as the key itself.
public class CorporateTaxCardTextTests
{
    // The name IStringLocalizer<CorporateTaxCard> reads River/CorporateTaxCard.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.CorporateTaxCard", typeof(CorporateTaxCard).Assembly);

    [Fact]
    public void CorporateTaxCardResx_everyReducedRateCondition_hasAText()
    {
        var keys = Enum.GetValues<ReducedRateCondition>().SelectMany(condition => new[] { false, true }.Select(benefits => CorporateTaxCardText.Condition(condition, benefits)));

        Missing(keys).ShouldBeEmpty();
    }

    [Fact]
    public void CorporateTaxCardResx_everyAddBackSentence_hasAText()
    {
        Missing(["Disallowed", "Disallowed_Car", "Disallowed_VouchersAndCar", "Disallowed_CarCosts", "Disallowed_VouchersAndCarCosts"])
            .ShouldBeEmpty();
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
        Missing(Enum.GetValues<SurchargeState>().Select(CorporateTaxCardText.Surcharge)).ShouldBeEmpty();
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

        Missing(phrases.Select(phrase => phrase.Key)).ShouldBeEmpty();
    }

    [Fact]
    public void CorporateTaxCardResx_everyFinancialYear_hasAText()
    {
        Missing(Enum.GetValues<FinancialYear>().Select(CorporateTaxCardText.FinancialYear)).ShouldBeEmpty();
    }

    private static IEnumerable<string> Missing(IEnumerable<string> keys) =>
        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
