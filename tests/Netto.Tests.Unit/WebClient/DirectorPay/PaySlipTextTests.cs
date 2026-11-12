using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client;
using Netto.Web.Client.DirectorPay;
using Shouldly;

namespace Netto.Tests.WebClient.DirectorPay;

// PaySlip.razor builds resource keys from the engine's keys. A key without a text in PaySlip.resx
// would show on screen as the key itself, so every key the engine returns for 2026 must have one,
// with a municipality's rate and with a rate the user entered, the contributions paid by the
// director or by the company, and the withholding derived or given.
public class PaySlipTextTests
{
    // The name IStringLocalizer<PaySlip> reads DirectorPay/PaySlip.resx under.
    private static readonly ResxText Resources = ResxText.For<PaySlip>();

    // The pay slip's stylesheet as rules, read through the tests' one reader.
    private static IReadOnlyList<(string Selector, string Declarations)> PaySlipRules =>
        Stylesheet.Rules(Stylesheet.Read("src", "Netto.Web.Client", "DirectorPay", "PaySlip.razor.css"));

    // The pay's income before the marital quotient is its net professional income, the Household card's own
    // word for it: « taxable income » is left to the income on the scale, after the quotient, which another
    // value holds there.
    [Fact]
    public void Row_taxableIncome_isTheNetProfessionalIncomeInBothLanguages()
    {
        (Resources.Get("Row_taxable_income"), ResxText.French<PaySlip>().Get("Row_taxable_income"))
            .ShouldBe(("Net professional income", "Revenu professionnel net"));
    }

    // A month's amount after the withholding alone is not called a net: the year's net, after the contributions
    // and the final tax, is the card's own « net pay », which the statement's hint points to.
    [Fact]
    public void ColumnNetPaid_isWhatIsPaidAfterWithholdingNotANet()
    {
        var french = ResxText.French<PaySlip>();

        (Resources.Get("ColumnNetPaid"), french.Get("ColumnNetPaid"), Resources.Get("StatementHint")?.Contains("Net pay"), french.Get("StatementHint")?.Contains("Rémunération nette"))
            .ShouldBe(("Paid after withholding", "Versé après précompte", true, true));
    }

    private static readonly Dictionary<string, RuleAmount> RatesByOrigin = new()
    {
        ["municipality"] = MunicipalRates.IncomeYear2026.Municipalities[0].Rate,
        ["entered"] = MunicipalRates.IncomeYear2026.Entered(0.07m),
    };

    private static readonly Dictionary<string, TaxPrepayments> WithholdingsByOrigin = new()
    {
        ["derived"] = TaxPrepayments.AsWithheld,
        ["given"] = new TaxPrepayments(10_000m, AdvancePayments.None),
    };

    public static TheoryData<string, ContributionsPaidBy, string> Cases =>
    [
        .. from rateOrigin in RatesByOrigin.Keys
           from paidBy in Enum.GetValues<ContributionsPaidBy>()
           from withholding in WithholdingsByOrigin.Keys
           select (rateOrigin, paidBy, withholding),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void PaySlipResx_everyKeyItBuildsFromThe2026Result_hasAText(string rateOrigin, ContributionsPaidBy paidBy, string withholding)
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, RatesByOrigin[rateOrigin], WithholdingsByOrigin[withholding], paidBy);
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        var keys = view.Tracks.Select(track => PaySlipText.Track(track.Key))
            .Concat(view.Tracks.Select(track => PaySlipText.TrackHint(track.Key)))
            .Concat(view.Tracks.SelectMany(track => track.Rows).Select(row => PaySlipText.Row(row.Key)))
            .Concat(view.NextThousand.Select(part => PaySlipText.Row(part.Key)))
            .Concat(view.Withholding.Select(period => PaySlipText.WithholdingMonths(period.FirstMonth, period.LastMonth)))
            .Append(PaySlipText.Withheld(view.Balance.WithheldGiven))
            .Append(PaySlipText.Balance(view.Balance.Kind));

        keys.Distinct().Where(key => Resources.Get(key) is null).ShouldBeEmpty();
    }

    // A pay with a car's benefit adds its row to the tax base and its column to the statement.
    [Fact]
    public void PaySlipResx_everyKeyOfAPayWithABenefit_hasAText()
    {
        var (value, _) = GrossToNet.Compute(
            2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.AsWithheld, ContributionsPaidBy.Director, new TaxedWithPay { BenefitInKind = 2_400m });
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        view.Tracks.SelectMany(track => track.Rows).Select(row => PaySlipText.Row(row.Key)).Append("ColumnBenefit").Append("AddedNote")
            .Distinct().Where(key => Resources.Get(key) is null).ShouldBeEmpty();
    }

    // The fund's fee adds its row to the track and its part to the next 1,000, whoever pays it.
    [Theory]
    [InlineData(ContributionsPaidBy.Director)]
    [InlineData(ContributionsPaidBy.Company)]
    public void PaySlipResx_everyKeyOfAPayWithTheFundsFee_hasAText(ContributionsPaidBy paidBy)
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.AsWithheld, paidBy, fundFeeRate: 0.0305m);
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        var keys = view.Tracks.SelectMany(track => track.Rows).Select(row => PaySlipText.Row(row.Key))
            .Concat(view.NextThousand.Select(part => PaySlipText.Row(part.Key)))
            .ToList();

        keys.ShouldSatisfyAllConditions(
            () => keys.ShouldContain(PaySlipText.Row("fund_fee")),
            () => keys.Distinct().Where(key => Resources.Get(key) is null).ShouldBeEmpty());
    }

    // Children add the credit for children's part to the next 1,000.
    [Fact]
    public void PaySlipResx_everyKeyOfAPayWithChildren_hasAText()
    {
        var household = Household.None with { Dependants = new Dependants(Children: 2, ChildrenUnder3: 1, SingleParent: true) };
        var (value, _) = GrossToNet.Compute(2026, 24_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.AsWithheld, household: household);
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        var keys = view.Tracks.SelectMany(track => track.Rows).Select(row => PaySlipText.Row(row.Key))
            .Concat(view.NextThousand.Select(part => PaySlipText.Row(part.Key)))
            .ToList();

        keys.ShouldSatisfyAllConditions(
            () => keys.ShouldContain(PaySlipText.Row("child_tax_credit_lost")),
            () => keys.Distinct().Where(key => Resources.Get(key) is null).ShouldBeEmpty());
    }

    // The credit for children the next 1,000 takes away is drawn as the credit for low earned income's is:
    // money that came back after the tax, its segment and swatch dotted, or both would be drawn empty.
    [Fact]
    public void PaySlipCss_creditForChildrenTheNextThousandTakesAway_isDrawnAsTheOtherCreditLost()
    {
        var (selector, _) = PaySlipRules.Single(rule => rule.Selector.Contains(".next__segment--low_earned_income_credit_lost", StringComparison.Ordinal));

        selector.ShouldSatisfyAllConditions(
            () => selector.ShouldContain(".next__segment--child_tax_credit_lost"),
            () => selector.ShouldContain(".next__part--child_tax_credit_lost .next__swatch"));
    }

    // When the company pays, the note under the next 1,000 names what it adds on top of the 1,000: the
    // contributions, and the fund's fee once its part is drawn.
    [Theory]
    [InlineData(false, "NextCompanyPaysNote")]
    [InlineData(true, "NextCompanyPaysNote_WithFee")]
    public void NextCompanyPaysNote_withOrWithoutTheFee_isTheNoteThatNamesWhatIsOnTop(bool withFee, string expected)
    {
        var key = PaySlipText.NextCompanyPaysNote(withFee);

        (key, Resources.Get(key) is not null).ShouldBe((expected, true));
    }

    // The fee's part of the next 1,000 rides on the contributions: its bar segment and its legend's swatch
    // take their colour, through the site's token, or both would be drawn empty.
    [Fact]
    public void PaySlipCss_fundFeesPartOfTheNextThousand_isColouredAsTheContributions()
    {
        var (selector, declarations) = PaySlipRules.Single(rule => rule.Selector.Contains(".next__segment--social_contributions", StringComparison.Ordinal));

        selector.ShouldSatisfyAllConditions(
            () => selector.ShouldContain(".next__segment--fund_fee"),
            () => selector.ShouldContain(".next__part--fund_fee .next__swatch"),
            () => declarations.ShouldContain("var(--netto-data-high)"));
    }

    // A royalty beyond its limits adds its row to the track and says what the next 1,000 holds; one
    // within them adds its lines to the balance.
    [Fact]
    public void PaySlipResx_everyKeyOfAPayWithARoyalty_hasAText()
    {
        var movable = new Netto.Core.MovableIncome.MovableIncomePaid(
            30_000m, new IncomeTaxedApart(24_000m, Netto.Core.Copyright.CopyrightRules.IncomeYear2026.TaxedApartRate), 6_000m, 5_400m);
        var (value, _) = GrossToNet.Compute(
            2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.AsWithheld, ContributionsPaidBy.Director, new TaxedWithPay { Movable = movable });
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        view.Tracks.SelectMany(track => track.Rows).Select(row => PaySlipText.Row(row.Key))
            .Concat([PaySlipText.FinalTax(false, true), PaySlipText.FinalTax(true, true), "FinalTaxOnRoyalty", "WithheldOnRoyalty", "NextRoyaltyNote"])
            .Distinct().Where(key => Resources.Get(key) is null).ShouldBeEmpty();
    }

    // A royalty that follows the pay rises with the next 1,000, which holds it as it is: the note says so.
    [Fact]
    public void PaySlipResx_noteOnARoyaltyThatFollowsThePay_hasAText()
    {
        Resources.Get("NextFollowingRoyaltyNote").ShouldNotBeNull();
    }

    [Theory]
    [InlineData(false, false, "FinalTax")]
    [InlineData(true, false, "FinalTax_WithRent")]
    [InlineData(false, true, "FinalTax_WithRoyalty")]
    [InlineData(true, true, "FinalTax_WithRentAndRoyalty")]
    public void FinalTax_saysWhatItIsOn(bool withRent, bool withRoyalty, string key)
    {
        PaySlipText.FinalTax(withRent, withRoyalty).ShouldBe(key);
    }

    // With a rent taxed with the pay, the final tax and the next 1,000 say so.
    [Fact]
    public void PaySlipResx_everyKeyOfARentTaxedWithThePay_hasAText()
    {
        string[] keys = [PaySlipText.FinalTax(false), PaySlipText.FinalTax(true), "FinalTaxOnRent", "NextRentNote"];

        keys.Where(key => Resources.Get(key) is null).ShouldBeEmpty();
    }

    // Above each period's withholding, the months it covers: in 2026 January to October, then November
    // and December by the rules from 1 November.
    [Theory]
    [InlineData(1, 10, "Withheld from January to October")]
    [InlineData(11, 12, "Withheld from November to December")]
    [InlineData(12, 12, "Withheld in December")]
    public void WithholdingMonths_aPeriodsMonths_nameThem(int firstMonth, int lastMonth, string expected)
    {
        Resources.Format(PaySlipText.WithholdingMonths(firstMonth, lastMonth), Formats.Month(2026, firstMonth), Formats.Month(2026, lastMonth)).ShouldBe(expected);
    }

    [Fact]
    public void PaySlipResx_everyWayTheBalanceCanGo_hasAText()
    {
        Enum.GetValues<BalanceKind>().Select(PaySlipText.Balance)
            .Where(key => Resources.Get(key) is null).ShouldBeEmpty();
    }
}
