using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// How thick the map draws a line: a line of money as thick as its amount, on the square root of its share
// of the run's largest line of money, from the thinnest, which a line of a few hundred euros beside the
// year's profit still shows, to the thickest, the largest line's; a line that carries no money its kind's
// width, whatever its amount.
public class MapLineWidthsTests
{
    [Fact]
    public void Money_runsFromOneAndAHalfToEightPixels()
    {
        (MapLineWidths.Thinnest, MapLineWidths.Thickest).ShouldBe((1.5m, 8m));
    }

    [Fact]
    public void Money_theLargestLine_isTheThickest()
    {
        MapLineWidths.Money(100_000m, 100_000m).ShouldBe(MapLineWidths.Thickest);
    }

    // €350.00 of vouchers beside €100,000.00 of profit: the square root of its share, 0.059, of the way from
    // the thinnest to the thickest, to the hundredth of a pixel.
    [Fact]
    public void Money_aFewHundredEurosBesideTheYearsProfit_isDrawnAboveTheThinnest()
    {
        MapLineWidths.Money(350m, 100_000m).ShouldBe(1.88m);
    }

    // The square root: a quarter of the largest is half the way from the thinnest to the thickest.
    [Fact]
    public void Money_aQuarterOfTheLargest_isHalfwayFromTheThinnestToTheThickest()
    {
        MapLineWidths.Money(25_000m, 100_000m).ShouldBe((MapLineWidths.Thinnest + MapLineWidths.Thickest) / 2m);
    }

    // A line is as thick as its amount's size, whichever way the amount is signed.
    [Fact]
    public void Money_anAmountBelowNothing_isAsThickAsItsSize()
    {
        MapLineWidths.Money(-25_000m, 100_000m).ShouldBe(MapLineWidths.Money(25_000m, 100_000m));
    }

    // A map with no line of money has no largest line to measure by: nothing is divided by it.
    [Fact]
    public void Money_noLineOfMoneyOnTheMap_isTheThinnest()
    {
        MapLineWidths.Money(0m, 0m).ShouldBe(MapLineWidths.Thinnest);
    }

    [Theory]
    [InlineData(MapLinkKind.PassedOn)]
    [InlineData(MapLinkKind.IntoBucket)]
    public void Of_aLineOfMoney_isAsThickAsItsAmount(MapLinkKind kind)
    {
        MapLineWidths.Of(kind, 25_000m, 100_000m).ShouldBe(MapLineWidths.Money(25_000m, 100_000m));
    }

    // A tax base and a condition move no money: their lines keep their kind's width, a tax base's 2 px and a
    // condition's 1.75 px, whatever amount they name.
    [Theory]
    [InlineData(MapLinkKind.TaxBase, 2)]
    [InlineData(MapLinkKind.Condition, 1.75)]
    public void Of_aLineThatCarriesNoMoney_keepsItsKindsWidthWhateverItsAmount(MapLinkKind kind, double width)
    {
        (MapLineWidths.Of(kind, 1m, 100_000m), MapLineWidths.Of(kind, 100_000m, 100_000m)).ShouldBe(((decimal)width, (decimal)width));
    }
}
