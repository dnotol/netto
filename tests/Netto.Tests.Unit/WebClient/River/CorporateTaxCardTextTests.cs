using System.Collections;
using System.Globalization;
using System.Resources;
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
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.CorporateTaxCard", typeof(CorporateTaxCard).Assembly);

    [Fact]
    public void CorporateTaxCardResx_everyReducedRateCondition_hasAText()
    {
        var keys = Enum.GetValues<ReducedRateCondition>().SelectMany(condition => new[] { false, true }.Select(benefits => CorporateTaxCardText.Condition(condition, benefits)));

        Missing(keys).ShouldBeEmpty();
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

        Missing(lines.SelectMany(line => new[] { line.Key, line.Help }).OfType<string>()).ShouldBeEmpty();
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

    // What a figure cannot say stays a sentence of fifteen words at most: which condition decided the
    // rate, that there is no taxable profit, and what the advance payments leave.
    [Fact]
    public void CorporateTaxCardResx_everySentenceItKeeps_isAtMostFifteenWords()
    {
        var kept = Texts().Where(text => text.Key.StartsWith("Condition_", StringComparison.Ordinal) || text.Key.StartsWith("Surcharge_", StringComparison.Ordinal)
            || text.Key == "NoTaxableProfit").ToList();

        kept.ShouldSatisfyAllConditions(
            () => kept.ShouldNotBeEmpty(),
            () => kept.Where(text => WordCount(text.Value) > 15).Select(text => text.Key).ShouldBeEmpty());
    }

    private static IEnumerable<string> Missing(IEnumerable<string> keys) =>
        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);

    // Each text of the card's .resx with its key.
    private static List<(string Key, string Value)> Texts() =>
    [
        .. Resources.GetResourceSet(CultureInfo.InvariantCulture, true, true).ShouldNotBeNull().Cast<DictionaryEntry>()
            .Select(entry => ((string)entry.Key, entry.Value as string ?? "")),
    ];

    // A word holds a letter or a digit, an amount or a placeholder counting as one.
    private static int WordCount(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(word => word.Any(char.IsLetterOrDigit));
}
