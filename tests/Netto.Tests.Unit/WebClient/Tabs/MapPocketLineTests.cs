using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What reaches the director runs into one line down their column, the pocket line: each card's line into the
// pocket joins it at its module's point, what leaves the pocket, the car's contribution, leaves it at the car's,
// and the line runs down the map's rows from point to point into the pocket, each stretch carrying what has
// reached the director so far, the sum of the lines above it less what left it: arithmetic of display, its last
// the pocket the bar says. That each line joining it is a line its card draws is MapCardsTests'.
public class MapPocketLineTests
{
    private static readonly MapEnd Pocket = MapEnd.InBucket("pocket");

    private static IReadOnlyList<MapLink> Links(RiverInputs river) => MapLinks.Of(river, river.Run());

    // Each line into the pocket joins the pocket line at its module's point, and what leaves the pocket leaves it
    // at the car's point, each as it was but for that end; no other line changes.
    [Fact]
    public void Of_everyLever_joinsEachLineIntoThePocketAtItsModulesPoint()
    {
        var links = Links(MapLinksTests.Full);

        var line = MapPocketLine.Of(links);

        line.ShouldSatisfyAllConditions(
            () => line.Take(links.Count).ShouldBe(links.Select(link => link.To == Pocket ? link with { To = MapEnd.OnPocketLine(link.From.Module!.Value) }
                : link.From == Pocket ? link with { From = MapEnd.OnPocketLine(link.To.Lever!.Value) } : link)),
            () => links.Select(link => link.From.Module).Where((_, index) => links[index].To == Pocket).Distinct().Count().ShouldBe(6),
            () => line.Take(links.Count).Where(link => link.From == Pocket || link.To == Pocket).ShouldBeEmpty(),
            () => line.ShouldContain(link => link.From == MapEnd.OnPocketLine(RiverModule.Car) && link.To == MapEnd.Beside(RiverModule.Car)));
    }

    // The line runs down the map's rows from the vouchers to the dividend, a point for each module whose money
    // joins or leaves it, into the pocket; each stretch carries what has reached the director once its upper
    // point's lines joined or left, money into the pocket, and is that module's.
    [Fact]
    public void Of_everyLever_runsDownTheRowsEachStretchWhatHasReachedYouSoFar()
    {
        var links = Links(MapLinksTests.Full);

        var stretches = Stretches(MapPocketLine.Of(links));

        var points = new[] { RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.Car, RiverModule.DirectorPay, RiverModule.Copyright, RiverModule.Dividend };
        var totals = points.Select((point, index) => points.Take(index + 1).Sum(above => MapBundle.Sum(links.Where(link => link.From.Module == above && link.To == Pocket))
            - MapBundle.Sum(links.Where(link => link.From == Pocket && link.To.Lever == above)))).ToList();
        stretches.ShouldSatisfyAllConditions(
            () => stretches.Select(link => (link.From, link.To)).ShouldBe(points.Select((point, index) =>
                (MapEnd.OnPocketLine(point), index + 1 < points.Length ? MapEnd.OnPocketLine(points[index + 1]) : Pocket))),
            () => stretches.Select(link => link.Amount).ShouldBe(totals.Cast<decimal?>()),
            () => stretches.Select(link => (link.Kind, link.Card, link.Carries.Text, link.Carries.Key)).ShouldBe(points.Select(point =>
                (MapLinkKind.IntoBucket, point, MapText.Map, MapTabText.PocketSoFar))));
    }

    // What has reached the director at the end of the line is the pocket the bar says.
    [Theory]
    [InlineData("every lever")]
    [InlineData("the example")]
    [InlineData("the company skipped")]
    [InlineData("a contribution beyond the car's costs")]
    [InlineData("the company paying the contributions")]
    public void Of_aPlan_endsOnWhatThePocketHolds(string name)
    {
        var river = MapRendering.River(name);

        var last = Stretches(MapPocketLine.Of(Links(river)))[^1];

        (last.To, last.Amount).ShouldBe((Pocket, BucketsView.From(river.Run())!.AmountOf("pocket")));
    }

    // A contribution beyond what has reached the director above the car leaves the line below nothing there.
    [Fact]
    public void Of_aContributionBeyondWhatReachedYouAboveIt_leavesTheLineBelowNothingAtTheCar()
    {
        var stretches = Stretches(MapPocketLine.Of(Links(MapRendering.River("a contribution beyond the car's costs"))));

        stretches.Single(link => link.From == MapEnd.OnPocketLine(RiverModule.Car)).Amount!.Value.ShouldBeLessThan(0m);
    }

    // The company skipped, the pay alone joins the line, which runs from Director pay into the pocket.
    [Fact]
    public void Of_theCompanySkipped_isDirectorPaysLineIntoIt()
    {
        var river = MapRendering.River("the company skipped");

        var line = MapPocketLine.Of(Links(river));

        line.Where(link => link.From.PocketLine is not null || link.To.PocketLine is not null).Select(link => (link.From.Id, link.To.Id))
            .ShouldBe([("director-pay", "pocket-line-director-pay"), ("pocket-line-director-pay", "pocket")]);
    }

    // No line into the pocket or out of it, a refused run among them, leaves the links as they were.
    [Fact]
    public void Of_noLineIntoThePocket_isTheLinksAsTheyWere()
    {
        IReadOnlyList<MapLink> links = [.. Links(MapLinksTests.Full).Where(link => link.From != Pocket && link.To != Pocket)];

        (MapPocketLine.Of(links), MapPocketLine.Of([])).ShouldBe((links, (IReadOnlyList<MapLink>)[]));
    }

    // A point of the pocket line is named by its module, beside the director's gates and Director pay alone,
    // whose money reaches the pocket.
    [Fact]
    public void OnPocketLine_aModuleWhoseMoneyReachesThePocket_isItsPoint()
    {
        var point = MapEnd.OnPocketLine(RiverModule.Rent);

        (point.PocketLine, point.Module, point.Bucket, point.Lever, point.Id, point == MapEnd.OnPocketLine(RiverModule.Rent))
            .ShouldBe((RiverModule.Rent, null, null, null, "pocket-line-rent", true));
    }

    [Theory]
    [InlineData(RiverModule.CompanyYear)]
    [InlineData(RiverModule.Split)]
    [InlineData(RiverModule.Household)]
    [InlineData(RiverModule.CorporateTax)]
    public void OnPocketLine_aModuleWhoseMoneyNeverReachesThePocket_isRefused(RiverModule module)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapEnd.OnPocketLine(module));
    }

    // A line joining the pocket line ends with no arrowhead, nor does a stretch of it, the line running on; what
    // leaves it into the company's line ends in one.
    [Fact]
    public void EndsInArrowhead_thePocketLine_onlyWhereMoneyLeavesItIntoTheCompanysLine()
    {
        var (car, pay) = (MapEnd.OnPocketLine(RiverModule.Car), MapEnd.OnPocketLine(RiverModule.DirectorPay));

        (MapBundle.EndsInArrowhead(MapEnd.Of(RiverModule.DirectorPay), pay), MapBundle.EndsInArrowhead(car, pay), MapBundle.EndsInArrowhead(pay, Pocket),
                MapBundle.EndsInArrowhead(car, MapEnd.Beside(RiverModule.Car)), MapBundle.EndsInArrowhead(MapEnd.Beside(RiverModule.Rent), MapEnd.Beside(RiverModule.Car)))
            .ShouldBe((false, false, false, true, false));
    }

    // A stretch of the pocket line touches the modules at its two points, as the company's line does the levers.
    [Fact]
    public void Touches_aStretchOfThePocketLine_isTheModulesAtItsTwoPoints()
    {
        var stretch = new MapLink(MapLinkKind.IntoBucket, MapEnd.OnPocketLine(RiverModule.Rent), MapEnd.OnPocketLine(RiverModule.Car), RiverModule.Rent,
            new MapLabel(MapText.Map, MapTabText.PocketSoFar, []), 1m);

        (stretch.Touches(RiverModule.Rent), stretch.Touches(RiverModule.Car), stretch.Touches(RiverModule.Vouchers)).ShouldBe((true, true, false));
    }

    private static List<MapLink> Stretches(IReadOnlyList<MapLink> links) =>
        [.. links.Where(link => link.From.PocketLine is not null && (link.To.PocketLine is not null || link.To == Pocket))];
}
