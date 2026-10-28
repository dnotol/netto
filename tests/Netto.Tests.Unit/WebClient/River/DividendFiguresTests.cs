using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The dividend's figure, line by line: this year's profit after tax once profits kept from earlier years
// join it in what can be paid out, then the dividend paid out of them. The example's year leaves 40,000
// after tax; with 60,000 from earlier years and 30,000 kept, 70,000 is paid out (ModuleViewsTests).
public class DividendFiguresTests
{
    // The name IStringLocalizer<DividendCard> reads River/DividendCard.resx under.
    private static readonly ResxText Resources = ResxText.For<DividendCard>();

    private static readonly RiverInputs Prepaid = RiverInputs.Example.WithAdvancePayment(0, 7_500m).WithTaxWithheld(11_334m);

    [Fact]
    public void From_earlierProfits_isThisYearsProfitAfterTaxThenTheDividend()
    {
        Lines(Prepaid.WithEarlierProfits(60_000m).WithReserve(30_000m))
            .ShouldBe([(DividendFigures.ProfitAfterTax, 40_000m), (DividendFigures.MostPaidOut, 100_000m), (DividendFigures.Dividend, 70_000m)]);
    }

    // Once the company keeps a reserve, the most it can pay out is a line, and the dividend after it is
    // that less the reserve.
    [Fact]
    public void From_aReserve_isTheMostThatCanBePaidOutThenTheDividend()
    {
        Lines(Prepaid.WithReserve(15_000m)).ShouldBe([(DividendFigures.MostPaidOut, 40_000m), (DividendFigures.Dividend, 25_000m)]);
    }

    // No reserve, nothing the card would state of it: what can be paid out is the year's profit after tax,
    // which the summary says, and the dividend is a line.
    [Fact]
    public void From_noReserveNorEarlierProfits_isTheDividendAlone()
    {
        Lines(Prepaid).ShouldBe([(DividendFigures.Dividend, 40_000m)]);
    }

    // A reserve beyond what can be paid out leaves no dividend: no line of nothing, but the most that can
    // be paid out, which the reserve is not covered by.
    [Fact]
    public void From_aReserveBeyondWhatCanBePaidOut_isTheMostThatCanBePaidOutAlone()
    {
        Lines(Prepaid.WithReserve(50_000m)).ShouldBe([(DividendFigures.MostPaidOut, 40_000m)]);
    }

    // Everything the company has is kept to the cent: the reserve is covered, no dividend.
    [Fact]
    public void From_aReserveEqualToWhatCanBePaidOut_isTheMostThatCanBePaidOutAlone()
    {
        Lines(Prepaid.WithReserve(40_000m)).ShouldBe([(DividendFigures.MostPaidOut, 40_000m)]);
    }

    [Fact]
    public void DividendCardResx_everyLine_hasALabel()
    {
        new[] { DividendFigures.ProfitAfterTax, DividendFigures.MostPaidOut, DividendFigures.Dividend }
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    private static List<(string Key, decimal Amount)> Lines(RiverInputs inputs) =>
        [.. DividendFigures.From(DividendView.From(inputs.Run().Year.ShouldNotBeNull())).Select(line => (line.Key, line.Amount))];
}
