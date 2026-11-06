using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// A line of the map is drawn on a path through its points, each corner turned on an arc of 8 px on the map where
// it is drawn smallest, never more than half the shorter side beside it, nor under the arrowhead ending the line;
// the arc's radii are in percent of the map each way, so it is a circle there and stretches with the map. A halo
// cut short keeps the line's arcs. Here a percent is a pixel across and two down (MapScale.Of(100, 200)).
public class MapPathTests
{
    private static readonly MapScale Scale = MapScale.Of(100m, 200m);

    private static MapPoint P(decimal x, decimal y) => new(x, y);

    [Fact]
    public void Of_aStraightLine_isOneStretchWithNoArc()
    {
        MapPath.Of([P(10m, 10m), P(50m, 10m)], Scale).ShouldBe("M 10,10 L 50,10");
    }

    // Right, then down: a clockwise turn on the screen, the arc sweeping that way, 8 px each way from its corner.
    [Fact]
    public void Of_aCorner_turnsOnAnArcOfEightPixels()
    {
        MapPath.Of([P(10m, 10m), P(50m, 10m), P(50m, 40m)], Scale).ShouldBe("M 10,10 L 42,10 A 8 4 0 0 1 50,14 L 50,40");
    }

    [Fact]
    public void Of_aTurnTheOtherWay_sweepsTheOtherWay()
    {
        MapPath.Of([P(10m, 30m), P(50m, 30m), P(50m, 0m)], Scale).ShouldBe("M 10,30 L 42,30 A 8 4 0 0 0 50,26 L 50,0");
    }

    // A side of 10 px turns on 5 px, so two turns sharing it never overlap.
    [Fact]
    public void Of_aShortSide_turnsOnHalfItsLength()
    {
        MapPath.Of([P(10m, 10m), P(20m, 10m), P(20m, 40m)], Scale).ShouldBe("M 10,10 L 15,10 A 5 2.5 0 0 1 20,12.5 L 20,40");
    }

    [Fact]
    public void Of_twoTurnsSharingASide_eachTakesHalfOfItAtMost()
    {
        MapPath.Of([P(0m, 0m), P(40m, 0m), P(40m, 5m), P(80m, 5m)], Scale).ShouldBe("M 0,0 L 35,0 A 5 2.5 0 0 1 40,2.5 A 5 2.5 0 0 0 45,5 L 80,5");
    }

    // The last side, 20 px, ends in an arrowhead 15 px long: the arc keeps behind its base.
    [Fact]
    public void Of_aLastSideEndingInAnArrowhead_turnsBehindItsBase()
    {
        MapPath.Of([P(10m, 10m), P(50m, 10m), P(50m, 20m)], Scale, arrowLength: 15m).ShouldBe("M 10,10 L 45,10 A 5 2.5 0 0 1 50,12.5 L 50,20");
    }

    [Fact]
    public void Of_pointsOfNoLengthOrInLine_drawNoArc()
    {
        MapPath.Of([P(10m, 10m), P(10m, 10m), P(30m, 10m), P(50m, 10m)], Scale).ShouldBe("M 10,10 L 50,10");
    }

    [Fact]
    public void Of_noPoints_isNothing()
    {
        MapPath.Of([], Scale).ShouldBe("");
    }

    // A halo cut short at each end keeps the arcs its line turns on.
    [Fact]
    public void Of_cutShort_dropsThePixelsAtEachEndKeepingItsArcs()
    {
        MapPath.Of([P(10m, 10m), P(50m, 10m), P(50m, 40m)], Scale, trimStart: 5m, trimEnd: 20m).ShouldBe("M 15,10 L 42,10 A 8 4 0 0 1 50,14 L 50,30");
    }

    // Cut into an arc, it stops where the arc begins, so the arc is never cut.
    [Fact]
    public void Of_cutShortIntoAnArc_stopsWhereTheArcBegins()
    {
        MapPath.Of([P(10m, 10m), P(20m, 10m), P(20m, 40m)], Scale, trimStart: 8m).ShouldBe("M 15,10 A 5 2.5 0 0 1 20,12.5 L 20,40");
    }

    [Fact]
    public void Of_cutShortByAllItsLength_isNothing()
    {
        MapPath.Of([P(10m, 10m), P(20m, 10m)], Scale, trimEnd: 12m).ShouldBe("");
    }

    [Fact]
    public void Turns_aRoute_isEachCornerWithTheRadiusItTurnsOn()
    {
        MapPath.Turns([P(0m, 0m), P(40m, 0m), P(40m, 5m), P(80m, 5m)], Scale).ShouldBe([(P(40m, 0m), 5m), (P(40m, 5m), 5m)]);
    }
}
