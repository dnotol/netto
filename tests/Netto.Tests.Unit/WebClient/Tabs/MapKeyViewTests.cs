using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the key to the map's lines says of each style of line, in its order: money passed on, money into a
// bucket, what reaches the director, a tax base and a condition, each drawn as the map draws its lines, the
// very dash, ink and end (MapLinkStyle), what reaches the director in the map's one gold.
public class MapKeyViewTests
{
    [Fact]
    public void Lines_areTheMapsStylesOfLineInTheirOrder()
    {
        MapKeyView.Lines.ShouldBe([MapKeyLine.PassedOn, MapKeyLine.IntoBucket, MapKeyLine.Pocket, MapKeyLine.TaxBase, MapKeyLine.Condition]);
    }

    [Theory]
    [InlineData(MapKeyLine.PassedOn, MapLinkKind.PassedOn)]
    [InlineData(MapKeyLine.IntoBucket, MapLinkKind.IntoBucket)]
    [InlineData(MapKeyLine.Pocket, MapLinkKind.IntoBucket)]
    [InlineData(MapKeyLine.TaxBase, MapLinkKind.TaxBase)]
    [InlineData(MapKeyLine.Condition, MapLinkKind.Condition)]
    public void Style_eachLine_isItsKindsAsTheMapDrawsIt(MapKeyLine line, MapLinkKind kind)
    {
        (MapKeyView.Kind(line), MapKeyView.Dash(line), MapKeyView.Width(line), MapKeyView.End(line))
            .ShouldBe((kind, MapLinkStyle.Dash(kind), MapLinkStyle.Sample(kind), MapLinkStyle.SampleEnd(kind)));
    }

    // Every line is in its kind's ink but what reaches the director, in the map's one gold, as the pocket line
    // and the lines joining it are drawn.
    [Fact]
    public void Ink_eachLine_isItsKindsOrThePocketsGold()
    {
        MapKeyView.Lines.Select(MapKeyView.Ink).ShouldBe([
            MapLinkStyle.Ink(MapLinkKind.PassedOn), MapLinkStyle.Ink(MapLinkKind.IntoBucket), MapLinkStyle.PocketInk, MapLinkStyle.Ink(MapLinkKind.TaxBase),
            MapLinkStyle.Ink(MapLinkKind.Condition)]);
    }

    [Fact]
    public void Kind_aLineTheKeyHasNoStyleFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapKeyView.Kind((MapKeyLine)99));
    }
}
