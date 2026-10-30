using Netto.Core.CompanyCar;
using Netto.Core.CorporateTax;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Corporate tax card builds resource keys from the engine's conditions and figures. A key without a
// text in its .resx would show on screen as the key itself.
public class CorporateTaxCardTextTests
{
    // The name IStringLocalizer<CorporateTaxCard> reads River/CorporateTaxCard.resx under.
    private static readonly ResxText Resources = ResxText.For<CorporateTaxCard>();

    private static readonly RiverInputs RoyaltyBeyondItsLimits = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);

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

    // Every sentence that tests the pay names the pay it tested, a term whose bubble says what that pay
    // holds: a condition the pay test decides, the benefits beyond their share of it, and the benefits held
    // within it in the first four tax periods. A company the rate is not open to, and the first four tax
    // periods without benefits, test no pay and name none.
    [Theory]
    [InlineData(ReducedRateCondition.MinimumPayMet, false, "tested pay")]
    [InlineData(ReducedRateCondition.MinimumPayMet, true, "tested pay")]
    [InlineData(ReducedRateCondition.TaxableIncomeNotAbovePay, false, "tested pay")]
    [InlineData(ReducedRateCondition.TaxableIncomeNotAbovePay, true, "tested pay")]
    [InlineData(ReducedRateCondition.MinimumPayNotMet, false, "tested pay")]
    [InlineData(ReducedRateCondition.MinimumPayNotMet, true, "tested pay")]
    [InlineData(ReducedRateCondition.ExcessBenefits, false, "tested pay")]
    [InlineData(ReducedRateCondition.ExcessBenefits, true, "tested pay")]
    [InlineData(ReducedRateCondition.FirstFourTaxPeriods, false, null)]
    [InlineData(ReducedRateCondition.FirstFourTaxPeriods, true, "tested pay")]
    [InlineData(ReducedRateCondition.NotSmallCompany, false, null)]
    [InlineData(ReducedRateCondition.NotSmallCompany, true, null)]
    public void CorporateTaxCardResx_condition_marksThePayTestedWhereItTestsOne(ReducedRateCondition condition, bool withBenefits, string? term)
    {
        Term(CorporateTaxCardText.Condition(condition, withBenefits)).ShouldBe(term);
    }

    // What the pay tested holds, in its bubble: Director pay's taxable pay with nothing left out while no
    // royalty's part is taxed as pay (DirectorPayFiguresTests' royalty of 30,000 passes its limits by
    // 6,000); beside one, what Director pay's taxable pay counts beyond the pay tested: that part, and, when
    // the company pays the contributions, the contributions and the fund's fee on it too.
    [Fact]
    public void TestedPayTerm_noRoyaltyTaxedAsPay_leavesNothingOut()
    {
        TestedPayTerm(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company)).ShouldBe(("Term_TestedPay", 0m));
    }

    [Fact]
    public void TestedPayTerm_royaltyTaxedAsPay_leavesItsPartOut()
    {
        TestedPayTerm(RoyaltyBeyondItsLimits).ShouldBe(("Term_TestedPay_Royalty", 6_000m));
    }

    [Fact]
    public void TestedPayTerm_royaltyTaxedAsPayAndCompanyPays_leavesOutItsPartWithTheContributionsOnIt()
    {
        var river = RoyaltyBeyondItsLimits.WithContributionsPaidBy(ContributionsPaidBy.Company);
        var pay = river.Run().Year.ShouldNotBeNull().DirectorPay;

        var (key, leftOut) = TestedPayTerm(river);

        (key, leftOut, leftOut > 6_000m).ShouldBe(("Term_TestedPay_RoyaltyCompanyPays", pay.TaxablePay - pay.TaxablePayWithoutRequalified, true));
    }

    [Fact]
    public void CorporateTaxCardResx_everyTestedPayTerm_hasAText()
    {
        Resources.Missing(["Term_TestedPay", "Term_TestedPay_Royalty", "Term_TestedPay_RoyaltyCompanyPays"]).ShouldBeEmpty();
    }

    // Each bubble beside a royalty names the amount left out and what it holds.
    [Theory]
    [InlineData("Term_TestedPay_Royalty", "royalty")]
    [InlineData("Term_TestedPay_RoyaltyCompanyPays", "royalty")]
    [InlineData("Term_TestedPay_RoyaltyCompanyPays", "contributions and fund fee the company pays on it")]
    public void CorporateTaxCardResx_testedPayTermBesideARoyalty_namesWhatItLeavesOut(string key, string words)
    {
        Resources.Format(key, "€6,000.00").ShouldSatisfyAllConditions(
            text => text.ShouldContain("€6,000.00"),
            text => text.ShouldContain(words));
    }

    // With no taxable profit, the sentence names the dividend the earlier profits still pay, while they pay one.
    [Theory]
    [InlineData(0, "NoTaxableProfit")]
    [InlineData(0.01, "NoTaxableProfit_EarlierProfits")]
    public void NoTaxableProfit_dividend_isNamedWhileThereIsOne(decimal dividend, string key)
    {
        (CorporateTaxCardText.NoTaxableProfit(dividend), Resources.Missing([key]).Count()).ShouldBe((key, 0));
    }

    [Fact]
    public void CorporateTaxCardResx_everySurchargeState_hasAText()
    {
        Resources.Missing(Enum.GetValues<SurchargeState>().Select(CorporateTaxCardText.Surcharge)).ShouldBeEmpty();
    }

    // What the advance payments leave is a sentence of the card's: while a surcharge is due and a deadline
    // is open, what more by that deadline cancels it; due with every deadline past, that none can; any
    // other state is why none is due, as before.
    [Fact]
    public void CorporateTaxCardText_surchargeDue_isWhatMoreByTheOpenDeadlineCancelsItOrThatEveryDeadlineIsPast()
    {
        var more = new MoreBy(new DateOnly(2026, 12, 21), 1m);

        (CorporateTaxCardText.SurchargeSentence(SurchargeState.Due, more), CorporateTaxCardText.SurchargeSentence(SurchargeState.Due, null))
            .ShouldBe(("Surcharge_Due", "Surcharge_TooLate"));
    }

    [Fact]
    public void CorporateTaxCardText_surchargeNotDue_isWhyNoneIsDue()
    {
        var states = Enum.GetValues<SurchargeState>().Where(state => state != SurchargeState.Due).ToList();

        states.Select(state => CorporateTaxCardText.SurchargeSentence(state, null)).ShouldBe(states.Select(CorporateTaxCardText.Surcharge));
    }

    [Fact]
    public void CorporateTaxCardResx_everySurchargeSentence_hasAText()
    {
        var more = new MoreBy(new DateOnly(2026, 12, 21), 1m);
        var sentences = Enum.GetValues<SurchargeState>()
            .SelectMany(state => new[] { CorporateTaxCardText.SurchargeSentence(state, null), CorporateTaxCardText.SurchargeSentence(state, more) });

        Resources.Missing(sentences).ShouldBeEmpty();
    }

    // The sentence of what more by a deadline cancels the surcharge takes its amount and that deadline.
    [Fact]
    public void CorporateTaxCardText_surchargeArguments_areTheAmountAndTheDeadline()
    {
        CorporateTaxCardText.SurchargeArguments(new MoreBy(new DateOnly(2026, 12, 21), 14_999.89m)).ShouldBe(["€14,999.89", "21 December 2026"]);
        CorporateTaxCardText.SurchargeArguments(null).ShouldBeEmpty();
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
    // a condition the minimum pay, the benefits and their share of it and the pay tested, the surcharge
    // what more by the first deadline still open cancels it and that deadline, a year with no taxable
    // profit the dividend, the pay tested's bubble the part of a royalty it leaves out.
    [Theory]
    [InlineData("Condition_", 4)]
    [InlineData("Surcharge_", 2)]
    [InlineData("NoTaxableProfit", 1)]
    [InlineData("Term_TestedPay", 1)]
    public void CorporateTaxCardResx_sentence_readsOnlyTheArgumentsTheCardPasses(string prefix, int arguments)
    {
        InterfaceTexts.Of(typeof(CorporateTaxCard)).Where(text => text.Key.StartsWith(prefix, StringComparison.Ordinal) && InterfaceTexts.Placeholders(text.Value).Any(index => index >= arguments))
            .Select(text => text.Key).ShouldBeEmpty();
    }

    private static (string Key, decimal LeftOut) TestedPayTerm(RiverInputs river) =>
        CorporateTaxCardText.TestedPayTerm(river.Run().Year.ShouldNotBeNull().DirectorPay);

    // The term a text marks, null where it marks none or is missing.
    private static string? Term(string key) => Resources.Get(key) is { } text ? TermText.Split(text).Term : null;
}
