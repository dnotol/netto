using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// How each kind of link is drawn, told apart at a glance and with no colour needed: money passed on solid
// and heavy, money into a bucket solid and light, what only changes a tax base dotted, a condition dashed.
// The map's lines and the filter's swatches read the one style.
public class MapLinkStyleTests
{
    [Fact]
    public void Style_noTwoKinds_shareOne()
    {
        Enum.GetValues<MapLinkKind>().Select(kind => (MapLinkStyle.Dash(kind), MapLinkStyle.Width(kind))).Distinct().Count()
            .ShouldBe(Enum.GetValues<MapLinkKind>().Length);
    }

    [Fact]
    public void Style_money_isSolidAndHeavierPassedOnThanIntoABucket()
    {
        (MapLinkStyle.Dash(MapLinkKind.PassedOn), MapLinkStyle.Dash(MapLinkKind.IntoBucket),
                decimal.Parse(MapLinkStyle.Width(MapLinkKind.PassedOn), System.Globalization.CultureInfo.InvariantCulture)
                > decimal.Parse(MapLinkStyle.Width(MapLinkKind.IntoBucket), System.Globalization.CultureInfo.InvariantCulture))
            .ShouldBe((null, null, true));
    }

    [Fact]
    public void Style_aTaxBaseAndACondition_areBrokenLinesOfTheirOwn()
    {
        (MapLinkStyle.Dash(MapLinkKind.TaxBase), MapLinkStyle.Dash(MapLinkKind.Condition)).ShouldBe(("1 4", "7 5"));
    }

    [Fact]
    public void Style_aKindItHasNoStyleFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapLinkStyle.Dash((MapLinkKind)99));
    }
}
