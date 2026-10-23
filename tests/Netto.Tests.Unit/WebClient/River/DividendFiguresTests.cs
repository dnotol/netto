using System.Globalization;
using System.Resources;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The dividend's figure, line by line: this year's profit after tax once profits kept from earlier years
// join it in what can be paid out, then the dividend paid out of them. The example's year leaves 40,000
// after tax; with 60,000 from earlier years and 30,000 kept, 70,000 is paid out (ModuleViewsTests).
public class DividendFiguresTests
{
    // The name IStringLocalizer<DividendCard> reads River/DividendCard.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.DividendCard", typeof(DividendCard).Assembly);

    private static readonly RiverInputs Prepaid = RiverInputs.Example.WithAdvancePayment(0, 7_500m).WithTaxWithheld(11_334m);

    [Fact]
    public void From_earlierProfits_isThisYearsProfitAfterTaxThenTheDividend()
    {
        Lines(Prepaid.WithEarlierProfits(60_000m).WithReserve(30_000m))
            .ShouldBe([(DividendFigures.ProfitAfterTax, 40_000m), (DividendFigures.Dividend, 70_000m)]);
    }

    // Without earlier profits, what can be paid out is the year's profit after tax, which the summary
    // says: the dividend paid out of it alone is a line.
    [Fact]
    public void From_noEarlierProfits_isTheDividendAlone()
    {
        Lines(Prepaid.WithReserve(15_000m)).ShouldBe([(DividendFigures.Dividend, 25_000m)]);
    }

    // All of it kept, no dividend: no line of nothing.
    [Fact]
    public void From_allKept_isNoLine()
    {
        Lines(Prepaid.WithReserve(50_000m)).ShouldBeEmpty();
    }

    [Fact]
    public void DividendCardResx_everyLine_hasALabel()
    {
        new[] { DividendFigures.ProfitAfterTax, DividendFigures.Dividend }
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    private static List<(string Key, decimal Amount)> Lines(RiverInputs inputs) =>
        [.. DividendFigures.From(DividendView.From(inputs.Run().Year.ShouldNotBeNull())).Select(line => (line.Key, line.Amount))];
}
