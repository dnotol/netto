using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

public class WhenToPayTabTextTests
{
    // The name IStringLocalizer<WhenToPayTab> reads Tabs/WhenToPayTab.resx under.
    private static readonly ResxText Resources = ResxText.For<WhenToPayTab>();

    private static readonly DateOnly On21December = new(2026, 12, 21);

    [Fact]
    public void WhenToPayTabResx_everyCompanyState_hasAText()
    {
        Enum.GetValues<SurchargeState>()
            .SelectMany(state => new[] { new CalendarTax<SurchargeState>(state, 1m, 1m, On21December), new CalendarTax<SurchargeState>(state, 1m, 0m, null) })
            .Select(WhenToPayTabText.Company)
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    [Fact]
    public void WhenToPayTabResx_everyDirectorState_hasAText()
    {
        Enum.GetValues<TaxSurchargeState>()
            .SelectMany(state => new[] { new CalendarTax<TaxSurchargeState>(state, 1m, 1m, On21December), new CalendarTax<TaxSurchargeState>(state, 1m, 0m, null) })
            .Select(WhenToPayTabText.Director)
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    // The tab passes the date, the company's amount, the director's and the income year, in that
    // order; no amount on either side is no row still open.
    [Theory]
    [InlineData(0, 0, "Nothing left to pay in advance in 2026")]
    [InlineData(15_000, 0, "21 December 2026 · €15,000.00 from the company")]
    [InlineData(0, 14_317.50, "21 December 2026 · €14,317.50 from you")]
    [InlineData(15_000, 14_317.50, "21 December 2026 · €15,000.00 from the company, €14,317.50 from you")]
    public void Next_nextRow_saysWhenAndWhoPaysWhat(double company, double director, string expected)
    {
        var next = company == 0 && director == 0 ? null : new CalendarRow(On21December, false, 0m, (decimal)company, 0m, (decimal)director);

        var text = Resources.Format(WhenToPayTabText.Next(next),
            next is null ? "" : Netto.Web.Client.Formats.Day(next.Date), Netto.Web.Client.Formats.Euros(next?.Company ?? 0m),
            Netto.Web.Client.Formats.Euros(next?.Director ?? 0m), 2026);

        text.ShouldBe(expected);
    }

    // The one classifier of who pays by the next deadline: the Next panel and the timeline's label both
    // call it, so they cannot name different payers.
    [Theory]
    [InlineData(true, true, "Both")]
    [InlineData(true, false, "Company")]
    [InlineData(false, true, "Director")]
    [InlineData(false, false, "Nothing")]
    public void Payers_whoPaysByTheNextDeadline_isOneOfFour(bool company, bool director, string expected)
    {
        WhenToPayTabText.Payers(company, director).ToString().ShouldBe(expected);
    }

    // A row with an amount on a side has that payer, whatever the other pays.
    [Theory]
    [InlineData(15_000, 0, "Next_Company")]
    [InlineData(0, 14_317.50, "Next_Director")]
    [InlineData(15_000, 14_317.50, "Next_Both")]
    public void Next_row_isNamedByTheSidesThatPay(double company, double director, string expected)
    {
        WhenToPayTabText.Next(new CalendarRow(On21December, false, 0m, (decimal)company, 0m, (decimal)director)).ShouldBe(expected);
    }

    [Fact]
    public void Next_noRow_isNothing()
    {
        WhenToPayTabText.Next(null).ShouldBe("Next_Nothing");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(0)]
    public void WhenToPayTabResx_everyBalance_hasAText(int balance)
    {
        Resources.Get(WhenToPayTabText.CompanyBalance(balance)).ShouldNotBeNull();
        Resources.Get(WhenToPayTabText.DirectorBalance(balance)).ShouldNotBeNull();
        Resources.Get(WhenToPayTabText.Regularisation(balance)).ShouldNotBeNull();
    }

    [Fact]
    public void DirectorBalance_toPay_saysTheTaxYearAndTheAmount()
    {
        var text = Resources.Format(WhenToPayTabText.DirectorBalance(1_436.06m), 2027, "€1,436.06");

        text.ShouldBe("On your assessment for tax year 2027: €1,436.06 to pay, on the day the administration sets.");
    }

    [Fact]
    public void Company_dueWithADeadlineOpen_saysWhatMoreCancelsItByWhen()
    {
        var text = Resources.Format(WhenToPayTabText.Company(new CalendarTax<SurchargeState>(SurchargeState.Due, 675m, 15_000m, On21December)),
            "€675.00", "€15,000.00", "21 December 2026", 2026);

        text.ShouldBe("The company's payments leave a surcharge of €675.00 on its corporate tax: €15,000.00 more by 21 December 2026 cancels it.");
    }

    // The pay's line for a run of its months, here 4,166.67 gross and 882.47 withheld: every month while
    // the year pays alike, else the run's months, or its month alone.
    [Theory]
    [InlineData(1, 12, 1, "Each month: €4,166.67 gross on your payslip, €882.47 withheld by the company, €3,284.20 net paid to you.")]
    [InlineData(11, 12, 2, "Each month from November to December: €4,166.67 gross on your payslip, €882.47 withheld by the company, €3,284.20 net paid to you.")]
    [InlineData(12, 12, 2, "In December: €4,166.67 gross on your payslip, €882.47 withheld by the company, €3,284.20 net paid to you.")]
    public void PayLine_aRunOfThePaysMonths_saysWhatEachOfThemPays(int firstMonth, int lastMonth, int runs, string expected)
    {
        var run = new PayRun(new DateOnly(2026, firstMonth, 1), new DateOnly(2026, lastMonth, 1), 4_166.67m, 882.47m, 3_284.20m);

        var text = Resources.Format(WhenToPayTabText.PayLine(run, runs), Formats.Euros(run.GrossPay), Formats.Euros(run.Withholding), Formats.Euros(run.NetPaid),
            Formats.Month(2026, firstMonth), Formats.Month(2026, lastMonth));

        text.ShouldBe(expected);
    }
}
