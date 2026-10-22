using System.Globalization;
using System.Resources;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

public class CalendarCardTextTests
{
    // The name IStringLocalizer<CalendarCard> reads River/CalendarCard.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.CalendarCard", typeof(CalendarCard).Assembly);

    private static readonly DateOnly On21December = new(2026, 12, 21);

    private static string? Text(string key) => Resources.GetString(key, CultureInfo.InvariantCulture);

    [Fact]
    public void CalendarCardResx_everyCompanyState_hasAText()
    {
        Enum.GetValues<SurchargeState>()
            .SelectMany(state => new[] { new CalendarTax<SurchargeState>(state, 1m, 1m, On21December), new CalendarTax<SurchargeState>(state, 1m, 0m, null) })
            .Select(CalendarCardText.Company)
            .Where(key => Text(key) is null)
            .ShouldBeEmpty();
    }

    [Fact]
    public void CalendarCardResx_everyDirectorState_hasAText()
    {
        Enum.GetValues<TaxSurchargeState>()
            .SelectMany(state => new[] { new CalendarTax<TaxSurchargeState>(state, 1m, 1m, On21December), new CalendarTax<TaxSurchargeState>(state, 1m, 0m, null) })
            .Select(CalendarCardText.Director)
            .Where(key => Text(key) is null)
            .ShouldBeEmpty();
    }

    // The card passes the date, the company's amount, the director's and the income year, in that
    // order; no amount on either side is no row still open.
    [Theory]
    [InlineData(0, 0, "Nothing left to pay in advance in 2026")]
    [InlineData(15_000, 0, "Next by 21 December 2026: €15,000.00 from the company")]
    [InlineData(0, 14_317.50, "Next by 21 December 2026: €14,317.50 from you")]
    [InlineData(15_000, 14_317.50, "Next by 21 December 2026: €15,000.00 from the company, €14,317.50 from you")]
    public void Summary_nextRow_saysWhoPaysWhatByWhen(double company, double director, string expected)
    {
        var next = company == 0 && director == 0 ? null : new CalendarRow(On21December, false, 0m, (decimal)company, 0m, (decimal)director);

        var text = string.Format(CultureInfo.InvariantCulture, Text(CalendarCardText.Summary(next)) ?? "",
            next is null ? "" : Netto.Web.Client.Formats.Day(next.Date), Netto.Web.Client.Formats.Euros(next?.Company ?? 0m),
            Netto.Web.Client.Formats.Euros(next?.Director ?? 0m), 2026);

        text.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(0)]
    public void CalendarCardResx_everyBalance_hasAText(int balance)
    {
        Text(CalendarCardText.CompanyBalance(balance)).ShouldNotBeNull();
        Text(CalendarCardText.DirectorBalance(balance)).ShouldNotBeNull();
    }

    [Fact]
    public void DirectorBalance_toPay_saysTheTaxYearAndTheAmount()
    {
        var text = string.Format(CultureInfo.InvariantCulture, Text(CalendarCardText.DirectorBalance(1_436.06m)) ?? "", 2027, "€1,436.06");

        text.ShouldBe("On your assessment for tax year 2027: €1,436.06 to pay, on the day the administration sets.");
    }

    [Fact]
    public void Company_dueWithADeadlineOpen_saysWhatMoreCancelsItByWhen()
    {
        var text = string.Format(CultureInfo.InvariantCulture, Text(CalendarCardText.Company(new CalendarTax<SurchargeState>(SurchargeState.Due, 675m, 15_000m, On21December))) ?? "",
            "€675.00", "€15,000.00", "21 December 2026", 2026);

        text.ShouldBe("The company's payments leave a surcharge of €675.00 on its corporate tax: €15,000.00 more by 21 December 2026 cancels it.");
    }
}
