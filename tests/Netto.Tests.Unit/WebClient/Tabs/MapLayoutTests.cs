using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Where the map draws its modules, its buckets and its lines: fixed slots in columns by whose money each
// module moves, rows in the river's order closing up over a lever out of the plan, the levers that move no
// money of their own in one quiet pill, and an orthogonal route for every link, the conditions one module
// sets on another among them, that crosses no box, no quiet pill and no pill of the pay's but the pay itself,
// ends on its two boxes and shares no stretch with another link's, whatever the plan holds and whichever of
// its levers it leaves empty; and at the widths its lines are drawn, apart from one another, off the boxes and
// within the sides they meet, on the map at 1,201 px, where it is narrowest, and at 1,920 px.
public class MapLayoutTests
{
    // The conditions one module sets on another, as the engine will name them: the pay keeping the
    // reduced rate, a car's benefit within 20% of the pay, a rent leaving no office allowance, a royalty
    // lowering the pay that keeps the rate, and the reserve the dividend keeps.
    private static readonly (RiverModule From, RiverModule To)[] Conditions =
    [
        (RiverModule.Split, RiverModule.CorporateTax),
        (RiverModule.Car, RiverModule.CorporateTax),
        (RiverModule.Allowances, RiverModule.Rent),
        (RiverModule.Copyright, RiverModule.Split),
        (RiverModule.Dividend, RiverModule.Split),
    ];

    // Every module and bucket a card's line can join: a line runs either way between them, money into the
    // bucket or, a refund, a credit, a contribution or a bonus beyond its surcharge, back out of it.
    private static readonly (MapEnd End, string Bucket)[] DrainEnds =
    [
        (MapEnd.Of(RiverModule.CompanyYear), "expenses"),
        (MapEnd.Of(RiverModule.Vouchers), "pocket"),
        (MapEnd.Of(RiverModule.Allowances), "pocket"),
        (MapEnd.Of(RiverModule.Rent), "taxes_on_pay"),
        (MapEnd.Of(RiverModule.Rent), "pocket"),
        (MapEnd.Beside(RiverModule.Car), "pocket"),
        (MapEnd.Beside(RiverModule.Car), "expenses"),
        (MapEnd.Of(RiverModule.Copyright), "taxes_on_pay"),
        (MapEnd.Of(RiverModule.Copyright), "pocket"),
        (MapEnd.Of(RiverModule.DirectorPay), "taxes_on_pay"),
        (MapEnd.Of(RiverModule.DirectorPay), "pocket"),
        (MapEnd.Of(RiverModule.CorporateTax), "taxes_on_profit"),
        (MapEnd.Of(RiverModule.Dividend), "taxes_on_profit"),
        (MapEnd.Of(RiverModule.Dividend), "pocket"),
        (MapEnd.Of(RiverModule.Dividend), "kept_in_company"),
    ];

    public static TheoryData<int> EveryDrainEnds => new(Enumerable.Range(0, DrainEnds.Length));

    // The most badges a module's box can hold, which the routes here are worked out with, so its box is the
    // tallest the map draws: a badge for each condition acting on it (Conditions, the office rule's
    // leaving none) and one for what its tax base gains, Corporate tax's from the vouchers and the car,
    // Director pay's from the car's benefit. MapBadgesTests holds every plan within it.
    internal static readonly IReadOnlyDictionary<RiverModule, int> MostBadges = new Dictionary<RiverModule, int>
    {
        [RiverModule.Split] = 2,
        [RiverModule.CorporateTax] = 3,
        [RiverModule.DirectorPay] = 1,
    };

    [Fact]
    public void Place_everyModuleOfTheMap_hasASlotOfItsOwn()
    {
        MapModules.Order.Select(MapLayout.Place).Distinct().Count().ShouldBe(MapModules.Order.Count);
    }

    [Theory]
    [InlineData(RiverModule.River)]
    [InlineData((RiverModule)99)]
    public void Place_aModuleTheMapHasNoPlaceFor_isRefused(RiverModule module)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapLayout.Place(module));
    }

    // Each module sits in the column of whose money it moves: the company's year, its split and its tax in the
    // company's; Director pay and the partner in the director's, the partner over Director pay; every way across,
    // each lever, the royalty and the dividend, a gate in the middle; and the rows keep the river's order.
    [Fact]
    public void Place_eachModule_isInTheColumnOfWhoseMoneyItMovesInTheRiversOrder()
    {
        MapModules.Order.Select(module => (module, MapLayout.Place(module).Column)).ShouldBe([
            (RiverModule.CompanyYear, MapColumn.Company), (RiverModule.Vouchers, MapColumn.Gates), (RiverModule.Allowances, MapColumn.Gates),
            (RiverModule.Rent, MapColumn.Gates), (RiverModule.Car, MapColumn.Gates), (RiverModule.Split, MapColumn.Company),
            (RiverModule.Copyright, MapColumn.Gates), (RiverModule.DirectorPay, MapColumn.You), (RiverModule.Household, MapColumn.Household),
            (RiverModule.CorporateTax, MapColumn.Company), (RiverModule.Dividend, MapColumn.Gates)]);
        ((int)MapLayout.Place(RiverModule.Household).Row).ShouldBeLessThan((int)MapLayout.Place(RiverModule.DirectorPay).Row);
    }

    // The columns are framed and labelled left to right: the director's and their household's, the ways across,
    // the company's; every box of a plan lies in its column, the gates' in the ways across.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void From_anyPlan_drawsEachBoxInTheColumnOfWhoseMoneyItMoves(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);
        var view = MapView.From(river, river.Run());
        var columns = view.Columns.ToDictionary(column => column.Owner, column => column.Slot);

        view.ShouldSatisfyAllConditions(
            () => view.Columns.Select(column => column.Owner).ShouldBe([MapOwner.You, MapOwner.Across, MapOwner.Company]),
            () => view.Modules.Where(node => !Within(node.Slot, columns[OwnerOf(node.End.Module!.Value)])).Select(node => node.End.Id).ShouldBeEmpty());
    }

    private static MapOwner OwnerOf(RiverModule module) => MapLayout.Place(module).Column switch
    {
        MapColumn.You or MapColumn.Household => MapOwner.You,
        MapColumn.Gates => MapOwner.Across,
        _ => MapOwner.Company,
    };

    private static bool Within(MapSlot box, MapSlot column) =>
        box.Left >= column.Left && box.Right <= column.Right && box.Top >= column.Top && box.Bottom <= column.Bottom;

    [Fact]
    public void Grid_theExample_closesUpOverTheLeversOutOfThePlan()
    {
        MapLayout.Grid(MapView.ModulesOf(RiverInputs.Example)).Rows
            .ShouldBe([MapRow.CompanyYear, MapRow.Split, MapRow.Royalty, MapRow.CorporateTax, MapRow.Buckets]);
    }

    [Fact]
    public void Grid_everyLever_drawsEveryRow()
    {
        MapLayout.Grid(MapView.ModulesOf(MapLinksTests.Full)).Rows.ShouldBe(Enum.GetValues<MapRow>());
    }

    [Fact]
    public void Grid_companySkipped_isCompanyYearAndDirectorPayOverTheBuckets()
    {
        var river = MapLinksTests.Full.SkipCompany().TakeOutOfPlan(OptionalModule.Household);

        MapLayout.Grid(MapView.ModulesOf(river)).Rows.ShouldBe([MapRow.CompanyYear, MapRow.Split, MapRow.Buckets]);
    }

    // The partner in the plan keeps its row over Director pay's, the car's, its quotient a tax base of the pay
    // alone.
    [Fact]
    public void Grid_companySkippedWithThePartner_isCompanyYearThePartnerOverDirectorPayAndTheBuckets()
    {
        MapLayout.Grid(MapView.ModulesOf(MapLinksTests.Full.SkipCompany())).Rows
            .ShouldBe([MapRow.CompanyYear, MapRow.Car, MapRow.Split, MapRow.Buckets]);
    }

    // A gate's box holds its name, on two lines for the allowances', and a line for each line it says, at the
    // largest sizes the stylesheet draws them, its padding and frame: the rent saying two lines 3.124rem, the
    // allowances saying two 4.059rem, half a chain row at least.
    [Theory]
    [InlineData(RiverModule.Rent, 2, 3.124)]
    [InlineData(RiverModule.Allowances, 2, 4.059)]
    [InlineData(RiverModule.Vouchers, 0, 1.56)]
    public void Grid_aGatesLines_growItsBoxALineEach(RiverModule gate, int lines, double rem)
    {
        var modules = MapView.ModulesOf(MapLinksTests.Full);

        var grid = MapLayout.Grid(modules, gateLines: new Dictionary<RiverModule, int> { [gate] = lines });

        Units(grid, gate).ShouldBe(Math.Round(Math.Max(0.5m, (decimal)rem / MapLayout.SmallestRowRem), 12));
    }

    // What the stylesheet draws a gate with is what the layout keeps room for: its name at most 0.85rem with a
    // line height of 1.1, its lines at 0.68rem with a line height of 1.15, its padding of 0.25rem over and under.
    [Fact]
    public void MapTabCss_drawsAGateWithTheRoomTheLayoutKeepsForIt()
    {
        var (tab, words) = (Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css"), Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapGateWords.razor.css"));

        tab.ShouldSatisfyAllConditions(
            () => Stylesheet.Block(tab, ".map-module").ShouldContain("font-size: clamp(0.72rem, 1.6cqi, 0.85rem);"),
            () => Stylesheet.Block(tab, ".map-module").ShouldContain("line-height: 1.1;"),
            () => Stylesheet.Block(tab, ".map-module--gate").ShouldContain($"padding: {Stylesheet.Rem(MapLayout.GatePadRem)} 0.35rem;"),
            () => Stylesheet.Block(words, ".map-gate-words").ShouldContain("font-size: 0.68rem;"),
            () => Stylesheet.Block(words, ".map-gate-words").ShouldContain("line-height: 1.15;"),
            () => (MapLayout.TitleLineRem, MapLayout.GateLineRem).ShouldBe((0.85m * 1.1m, 0.68m * 1.15m)));
    }

    // How many lines a name takes on the narrowest map, as wide as Inter draws it at 0.72rem (MapAmounts.Width),
    // 4% more for weight 600: in a gate's box, 124 px less its padding and frame, and in a quiet pill in the
    // gates' column, less its padding and frame and a name's own padding. The allowances' name wraps in its box;
    // the allowances', the rent's and the royalty's wrap in a pill.
    [Theory]
    [InlineData(RiverModule.Vouchers)]
    [InlineData(RiverModule.Allowances)]
    [InlineData(RiverModule.Rent)]
    [InlineData(RiverModule.Car)]
    [InlineData(RiverModule.Copyright)]
    [InlineData(RiverModule.Dividend)]
    public void TitleLinesAndQuietLines_aGatesName_areTheLinesItTakesOnTheNarrowestMap(RiverModule gate)
    {
        var name = ResxText.For<MapTab>().Get(MapTabText.Module(gate)).ShouldNotBeNull();
        var width = (MapAmounts.Width(name) - 2m * MapAmounts.GroundRem * MapLayout.PixelsPerRem) * 1.04m;
        var pill = 124m - 2m * 0.6m * MapLayout.PixelsPerRem - 2m - 2m * 0.35m * MapLayout.PixelsPerRem;

        (MapLayout.TitleLines(gate), MapLayout.QuietLines(gate)).ShouldBe((width > MapLayout.GateRoomPixels ? 2 : 1, width > pill ? 2 : 1));
    }

    // A module's badges grow its box a line each, 0.3 of a chain row (MapLayout.BadgeRem and BadgeGapRem
    // at the smallest scale), and its row keeps round its tallest box the room a chain row keeps round its
    // box: the split's two badges add their lines to its row, every other row keeping its height.
    [Fact]
    public void Grid_aBoxsBadges_growItALineEachAndItsRowWithIt()
    {
        var modules = MapView.ModulesOf(MapLinksTests.Full);
        var (without, with) = (MapLayout.Grid(modules), MapLayout.Grid(modules, badgeLines: new Dictionary<RiverModule, int> { [RiverModule.Split] = 2 }));

        with.ShouldSatisfyAllConditions(
            () => Units(with, RiverModule.Split).ShouldBe(0.5m + 2 * 0.3m),
            () => (with.Units - without.Units).ShouldBe(0.6m),
            () => modules.Where(module => module != RiverModule.Split).Select(module => Units(with, module))
                .ShouldBe(modules.Where(module => module != RiverModule.Split).Select(module => Units(without, module))));
    }

    // Corporate tax's row is taller than its box needs, for the amounts of its many lines: badges grow it only
    // once its box takes more than the room the row has round it, at three; the band over the buckets, under it,
    // grows by what the box takes of the room under it, 0.15 a badge until the row grows.
    [Theory]
    [InlineData(1, 0.15)]
    [InlineData(2, 0.3)]
    [InlineData(3, 0.6)]
    public void Grid_badgesInAStreamsBox_growItsRowOnlyPastItsRoom(int lines, double growth)
    {
        var modules = MapView.ModulesOf(MapLinksTests.Full);

        var with = MapLayout.Grid(modules, badgeLines: new Dictionary<RiverModule, int> { [RiverModule.CorporateTax] = lines });

        (Units(with, RiverModule.CorporateTax), with.Units - MapLayout.Grid(modules).Units).ShouldBe((0.5m + lines * 0.3m, (decimal)growth));
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Grid_anyPlan_drawsNoBoxOverAnother(int bits, int quiet, bool companySkipped)
    {
        var boxes = Boxes(River(bits, quiet, companySkipped));

        boxes.SelectMany((a, i) => boxes.Skip(i + 1).Where(b => Overlaps(Area(a.Slot), Area(b.Slot))).Select(b => $"{a.Id} over {b.Id}")).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_runsEveryLineAcrossOrDownOnly(int bits, int quiet, bool companySkipped)
    {
        Routes(River(bits, quiet, companySkipped)).Where(route => MapLineWidths.Stretches(route.Route).Any(segment => segment.A.X != segment.B.X && segment.A.Y != segment.B.Y))
            .Select(route => route.Name).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_endsEveryLineOnItsTwoBoxes(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);
        var grid = Grid(river);

        Routes(river).Where(route => !OnEdge(grid, route.From, route.Route.Points[0]) || !EndsOn(grid, route.To, route.Route.End))
            .Select(route => route.Name).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_crossesNoBox(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);
        var boxes = Boxes(river);

        Routes(river).SelectMany(route => MapLineWidths.Stretches(route.Route).SelectMany(segment =>
                boxes.Where(box => Enters(segment, box.Slot) && !MayCross(route.Name, box.Id)).Select(box => $"{route.Name} through {box.Id}")))
            .ShouldBeEmpty();
    }

    // What the map itself draws, its boxes holding this plan's badges and its gates their lines: no line
    // through a box, a quiet pill or the pay's pill, but the pay through its own.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void From_anyPlan_drawsNoLineThroughABox(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);
        var view = MapView.From(river, river.Run());
        var boxes = view.Places().Select(place => (Id: string.Join('+', place.Nodes.Select(node => node.End.Id)), place.Slot))
            .Concat(view.Buckets.Select(bucket => (Id: bucket.End.Id, bucket.Slot)))
            .Concat(view.Pill is { } pill ? [(Id: Pill, pill.Slot)] : []).ToList();

        view.Links.SelectMany(link => MapLineWidths.Stretches(RouteOf(link)).SelectMany(segment =>
                boxes.Where(box => Enters(segment, box.Slot) && !MayCross($"{link.Kind} {link.From.Id} to {link.To.Id}", box.Id))
                    .Select(box => $"{link.Kind} {link.From.Id} to {link.To.Id} through {box.Id}")))
            .ShouldBeEmpty();
    }

    // The levers the routes here are worked out with as quiet, which the map draws quiet, are those the plan
    // leaves empty: every one of them, but for Expense allowances beside a rent paid (below).
    [Theory]
    [MemberData(nameof(RiversWithoutEmptyAllowancesBesideAPaidRent))]
    public void Quiet_anyPlan_isTheLeversItLeavesEmpty(int bits, int quiet, bool companySkipped)
    {
        Quiet(River(bits, quiet, companySkipped)).ShouldBe(Levers(quiet).Select(MapModules.Of), ignoreOrder: true);
    }

    // Empty Expense allowances beside a rent paid keep their box: the rule that leaves no office allowance
    // beside a rent links them to it, a condition, which counts as any link does.
    [Theory]
    [MemberData(nameof(RiversWithEmptyAllowancesBesideAPaidRent))]
    public void Quiet_emptyAllowancesBesideAPaidRent_isTheOtherLeversItLeavesEmpty(int bits, int quiet, bool companySkipped)
    {
        Quiet(River(bits, quiet, companySkipped)).ShouldBe(Levers(quiet).Where(lever => lever != OptionalModule.Allowances).Select(MapModules.Of), ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_sharesNoStretchOfLineBetweenTwoLinks(int bits, int quiet, bool companySkipped)
    {
        var routes = Routes(River(bits, quiet, companySkipped));

        routes.SelectMany((a, i) => routes.Skip(i + 1)
                .Where(b => MapLineWidths.Stretches(a.Route).Any(sa => MapLineWidths.Stretches(b.Route).Any(sb => Shared(sa, sb))))
                .Select(b => $"{a.Name} with {b.Name}"))
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_staysInsideTheMap(int bits, int quiet, bool companySkipped)
    {
        Routes(River(bits, quiet, companySkipped)).Where(route => route.Route.Points.Any(point => point.X is < 0m or > 100m || point.Y is < 0m or > 100m))
            .Select(route => route.Name).ShouldBeEmpty();
    }

    // Lines that run side by side, down a lane, along a side of a box or across a gap, are apart by their
    // half-widths and a halo's gap, so none covers another, on the map at 1,201 px, where it is narrowest,
    // and at 1,920 px.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_keepsLinesSideBySideApartAtTheirWidths(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);

        Crowded(Routes(river), Grid(river).Units).ShouldBeEmpty();
    }

    // No line crosses a box at its width, nor runs past the map's edge, but the chain's through its own
    // quiet row.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_crossesNoBoxAtItsWidth(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);

        Crossings(Routes(river), Boxes(river), Grid(river).Units).ShouldBeEmpty();
    }

    // A line meets each of its boxes within the side it meets, its whole width on it.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_meetsItsBoxesWithinTheirSidesAtItsWidth(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);

        Overhangs(Routes(river), Grid(river)).ShouldBeEmpty();
    }

    // Each arrowhead, sized by its line (MapLineWidths), is clear of every other arrowhead and of every other
    // line, and no longer than the stretch of line it ends, so it reads as an arrowhead and never runs past a
    // turn: on the narrowest map, where it takes the largest share of it.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_drawsEachArrowheadClearOfTheOthersAndWithinItsLastStretch(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);

        Arrowheads(Routes(river), Grid(river).Units).ShouldBeEmpty();
    }

    // Plans at the edges of what a plan holds keep their lines apart and off the boxes too, and their
    // arrowheads clear: the pay at its ceiling, a pay low enough for the tax credit on low earned income to
    // come back beside its taxes, a royalty past its limits, the costly car, a car costly to run, whose thick
    // line into the expenses leaves its side beside the drops into Corporate tax, and the company skipped
    // with a high pay or a low one.
    [Theory]
    [MemberData(nameof(ExtremeRivers))]
    public void Routed_aPlanAtTheEdges_keepsItsLinesApartAndOffTheBoxesAtTheirWidths(string plan)
    {
        var river = Extreme(plan);

        var routes = Routes(river);

        routes.ShouldSatisfyAllConditions(
            () => river.Run().Error.ShouldBeNull(),
            () => Crowded(routes, Grid(river).Units).ShouldBeEmpty(),
            () => Crossings(routes, Boxes(river), Grid(river).Units).ShouldBeEmpty(),
            () => Overhangs(routes, Grid(river)).ShouldBeEmpty(),
            () => Arrowheads(routes, Grid(river).Units).ShouldBeEmpty());
    }

    // Director pay's way into the taxes on pay and what comes back out of them turn along the band over the
    // buckets, a row each, what comes back along the row under the way in, leaving the bucket left of the way in
    // and coming up left of it, so that the two never cross; the dividend's way into its taxes and what comes back
    // run straight down and up beside each other, its way into what the company keeps along the band's first row.
    // A pay low enough for the credit on low earned income to come back beside the taxes on pay, the withholding
    // refunded on the exempt dividend beside the taxes on profit.
    [Fact]
    public void Routed_theWaysIntoTheTaxesAndWhatComesBack_turnAlongTheBandsRowsCrossingNeitherTheOther()
    {
        var view = MapView.From(Extreme("a low pay"), Extreme("a low pay").Run());

        var (payIn, payBack) = (MapViewTests.Line(view, MapEnd.Of(RiverModule.DirectorPay), MapEnd.InBucket("taxes_on_pay")), MapViewTests.Line(view, MapEnd.InBucket("taxes_on_pay"), MapEnd.Of(RiverModule.DirectorPay)));
        var (dividendIn, dividendBack) = (MapViewTests.Line(view, MapEnd.Of(RiverModule.Dividend), MapEnd.InBucket("taxes_on_profit")), MapViewTests.Line(view, MapEnd.InBucket("taxes_on_profit"), MapEnd.Of(RiverModule.Dividend)));

        view.ShouldSatisfyAllConditions(
            () => new[] { payIn, payBack }.Select(Turns).ShouldAllBe(turns => turns),
            () => new[] { dividendIn, dividendBack }.Select(line => line.Route.Points.Count).ShouldAllBe(points => points == 2),
            () => Row(payIn).ShouldBeLessThan(Row(payBack)),
            () => (Segments(payIn).Any(a => Segments(payBack).Any(b => Cross(a, b))), Segments(dividendIn).Any(a => Segments(dividendBack).Any(b => Cross(a, b)))).ShouldBe((false, false)));
    }

    // Corporate tax's way into its taxes comes down clear of the dividend's ways into them and back, crossing none
    // of the band's rows they turn along: on plans where the company pays its tax and withholds on a dividend,
    // with the refund on its exempt part.
    [Theory]
    [MemberData(nameof(DividendTaxedRivers))]
    public void Routed_corporateTaxsWayIntoItsTaxes_crossesNoneOfTheDividendsWaysIntoThemAndBack(string plan)
    {
        var river = Plan(plan);
        var view = MapView.From(river, river.Run());

        var corporate = view.Bundles().Where(line => line.From == MapEnd.Of(RiverModule.CorporateTax) && line.To == MapEnd.InBucket("taxes_on_profit")).ToList();
        var dividend = view.Bundles().Where(line => line.Touches(RiverModule.Dividend) && (line.From == MapEnd.InBucket("taxes_on_profit") || line.To == MapEnd.InBucket("taxes_on_profit"))).ToList();

        corporate.ShouldSatisfyAllConditions(
            () => (corporate.Count, dividend.Count).ShouldBe((1, 2)),
            () => corporate.SelectMany(a => dividend.Where(b => Segments(a).Any(sa => Segments(b).Any(sb => Cross(sa, sb)))).Select(b => $"{b.From.Id} to {b.To.Id}"))
                .ShouldBeEmpty());
    }

    // What reaches the pocket runs down one line left of the director's column, straight from the highest point
    // where a line joins it into the pocket's top, its stretches end to end, half its thickest width and a pixel
    // inside the pocket's left edge and clear of the director's boxes; each gate's line into it leaves its left
    // side straight across, Director pay's its left side, and none crosses another; the car's contribution
    // leaves it straight across under the car's gate into the company's line.
    [Fact]
    public void Routed_thePocketLine_runsDownTheLeftOfYourColumnEachLineJoiningItStraightAcross()
    {
        var view = MapView.From(MapLinksTests.Full, MapLinksTests.Full.Run());
        var line = view.Bundles().Where(bundle => bundle.From.PocketLine is not null && (bundle.To.PocketLine is not null || bundle.To == MapEnd.InBucket("pocket"))).ToList();
        var joins = view.Bundles().Where(bundle => bundle.To.PocketLine is not null && bundle.From.PocketLine is null).ToList();
        var (pocket, pay, leave) = (view.Buckets[0].Slot, Slot(view, RiverModule.DirectorPay), MapViewTests.Line(view, MapEnd.OnPocketLine(RiverModule.Car), MapEnd.Beside(RiverModule.Car)));
        var x = line[0].Route.Points[0].X;

        view.ShouldSatisfyAllConditions(
            () => line.SelectMany(bundle => bundle.Route.Points).Select(point => point.X).Distinct().ShouldBe([x]),
            () => (Math.Round((x - pocket.Left) / MapLayout.MapGrid.AcrossPixel, 6), Math.Round((pay.Left - x) / MapLayout.MapGrid.AcrossPixel, 6)).ShouldBe((MapLineWidths.Thickest / 2m + 1m, 5m)),
            () => line.Zip(line.Skip(1)).Where(pair => pair.First.Route.End != pair.Second.Route.Points[0]).ShouldBeEmpty(),
            () => (line[0].Route.Points[0].Y, line[^1].Route.End.Y).ShouldBe((joins.Min(join => join.Route.Points[0].Y), pocket.Top)),
            () => joins.Select(join => join.From.Module).ShouldBe([RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.DirectorPay, RiverModule.Copyright, RiverModule.Dividend], ignoreOrder: true),
            () => joins.Where(join => join.Route.Points is not [var a, var b] || a.Y != b.Y || b.X != x || a.X != Slot(view, join.From.Module!.Value).Left).Select(join => join.From.Id).ShouldBeEmpty(),
            () => joins.SelectMany((a, i) => joins.Skip(i + 1).Where(b => Segments(a).Any(sa => Segments(b).Any(sb => Cross(sa, sb)))).Select(b => $"{a.From.Id} with {b.From.Id}")).ShouldBeEmpty(),
            () => (leave.Route.Points.Count, leave.Route.Points[0].X, leave.Route.Arrow, leave.Route.End.Y > Slot(view, RiverModule.Car).Bottom).ShouldBe((2, x, MapDirection.Right, true)));
    }


    // The company's line runs down its own column from Company year's bottom into the split's top, straight; each
    // lever's share leaves it across to its gate's right side, at the share's height; the car's costs leave it to
    // the right, the car's contribution joins it from the left, under the car's gate.
    [Fact]
    public void Routed_theCompanysLine_runsStraightDownFromCompanyYearIntoTheSplitEachShareAcrossToItsGate()
    {
        var view = MapView.From(MapLinksTests.Full, MapLinksTests.Full.Run());
        var line = view.Bundles().Where(bundle => MapLinksTests.CompanysLine(bundle.Links).Any()).ToList();
        var takes = view.Bundles().Where(bundle => bundle.From.Lever is { } lever && bundle.To == MapEnd.Of(lever)).ToList();
        var (car, costs) = (Slot(view, RiverModule.Car), MapViewTests.Line(view, MapEnd.Beside(RiverModule.Car), MapEnd.InBucket("expenses")));
        var contribution = MapViewTests.Line(view, MapEnd.OnPocketLine(RiverModule.Car), MapEnd.Beside(RiverModule.Car));

        view.ShouldSatisfyAllConditions(
            () => line.Select(bundle => bundle.Route.Points.Select(point => point.X).Distinct().Count()).ShouldAllBe(count => count == 1),
            () => line.Select(bundle => bundle.Route.Points[0].X).Distinct().ShouldHaveSingleItem(),
            () => (line[0].Route.Points[0].Y, line[^1].Route.End.Y).ShouldBe((Slot(view, RiverModule.CompanyYear).Bottom, Slot(view, RiverModule.Split).Top)),
            () => line.Zip(line.Skip(1)).Where(pair => pair.First.Route.End != pair.Second.Route.Points[0]).ShouldBeEmpty(),
            () => takes.Select(take => take.To.Module).ShouldBe([RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent]),
            () => takes.Where(take => take.Route.Points is not [var a, var b] || a.Y != b.Y || b.X != Slot(view, take.To.Module!.Value).Right).Select(take => take.To.Id).ShouldBeEmpty(),
            () => (costs.Route.Points[0].X > line[0].Route.Points[0].X, (costs.Route.Points[0].X - line[0].Route.Points[0].X) / MapLayout.MapGrid.AcrossPixel <= MapLineWidths.Thickest / 2m,
                costs.Route.Points[1].X > Slot(view, RiverModule.Split).Right).ShouldBe((true, true, true)),
            () => (contribution.Route.Arrow, contribution.Route.End.Y > car.Bottom, contribution.Route.End.X < line[0].Route.Points[0].X,
                (line[0].Route.Points[0].X - contribution.Route.End.X) / MapLayout.MapGrid.AcrossPixel <= MapLineWidths.Thickest / 2m)
                .ShouldBe((MapDirection.Right, true, true, true)));
    }

    // The condition a royalty sets on the split leaves the royalty's right side, so it never climbs beside the
    // split's line into the royalty's top.
    [Fact]
    public void Routed_theConditionARoyaltySetsOnTheSplit_leavesTheRoyaltysRightSide()
    {
        var view = MapView.From(MapLinksTests.Full, MapLinksTests.Full.Run());
        var royalty = view.Modules.Single(node => node.End.Module == RiverModule.Copyright).Slot;

        var condition = view.Bundles().Single(line => line.Kind == MapLinkKind.Condition && line.From == MapEnd.Of(RiverModule.Copyright));

        condition.Route.Points[0].X.ShouldBe(royalty.Right);
    }

    // The marital quotient climbs straight from Director pay's top into the partner's bottom, its box over
    // Director pay; from the partner, straight down into Director pay's top.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Routed_theMaritalQuotient_runsStraightUpFromDirectorPayIntoThePartnerOrBackDown(bool fromThePartner)
    {
        var river = fromThePartner
            ? RiverInputs.Example.AddToPlan(OptionalModule.Household).WithDirectorPay(12_000m).WithPartnerIncome(60_000m)
            : MapLinksTests.Full;
        var view = MapView.From(river, river.Run());
        var (pay, partner) = (Slot(view, RiverModule.DirectorPay), Slot(view, RiverModule.Household));

        var route = view.Bundles().Single(line => line.Kind == MapLinkKind.TaxBase && line.Card == RiverModule.Household).Route;

        var (start, end) = fromThePartner ? (route.Points[^1], route.Points[0]) : (route.Points[0], route.Points[^1]);
        route.ShouldSatisfyAllConditions(
            () => (route.Points.Count, start.Y, end.Y, start.X == end.X).ShouldBe((2, pay.Top, partner.Bottom, true)),
            () => (end.X > partner.Left && end.X < partner.Right).ShouldBeTrue(),
            () => route.Arrow.ShouldBe(fromThePartner ? MapDirection.Down : MapDirection.Up),
            () => partner.Bottom.ShouldBeLessThan(pay.Top));
    }

    private static MapSlot Slot(MapView view, RiverModule module) => view.Modules.Single(node => node.End.Module == module).Slot;

    // The band over the buckets grows the map only by what the last row lacks under its boxes: its first 0.1 of a
    // chain row under the lowest box, each next row 0.35 under the one before, the buckets 0.2 under the last; two
    // rows with Director pay, 0.65, a rent's and a royalty's taxes a row each, and a whole chain row at least with a
    // dividend. Every lever: Company year's row 1; the gates' 1.46, each 0.96 high for its name and two lines, the
    // allowances' 1.75, theirs on two lines; the split's 1.3, a badge; the royalty's 1.6; Corporate tax's 1.9,
    // three badges, which keeps 0.25 under its box; the band's four rows 1.35, so the buckets' row 0.7 and 1.1,
    // 13.73 chain rows. The example: 1, the split's 1, the royalty's 1.6, Corporate tax's 1.6, the dividend's
    // gate 0.96 keeping 0.32 under it, the buckets' 0.7 and 0.68, 6.58. The company skipped: Company year's row
    // and Director pay's, 1 each, the buckets' 0.7 and 0.4, 3.1; with the partner in the plan, its row of 1.3 over
    // Director pay's, room for the marital quotient's amount between them, 4.4.
    [Theory]
    [MemberData(nameof(BandHeights))]
    public void From_aPlan_growsByTheBandItsLastRowLacksRoomFor(string plan, decimal units)
    {
        var river = Plan(plan);

        var view = MapView.From(river, river.Run());

        Math.Round(view.Units, 2).ShouldBe(units);
    }

    // A stretch of line covers its length and half its width each side, its halo's gap too when asked, in the
    // pixels of the map drawn at a scale: across, a stretch whose ends share their height, else down.
    [Fact]
    public void Stroke_aStretchAcrossOrDown_coversItsLengthAndHalfItsWidthEachSide()
    {
        var scale = MapScale.Of(500m, 200m);

        (MapLineWidths.Stroke(new MapPoint(10m, 50m), new MapPoint(30m, 50m), 4m, scale), MapLineWidths.Stroke(new MapPoint(20m, 40m), new MapPoint(20m, 10m), 4m, scale, 2m))
            .ShouldBe((new MapArea(50m, 98m, 150m, 102m), new MapArea(96m, 20m, 104m, 80m)));
    }

    // A line's stretches run from each of its points to the next, one of no length left out, and each covers
    // its own area, across or down.
    [Fact]
    public void Strokes_aRoute_isEachStretchFromOnePointToTheNextLeavingOutOneOfNoLength()
    {
        var route = new MapRoute([new MapPoint(10m, 10m), new MapPoint(10m, 10m), new MapPoint(30m, 10m), new MapPoint(30m, 40m)]) { Width = 2m };
        var scale = MapScale.Of(100m, 100m);

        route.ShouldSatisfyAllConditions(
            () => MapLineWidths.Stretches(route).ShouldBe([(new MapPoint(10m, 10m), new MapPoint(30m, 10m)), (new MapPoint(30m, 10m), new MapPoint(30m, 40m))]),
            () => MapLineWidths.Strokes(route, scale).ShouldBe([(true, new MapArea(10m, 9m, 30m, 11m)), (false, new MapArea(29m, 10m, 31m, 40m))]));
    }

    // An arrowhead covers its base across the line's end and its length back along it, its point on the end,
    // whichever way the line enters it.
    [Fact]
    public void Head_aLinesArrowhead_coversItsBaseAndLengthBackFromItsEnd()
    {
        var scale = MapScale.Of(100m, 100m);
        MapRoute Into(MapPoint from) => new([from, new MapPoint(50m, 50m)]) { Width = 2m };

        new[] { new MapPoint(50m, 10m), new MapPoint(50m, 90m), new MapPoint(10m, 50m), new MapPoint(90m, 50m) }.Select(from => MapLineWidths.Head(Into(from), scale))
            .ShouldBe([new MapArea(45.2m, 42.8m, 54.8m, 50m), new MapArea(45.2m, 50m, 54.8m, 57.2m), new MapArea(42.8m, 45.2m, 50m, 54.8m), new MapArea(50m, 45.2m, 57.2m, 54.8m)]);
    }

    // Two areas overlap where they share more than an edge, kept a clearance apart, or, the clearance below
    // nothing, where they overlap by more than it.
    [Theory]
    [InlineData(10, 0, false)]
    [InlineData(9.5, 0, true)]
    [InlineData(11, 1, false)]
    [InlineData(10.5, 1, true)]
    [InlineData(9.995, -0.01, false)]
    public void Overlaps_anAreaBesideAnother_isWhereTheyShareMoreThanAnEdgeAClearanceApart(double left, double clearance, bool overlaps)
    {
        new MapArea(0m, 0m, 10m, 10m).Overlaps(new MapArea((decimal)left, 0m, (decimal)left + 5m, 10m), (decimal)clearance).ShouldBe(overlaps);
    }

    // Whether a line turns once along a row: down, across, down.
    private static bool Turns(MapBundle line) =>
        line.Route.Points is [var a, var b, var c, var d] && a.X == b.X && b.Y == c.Y && c.X == d.X && b.X != c.X;

    // The y of the row a line turns along.
    private static decimal Row(MapBundle line) => line.Route.Points[1].Y;

    private static IEnumerable<(MapPoint A, MapPoint B)> Segments(MapBundle line) => MapLineWidths.Stretches(line.Route);

    // Whether two segments, one across and one down, cross inside both.
    private static bool Cross((MapPoint A, MapPoint B) a, (MapPoint A, MapPoint B) b)
    {
        var (across, down) = a.A.Y == a.B.Y ? (a, b) : (b, a);
        return across.A.Y == across.B.Y && down.A.X == down.B.X
            && Math.Min(across.A.X, across.B.X) < down.A.X && down.A.X < Math.Max(across.A.X, across.B.X)
            && Math.Min(down.A.Y, down.B.Y) < across.A.Y && across.A.Y < Math.Max(down.A.Y, down.B.Y);
    }

    // A lane its lines cannot all fit at their widths squeezes them into it, never past it: the royalty's part
    // taxed as pay and the rent's and the royalty's taxes and what comes back of them down the lane beside the
    // gates, each as large as the run's largest line, cross no box and land on their ends.
    [Fact]
    public void Routed_aLaneTooNarrowForItsLines_squeezesThemIntoItCrossingNoBox()
    {
        var river = MapLinksTests.Full;
        var grid = Grid(river);
        MapLink[] links =
        [
            Money(RiverModule.Copyright, MapEnd.Of(RiverModule.DirectorPay)),
            .. new[] { RiverModule.Rent, RiverModule.Copyright }.Select(module => Money(module, MapEnd.InBucket("taxes_on_pay"))),
            .. new[] { RiverModule.Rent, RiverModule.Copyright }.Select(module => Money(module, MapEnd.InBucket("taxes_on_pay")) with { From = MapEnd.InBucket("taxes_on_pay"), To = MapEnd.Of(module) }),
        ];

        var routes = RoutesOf(MapLayout.Routed(grid, links));

        routes.ShouldSatisfyAllConditions(
            () => routes.Select(route => route.Route.Width).Distinct().ShouldBe([MapLineWidths.Thickest]),
            () => Crossings(routes, Boxes(river), grid.Units).ShouldBeEmpty(),
            () => Overhangs(routes, grid).ShouldBeEmpty(),
            () => Arrowheads(routes, grid.Units).ShouldBeEmpty());
    }

    // The ends a card's line joins in these plans are among those the routes are held to below.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void DrainEnds_everyModuleAndBucketALineJoins_isHeldToItsRoutes(bool companyPays, bool companySkipped)
    {
        var river = companyPays ? MapLinksTests.Full.WithContributionsPaidBy(Netto.Core.SocialContributions.ContributionsPaidBy.Company) : MapLinksTests.Full;
        river = companySkipped ? river.SkipCompany() : river;

        MapLinks.Of(river, river.Run()).Where(link => link.From.Bucket is not null || link.To.Bucket is not null)
            .Select(link => (link.From.Bucket is null ? link.From : link.To, link.From.Bucket ?? link.To.Bucket!))
            .Where(ends => !DrainEnds.Contains(ends)).ShouldBeEmpty();
    }

    // A line between a module and a bucket, either way, beside every other line of the plan: it ends on
    // its two boxes, crosses none, runs across or down only and shares no stretch with another, and at its
    // width keeps apart from the others and off the boxes.
    [Theory]
    [MemberData(nameof(EveryDrainEnds))]
    public void Routed_aLineEitherWayBetweenAModuleAndABucket_endsOnBothAndCrossesNoBox(int ends)
    {
        var (end, bucket) = DrainEnds[ends];
        var river = MapLinksTests.Full;
        var grid = Grid(river);
        var module = (end.Module ?? end.Lever)!.Value;
        MapLink[] both =
        [
            new(MapLinkKind.IntoBucket, end, MapEnd.InBucket(bucket), module, new MapLabel(MapText.Map, "Into", []), 1m),
            new(MapLinkKind.IntoBucket, MapEnd.InBucket(bucket), end, module, new MapLabel(MapText.Map, "Back", []), 1m),
        ];

        var routes = Routes(river, both);

        var boxes = Boxes(river, routes);
        routes.ShouldSatisfyAllConditions(
            () => routes.Where(route => !OnEdge(grid, route.From, route.Route.Points[0]) || !EndsOn(grid, route.To, route.Route.End))
                .Select(route => route.Name).ShouldBeEmpty(),
            () => routes.SelectMany(route => MapLineWidths.Stretches(route.Route).SelectMany(segment =>
                boxes.Where(box => Enters(segment, box.Slot) && !MayCross(route.Name, box.Id)).Select(box => $"{route.Name} through {box.Id}"))).ShouldBeEmpty(),
            () => routes.Where(route => MapLineWidths.Stretches(route.Route).Any(segment => segment.A.X != segment.B.X && segment.A.Y != segment.B.Y))
                .Select(route => route.Name).ShouldBeEmpty(),
            () => routes.SelectMany((a, i) => routes.Skip(i + 1)
                .Where(b => MapLineWidths.Stretches(a.Route).Any(sa => MapLineWidths.Stretches(b.Route).Any(sb => Shared(sa, sb))))
                .Select(b => $"{a.Name} with {b.Name}")).ShouldBeEmpty(),
            () => Crowded(routes, grid.Units).ShouldBeEmpty(),
            () => Crossings(routes, boxes, grid.Units).ShouldBeEmpty(),
            () => Overhangs(routes, grid).ShouldBeEmpty(),
            () => Arrowheads(routes, grid.Units).ShouldBeEmpty());
    }

    // A line back out of a bucket ends on its module, its arrow pointing into it; back out of the pocket, it
    // leaves the pocket line at its module's point.
    [Theory]
    [MemberData(nameof(EveryDrainEnds))]
    public void Routed_aLineBackOutOfABucket_endsOnItsModule(int ends)
    {
        var (end, bucket) = DrainEnds[ends];
        var module = (end.Module ?? end.Lever)!.Value;
        var river = MapLinksTests.Full;
        var grid = Grid(river);
        var back = new MapLink(MapLinkKind.IntoBucket, MapEnd.InBucket(bucket), end, module, new MapLabel(MapText.Map, "Back", []), 1m);
        var from = bucket == "pocket" ? MapEnd.OnPocketLine(module) : MapEnd.InBucket(bucket);

        var route = RouteOf(MapLayout.Routed(grid, MapPocketLine.Of([back])).Single(link => link.To == end));

        (OnEdge(grid, from, route.Points[0]), OnEdge(grid, end, route.End)).ShouldBe((true, true));
    }

    // Links of the same ends and kind are one line on the map, their amount said once.
    [Fact]
    public void Routed_linksOfTheSameEndsAndKind_shareOneRoute()
    {
        var river = MapLinksTests.Full;

        var links = MapLayout.Routed(MapLayout.Grid(MapView.ModulesOf(river)), MapPocketLine.Of(MapLinks.Of(river, river.Run())));

        links.GroupBy(link => (link.From, link.To, link.Kind)).Where(bundle => bundle.Select(link => link.Route).Distinct().Count() != 1)
            .Select(bundle => $"{bundle.Key.From.Id} to {bundle.Key.To.Id}").ShouldBeEmpty();
    }

    // The car's costs, off the company's line beside it, and INASTI's company contribution, out of Company year's
    // right side, run along their row to a lane of their own right of the company's boxes, Company year's, the
    // higher, furthest out, and come down into the expenses' top.
    [Fact]
    public void Routed_theCarsCostsAndTheCompanyContribution_comeDownRightOfTheCompanysBoxesIntoTheExpensesTop()
    {
        var river = MapLinksTests.Full;
        var grid = Grid(river);
        var links = MapLayout.Routed(grid, MapPocketLine.Of(MapLinks.Of(river, river.Run())));
        var (expenses, company) = (grid.Slot(MapEnd.InBucket("expenses")), grid.Slot(MapEnd.Of(RiverModule.CorporateTax)).Right);

        var (car, year) = (Into(MapEnd.Beside(RiverModule.Car)), Into(MapEnd.Of(RiverModule.CompanyYear)));

        links.ShouldSatisfyAllConditions(
            () => (Down(car, car.Points[0].X), Down(year, grid.Slot(MapEnd.Of(RiverModule.CompanyYear)).Right)).ShouldBe((true, true)),
            () => (car.Points[0].X > grid.Slot(MapEnd.Beside(RiverModule.Car)).CenterX && car.Points[0].X <= grid.Slot(MapEnd.Beside(RiverModule.Car)).Right).ShouldBeTrue(),
            () => (year.Points[1].X > car.Points[1].X).ShouldBeTrue());

        MapRoute Into(MapEnd from) => RouteOf(links.Single(link => link.From == from && link.To == MapEnd.InBucket("expenses")));

        bool Down(MapRoute route, decimal leaves) => route.Points is [var a, var b, var c]
            && a.X == leaves && b.Y == a.Y && b.X > company && c == new MapPoint(b.X, expenses.Top) && c.X > expenses.Left && c.X < expenses.Right;
    }

    // No plan's lines cross one another more often than its baseline holds (MapCrossings.txt): every line but
    // the conditions', hidden until ticked, each stretch across crossing a stretch down inside both, a line
    // ending on another being no crossing (LineCrossings). Counted on the routes' points, so the same at every
    // width the map is drawn, 544 px among them. A layout that crosses fewer lines may lower the baseline; none
    // may raise it.
    [Theory]
    [MemberData(nameof(CrossingPlans))]
    public void From_anyPlan_crossesNoMoreLinesThanItsBaseline(string plan)
    {
        var crossings = LineCrossings(MapAmountsTests.View(plan));

        crossings.ShouldBeLessThanOrEqualTo(Baseline[plan]);
    }

    // The counts held name every plan the crossings are held on, once each.
    [Fact]
    public void MapCrossings_namesEveryPlanOnce()
    {
        Baseline.Keys.ShouldBe(CrossingPlans().Select(row => row.Data), ignoreOrder: true);
    }

    public static TheoryData<string> CrossingPlans() => MapAmountsTests.Plans();

    // How many times the lines of a map cross one another: each stretch across of a line crossing a stretch down
    // of another inside both, the conditions' lines left out.
    internal static int LineCrossings(MapView view)
    {
        var lines = view.Bundles().Where(line => line.Kind != MapLinkKind.Condition).Select(line => MapLineWidths.Stretches(line.Route).ToList()).ToList();
        return lines.SelectMany((a, i) => lines.Skip(i + 1).Select(b => a.Sum(sa => b.Count(sb => Cross(sa, sb))))).Sum();
    }

    // The most crossings each plan's map may have, by the plan's name (MapAmountsTests.Plans), the counts of the
    // map as it was drawn before the pocket line gathered what reaches the director: a line each, the name and
    // the count apart by a tab, a line opening on # a note. To take it again from a layout, write for each plan
    // its name, a tab and LineCrossings(MapAmountsTests.View(name)), one plan a line.
    private static IReadOnlyDictionary<string, int> Baseline => Counts.Value;

    private static readonly Lazy<IReadOnlyDictionary<string, int>> Counts = new(() =>
        File.ReadAllLines(RepositoryFile.FindUp("tests", "Netto.Tests.Unit", "WebClient", "Tabs", "MapCrossings.txt"))
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('\t'))
            .ToDictionary(parts => parts[0], parts => int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)));

    // Every composition of the levers, with the company's year and without it, and, with it, every set of
    // the levers in the plan left empty (quiet, by the bits of a number as the composition's), which the
    // map draws quiet but for Expense allowances beside a rent paid.
    public static TheoryData<int, int, bool> Rivers() => Data(Plans());

    public static TheoryData<int, int, bool> RiversWithoutEmptyAllowancesBesideAPaidRent() => Data(Plans().Where(plan => !EmptyAllowancesBesideAPaidRent(plan)));

    public static TheoryData<int, int, bool> RiversWithEmptyAllowancesBesideAPaidRent() => Data(Plans().Where(EmptyAllowancesBesideAPaidRent));

    internal static IEnumerable<(int Bits, int Quiet, bool CompanySkipped)> Plans()
    {
        var compositions = 1 << PlanComposition.Modules.Count;
        foreach (var bits in Enumerable.Range(0, compositions))
        {
            foreach (var quiet in Enumerable.Range(0, compositions).Where(quiet => (quiet & ~bits) == 0))
                yield return (bits, quiet, false);
            yield return (bits, 0, true);
        }
    }

    private static bool EmptyAllowancesBesideAPaidRent((int Bits, int Quiet, bool CompanySkipped) plan) =>
        Levers(plan.Quiet).Contains(OptionalModule.Allowances) && Levers(plan.Bits).Contains(OptionalModule.Rent) && !Levers(plan.Quiet).Contains(OptionalModule.Rent);

    private static TheoryData<int, int, bool> Data(IEnumerable<(int Bits, int Quiet, bool CompanySkipped)> plans)
    {
        var data = new TheoryData<int, int, bool>();
        foreach (var (bits, quiet, companySkipped) in plans)
            data.Add(bits, quiet, companySkipped);

        return data;
    }

    internal static RiverInputs River(int bits, int quiet, bool companySkipped)
    {
        var river = MapQuietTests.Emptied(MapLinksTests.Full with { Composition = MapModulesTests.Composition(bits) }, Levers(quiet));
        return companySkipped ? river.SkipCompany() : river;
    }

    private static OptionalModule[] Levers(int bits) => [.. PlanComposition.Modules.Where((_, index) => (bits & (1 << index)) != 0)];

    // The levers the map draws quiet, read off the map itself, the conditions of its year among its links.
    private static HashSet<RiverModule> Quiet(RiverInputs river) =>
        MapView.From(river, river.Run()).Modules.Where(node => node.Quiet).Select(node => node.End.Module!.Value).ToHashSet();

    // The plan's grid with the levers the map draws quiet, every box holding the most badges it can and every gate
    // the lines it says.
    private static MapLayout.MapGrid Grid(RiverInputs river) => MapLayout.Grid(MapView.ModulesOf(river), Quiet(river), MostBadges, GateLines(river));

    // How many lines each gate of a plan says (MapGates), as the map draws them.
    internal static IReadOnlyDictionary<RiverModule, int> GateLines(RiverInputs river)
    {
        var gates = MapGates.Of(river, river.Run());
        return MapView.ModulesOf(river).ToDictionary(module => module, module => gates.For(module).Count);
    }

    // How tall a module's box is, in chain rows.
    private static decimal Units(MapLayout.MapGrid grid, RiverModule module) => Math.Round(grid.Slot(MapEnd.Of(module)).Height * grid.Units / 100m, 12);

    // Each box of the map, a quiet pill once, with the levers in it, and the pay's pill on the pay's line.
    private static IReadOnlyList<(string Id, MapSlot Slot, IReadOnlyList<RiverModule> Modules)> Boxes(RiverInputs river, IReadOnlyList<RoutedLine>? routes = null)
    {
        var grid = Grid(river);
        var pay = (routes ?? Routes(river)).Where(route => route.Name == Pay).Select(route => MapLayout.PillOn(grid, route.Route)).ToList();
        return
        [
            .. MapView.ModulesOf(river).GroupBy(module => grid.Slot(MapEnd.Of(module)))
                .Select(place => (string.Join('+', place.Select(MapModules.Name)), place.Key, (IReadOnlyList<RiverModule>)[.. place])),
            .. BucketsView.Keys.Select(key => (key, grid.Slot(MapEnd.InBucket(key)), (IReadOnlyList<RiverModule>)[])),
            .. pay.Select(slot => (Pill, slot, (IReadOnlyList<RiverModule>)[])),
        ];
    }

    // The pay's pill, which the pay alone runs through, its pill standing on it.
    private const string Pill = "pill";

    private const string Pay = "PassedOn split to director-pay";

    private static bool MayCross(string line, string box) => box == Pill && line == Pay;

    // Each line of the plan, the run's links and the conditions between the modules it draws with a box,
    // folded past the levers the map draws quiet and gathered into the pocket line as the map does, once.
    private static IReadOnlyList<RoutedLine> Routes(RiverInputs river, params MapLink[] more)
    {
        var run = river.Run();
        var modules = MapView.ModulesOf(river);
        var quiet = Quiet(river);
        var conditions = Conditions
            .Where(condition => modules.Contains(condition.From) && modules.Contains(condition.To) && !quiet.Contains(condition.From) && !quiet.Contains(condition.To))
            .Select(condition => new MapLink(MapLinkKind.Condition, MapEnd.Of(condition.From), MapEnd.Of(condition.To), condition.From,
                new MapLabel(MapText.Map, "Condition", []), null));

        return RoutesOf(MapLayout.Routed(MapLayout.Grid(modules, quiet, MostBadges, GateLines(river)), MapPocketLine.Of(MapQuiet.Fold([.. MapLinks.Of(river, run), .. conditions, .. more]))));
    }

    // Each line of links routed, once.
    private static IReadOnlyList<RoutedLine> RoutesOf(IReadOnlyList<MapLink> links) =>
    [
        .. links.GroupBy(link => link.Route).Select(bundle => bundle.First())
            .Select(link => new RoutedLine($"{link.Kind} {link.From.Id} to {link.To.Id}", link.From, link.To, RouteOf(link), link.Through)),
    ];

    // The route the layout gave a link, which every link it routed has.
    private static MapRoute RouteOf(MapLink link) =>
        link.Route ?? throw new InvalidOperationException($"The layout gave {link.Kind} {link.From.Id} to {link.To.Id} no route.");

    // A line as the layout routes it, named by its kind and its ends.
    private sealed record RoutedLine(string Name, MapEnd From, MapEnd To, MapRoute Route, IReadOnlyList<RiverModule> Through);

    // The sizes the map is drawn at, in pixels, each with a chain row at its smallest, 3.25rem
    // (MapLayout.SmallestRowRem), the shortest the map is ever drawn: the narrowest, a column 34rem wide
    // beside the chosen module's from 1,201 px (MapLayout.NarrowestRem), and its 1,184 px at 1,920 px.
    internal static readonly (string Name, decimal Width)[] Sizes = [("1,201 px", MapLayout.NarrowestRem * 16m), ("1,920 px", 1_184m)];

    internal const decimal RowPixels = MapLayout.SmallestRowRem * 16m;

    // A hundredth of a pixel, for the rounding of the routes' percents.
    private const decimal Tolerance = 0.01m;

    // Every two lines running side by side closer than their half-widths and a halo's gap, at each size.
    private static List<string> Crowded(IReadOnlyList<RoutedLine> routes, decimal units)
    {
        var faults = new List<string>();
        foreach (var (size, width) in Sizes)
        {
            var scale = MapScale.Of(width, RowPixels * units);
            faults.AddRange(routes.SelectMany((a, i) => routes.Skip(i + 1)
                .Where(b => MapLineWidths.Stretches(a.Route).Any(sa => MapLineWidths.Stretches(b.Route).Any(sb => Closer(sa, sb, (a.Route.Width + b.Route.Width) / 2m + MapLinkStyle.HaloGap, scale))))
                .Select(b => $"{a.Name} with {b.Name} at {size}")));
        }

        return faults;
    }

    // Whether two segments, both across or both down, run side by side along a stretch they share, closer
    // than the pixels given.
    private static bool Closer((MapPoint A, MapPoint B) a, (MapPoint A, MapPoint B) b, decimal apart, MapScale scale)
    {
        if (a.A.X == a.B.X && b.A.X == b.B.X)
            return Along(a.A.Y, a.B.Y, b.A.Y, b.B.Y) && Math.Abs(a.A.X - b.A.X) * scale.X < apart - Tolerance;
        if (a.A.Y == a.B.Y && b.A.Y == b.B.Y)
            return Along(a.A.X, a.B.X, b.A.X, b.B.X) && Math.Abs(a.A.Y - b.A.Y) * scale.Y < apart - Tolerance;
        return false;
    }

    // Whether two stretches of one axis share a part of it.
    private static bool Along(decimal a1, decimal a2, decimal b1, decimal b2) =>
        Math.Max(Math.Min(a1, a2), Math.Min(b1, b2)) < Math.Min(Math.Max(a1, a2), Math.Max(b1, b2));

    // Every line whose stroke, at its width, enters a box, but the chain's its own quiet row, or runs past
    // the map's edge, at each size.
    private static List<string> Crossings(IReadOnlyList<RoutedLine> routes, IReadOnlyList<(string Id, MapSlot Slot, IReadOnlyList<RiverModule> Modules)> boxes, decimal units)
    {
        var faults = new List<string>();
        foreach (var (size, width) in Sizes)
        {
            var scale = MapScale.Of(width, RowPixels * units);
            var map = MapArea.Of(new MapSlot(0m, 0m, 100m, 100m), scale);
            foreach (var route in routes)
            {
                foreach (var (_, stroke) in MapLineWidths.Strokes(route.Route, scale))
                {
                    faults.AddRange(boxes.Where(box => !MayCross(route.Name, box.Id) && Overlaps(stroke, MapArea.Of(box.Slot, scale)))
                        .Select(box => $"{route.Name} through {box.Id} at {size}"));
                    if (stroke.Left < map.Left - Tolerance || stroke.Right > map.Right + Tolerance || stroke.Top < map.Top - Tolerance || stroke.Bottom > map.Bottom + Tolerance)
                        faults.Add($"{route.Name} past the map's edge at {size}");
                }
            }
        }

        return faults;
    }

    // What a box covers, in percent of the map.
    private static MapArea Area(MapSlot slot) => MapArea.Of(slot, new MapScale(1m, 1m));

    // Whether two areas share more than an edge, a hundredth of their unit left for rounding.
    private static bool Overlaps(MapArea a, MapArea b) => a.Overlaps(b, -Tolerance);

    // Every line whose width hangs past the side of a box it meets, where it leaves it and where it ends: on
    // the narrowest map, where a side is the fewest pixels. A point of the company's line or of the pocket line
    // has no side: the line runs on through it.
    private static List<string> Overhangs(IReadOnlyList<RoutedLine> routes, MapLayout.MapGrid grid)
    {
        var scale = MapScale.Of(Sizes[0].Width, RowPixels * grid.Units);
        return
        [
            .. routes.Where(route => route.From.Lever is null && route.From.PocketLine is null && Hangs(grid.Slot(route.From), route.Route.Points[0], route.Route.Width, scale))
                .Select(route => $"{route.Name} off {route.From.Id}"),
            .. routes.Where(route => route.To.Lever is null && route.To.PocketLine is null && Hangs(grid.Slot(route.To), route.Route.End, route.Route.Width, scale))
                .Select(route => $"{route.Name} off {route.To.Id}"),
        ];
    }

    // Whether a line meeting a box at a point of its edge hangs past the side the point is on, at its width.
    private static bool Hangs(MapSlot box, MapPoint at, decimal width, MapScale scale)
    {

        var half = width / 2m - Tolerance;
        return at.X == box.Left || at.X == box.Right
            ? (at.Y - box.Top) * scale.Y < half || (box.Bottom - at.Y) * scale.Y < half
            : (at.X - box.Left) * scale.X < half || (box.Right - at.X) * scale.X < half;
    }

    // Every two arrowheads that overlap, every arrowhead over another line, and every arrowhead longer than
    // the stretch of line it ends, on the narrowest map: an arrowhead is drawn where a line ends on a module,
    // none into a bucket.
    private static List<string> Arrowheads(IReadOnlyList<RoutedLine> routes, decimal units)
    {
        var scale = MapScale.Of(Sizes[0].Width, RowPixels * units);
        var heads = routes.Where(route => MapBundle.EndsInArrowhead(route.From, route.To))
            .Select(route => (route.Name, Area: MapLineWidths.Head(route.Route, scale), Long: MapLineWidths.ArrowLength(route.Route.Width) > Last(route.Route, scale) + Tolerance)).ToList();
        return
        [
            .. heads.Where(head => head.Long).Select(head => $"{head.Name}'s arrowhead past its last stretch"),
            .. heads.SelectMany((a, i) => heads.Skip(i + 1).Where(b => Overlaps(a.Area, b.Area)).Select(b => $"{a.Name}'s arrowhead over {b.Name}'s")),
            .. heads.SelectMany(head => routes.Where(route => route.Name != head.Name)
                .Where(route => MapLineWidths.Strokes(route.Route, scale).Any(stroke => Overlaps(head.Area, stroke.Area)))
                .Select(route => $"{head.Name}'s arrowhead over {route.Name}")),
        ];
    }

    // How long the last stretch of a line is, in pixels.
    private static decimal Last(MapRoute route, MapScale scale) =>
        Math.Abs(route.Points[^1].X - route.Points[^2].X) * scale.X + Math.Abs(route.Points[^1].Y - route.Points[^2].Y) * scale.Y;

    // A line of money as large as the run's largest, from a module to an end.
    internal static MapLink Money(RiverModule module, MapEnd to) =>
        new(to.Bucket is null ? MapLinkKind.PassedOn : MapLinkKind.IntoBucket, MapEnd.Of(module), to, module, new MapLabel(MapText.Map, "Money", []), 100_000m);

    // The plans at the edges of what a plan holds, by name.
    public static TheoryData<string> ExtremeRivers() => new(Extremes);

    internal static readonly string[] Extremes =
        ["the pay at its ceiling", "a low pay", "a royalty past its limits", "the costly car", "a car costly to run", "the company skipped, a high pay", "the company skipped, a low pay"];

    // Plans where the company pays its corporate tax and the dividend its withholding, with the refund on its
    // exempt part, by name.
    public static TheoryData<string> DividendTaxedRivers() => new("every lever", "the example", "a low pay");

    // Plans by name, each with the map's height in chain rows the band leaves it.
    public static TheoryData<string, decimal> BandHeights() => new()
    {
        { "every lever", 13.73m },
        { "the example", 6.58m },
        { "the company skipped", 3.1m },
        { "the company skipped, the partner in the plan", 4.4m },
    };

    // A plan by name: every lever, the example, every lever with the company skipped, or one at the edges.
    private static RiverInputs Plan(string plan) => plan switch
    {
        "every lever" => MapLinksTests.Full,
        "the example" => RiverInputs.Example,
        "the company skipped" => MapLinksTests.Full.SkipCompany().TakeOutOfPlan(OptionalModule.Household),
        "the company skipped, the partner in the plan" => MapLinksTests.Full.SkipCompany(),
        _ => Extreme(plan),
    };

    internal static RiverInputs Extreme(string plan) => plan switch
    {
        "the pay at its ceiling" => MapLinksTests.Full.WithDirectorPay(10_000_000m),
        "a low pay" => MapLinksTests.Full.WithDirectorPay(12_000m),
        "a royalty past its limits" => MapLinksTests.Full.WithRoyalty(60_000m),
        "the costly car" => MapViewConditionsTests.CostlyCar,
        "a car costly to run" => MapLinksTests.Full.WithCarCosts(40_000m),
        "the company skipped, a high pay" => MapLinksTests.Full.SkipCompany().WithDirectorPay(200_000m),
        "the company skipped, a low pay" => MapLinksTests.Full.SkipCompany().WithDirectorPay(12_000m),
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, "A plan the tests have no inputs for."),
    };

    // Whether a segment, across or down, runs through the inside of a box, its edges left out.
    private static bool Enters((MapPoint A, MapPoint B) segment, MapSlot box)
    {
        var (left, right) = (Math.Min(segment.A.X, segment.B.X), Math.Max(segment.A.X, segment.B.X));
        var (top, bottom) = (Math.Min(segment.A.Y, segment.B.Y), Math.Max(segment.A.Y, segment.B.Y));
        return left == right
            ? left > box.Left && left < box.Right && Math.Max(top, box.Top) < Math.Min(bottom, box.Bottom)
            : top > box.Top && top < box.Bottom && Math.Max(left, box.Left) < Math.Min(right, box.Right);
    }

    // Whether two segments lie on one line and overlap along a stretch of it.
    private static bool Shared((MapPoint A, MapPoint B) a, (MapPoint A, MapPoint B) b)
    {
        if (a.A.X == a.B.X && b.A.X == b.B.X && a.A.X == b.A.X)
            return Math.Max(Math.Min(a.A.Y, a.B.Y), Math.Min(b.A.Y, b.B.Y)) < Math.Min(Math.Max(a.A.Y, a.B.Y), Math.Max(b.A.Y, b.B.Y));
        if (a.A.Y == a.B.Y && b.A.Y == b.B.Y && a.A.Y == b.A.Y)
            return Math.Max(Math.Min(a.A.X, a.B.X), Math.Min(b.A.X, b.B.X)) < Math.Min(Math.Max(a.A.X, a.B.X), Math.Max(b.A.X, b.B.X));
        return false;
    }

    // Whether a line meets its end: on an edge of a box or a bucket, or on the stretch of the company's line or
    // of the pocket line where its point is, a line joining it or leaving it.
    private static bool OnEdge(MapLayout.MapGrid grid, MapEnd end, MapPoint point)
    {
        var box = grid.Slot(end);
        if (end.Lever is not null || end.PocketLine is not null)
            return point.X >= box.Left && point.X <= box.Right && point.Y >= box.Top && point.Y <= box.Bottom;

        var withinX = point.X >= box.Left && point.X <= box.Right;
        var withinY = point.Y >= box.Top && point.Y <= box.Bottom;
        return (withinX && (point.Y == box.Top || point.Y == box.Bottom)) || (withinY && (point.X == box.Left || point.X == box.Right));
    }

    // A line ends on its end's box.
    private static bool EndsOn(MapLayout.MapGrid grid, MapEnd to, MapPoint point) => OnEdge(grid, to, point);
}
