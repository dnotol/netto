using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Steps;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Steps;

// How a step's amount reads under « where each figure comes from »: money that comes back (the refund on exempt
// dividends, the credit for low earned income) is an addition, as on the drain that carries it, even when it is
// nothing. The engine negates what comes back, so a refund or a credit of nothing is a negative zero, which is
// no less than zero and so still comes back.
public class StepDetailTests
{
    // 0m - 0.00m and decimal.Negate(0m) are what the engine hands for a refund of nothing.
    private static readonly decimal NegativeZero = decimal.Negate(0m);

    [Fact]
    public void ComesBack_aNegativeAmount_isTrue()
    {
        Detail(-12.50m).ComesBack.ShouldBeTrue();
    }

    [Fact]
    public void ComesBack_aNegativeZero_isTrue()
    {
        decimal.IsNegative(NegativeZero).ShouldBeTrue();

        Detail(NegativeZero).ComesBack.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12.50)]
    public void ComesBack_zeroOrMore_isFalse(double amount)
    {
        Detail((decimal)amount).ComesBack.ShouldBeFalse();
    }

    [Fact]
    public void AmountShown_aRefund_isAnAdditionOfIt()
    {
        Detail(-12.50m).AmountShown.ShouldBe("+€12.50");
    }

    [Fact]
    public void AmountShown_aRefundOfNothing_isAnAdditionOfNothing()
    {
        Detail(NegativeZero).AmountShown.ShouldBe("+€0.00");
    }

    [Theory]
    [InlineData(0, "€0.00")]
    [InlineData(12.50, "€12.50")]
    public void AmountShown_anAmountPaid_isTheAmount(double amount, string shown)
    {
        Detail((decimal)amount).AmountShown.ShouldBe(shown);
    }

    // The step as the card draws it, folded: a refund of nothing on exempt dividends reads as its drain does.
    [Fact]
    public async Task StepDetails_aRefundOfNothing_isDrawnAsAnAddition()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync<StepDetails>(new Folds(new BrowserStorage()), new() { ["Details"] = new List<StepDetail> { Detail(NegativeZero) } }));

        Regex.IsMatch(html, @"<span class=""step-details__amount""[^>]*>\+€0\.00</span>").ShouldBeTrue();
    }

    // A step cited for its rule alone, because another line states its amount or none is worth stating: its
    // heading is the rule's name with no amount beside it.
    [Fact]
    public async Task StepDetails_aStepWithoutAmount_isDrawnWithNoAmount()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync<StepDetails>(new Folds(new BrowserStorage()), new() { ["Details"] = new List<StepDetail> { Detail(12.50m) with { WithoutAmount = true } } }));

        html.ShouldNotContain("step-details__amount");
    }

    // The refund on exempt dividends in a year that pays none: the engine's step is a negative zero, and its
    // detail reads like the drain, an addition of nothing.
    [Fact]
    public void AmountShown_noDividendPaid_theExemptionRefundIsAnAdditionOfNothing()
    {
        var step = Fixtures.Run(Fixtures.Scenario(directorPay: 50_000m, reserve: 1_000_000m)).Steps.Single(each => each.RuleKey == "dividend_exemption_refund");

        (decimal.IsNegative(step.Amount), StepDetail.From(step).AmountShown).ShouldBe((true, "+€0.00"));
    }

    private static StepDetail Detail(decimal amount) => new("dividend_exemption_refund", amount, []);
}
