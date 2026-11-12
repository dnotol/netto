using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client;
using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.River;
using Netto.Web.Client.Steps;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.DirectorPay;

// The pay slip as a viewer reads it: each figure once under a name that means one thing, and no line of
// nothing, on a pay of 50,000 in income year 2026 with nothing paid in advance.
public class PaySlipRenderingTests
{
    private static readonly ResxText Texts = ResxText.For<PaySlip>();

    [Fact]
    public async Task PaySlip_balance_saysItsAmountOnceAndHoldsNoLineOfNothing()
    {
        var html = await SlipAsync(ContributionsPaidBy.Director);

        html.ShouldSatisfyAllConditions(
            () => Count(html, Texts.Get("Balance_ToPay")!).ShouldBe(1),
            () => Count(html, Texts.Get("AdvancePayments")!).ShouldBe(0),
            () => Regex.IsMatch(html, Regex.Escape(ResxText.For<StepDetails>().Get("Title_assessment_balance")!) + @"</span>\s*</span>").ShouldBeTrue(),
            () => html.ShouldNotContain("€0.00"),
            () => html.ShouldNotContain("−€0.00"));
    }

    // The statement has the taxable-pay column only when it can differ from the gross.
    [Theory]
    [InlineData(ContributionsPaidBy.Director, 0)]
    [InlineData(ContributionsPaidBy.Company, 12)]
    public async Task PaySlip_statement_showsTheTaxablePayColumnOnlyWhereItCanDifferFromTheGross(ContributionsPaidBy paidBy, int cells)
    {
        var html = await SlipAsync(paidBy);

        Count(html, $"data-label=\"{Texts.Get("ColumnTaxablePay")}\"").ShouldBe(cells);
    }

    private static int Count(string html, string text) => html.Split(text).Length - 1;

    private static async Task<string> SlipAsync(ContributionsPaidBy paidBy)
    {
        var (pay, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), new TaxPrepayments(null, AdvancePayments.None), paidBy);
        error.ShouldBeNull();

        return WebUtility.HtmlDecode(await RenderAsync<PaySlip>(new Folds(new BrowserStorage()), new() { ["Pay"] = pay }));
    }
}
