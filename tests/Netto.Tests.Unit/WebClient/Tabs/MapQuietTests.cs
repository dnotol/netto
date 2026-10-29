using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The levers that move no money of their own in a plan, as the map reads them from its own links: a lever
// in the plan whose every link is a stretch of the company's line beside it, or the royalty with no link at
// all, is quiet. Consecutive quiet levers share one quiet pill among the gates, the company's line runs on past
// them in its own column with its amount once, and a lever gets its box back, in its place, once it moves
// money. A refused run folds nothing, and a plan whose levers all move money draws as it always did.
public class MapQuietTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // Every lever in the plan and none with an amount: the example's inputs for each.
    private static readonly RiverInputs EveryLeverEmpty = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module));

    private static readonly RiverModule[] Chain = [RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.Car];

    public static TheoryData<OptionalModule> LeversMovingNothingOnTheirOwn => new(OptionalModule.Vouchers, OptionalModule.Rent, OptionalModule.Car, OptionalModule.Copyright);

    // A river with some of its levers emptied: each as the example holds it, with no amount, still in the
    // plan. The royalty then follows the pay with no creative share, so it comes to nothing; the partner, whom
    // the example holds no income for, is taxed separately, with no marital quotient.
    internal static RiverInputs Emptied(RiverInputs river, params OptionalModule[] levers) => levers.Aggregate(river, (inputs, lever) => lever switch
    {
        OptionalModule.Vouchers => inputs with { Vouchers = RiverInputs.Example.Vouchers },
        OptionalModule.Allowances => inputs with { Allowances = RiverInputs.Example.Allowances },
        OptionalModule.Rent => inputs with { Rent = RiverInputs.Example.Rent },
        OptionalModule.Car => inputs with { Car = RiverInputs.Example.Car },
        OptionalModule.Copyright => inputs with { Copyright = RiverInputs.Example.Copyright },
        OptionalModule.Household => inputs.WithTaxedSeparately(true),
        _ => throw new ArgumentOutOfRangeException(nameof(levers), lever, null),
    });

    [Fact]
    public void From_everyLeverEmpty_drawsEveryLeverQuiet()
    {
        var view = MapView.From(EveryLeverEmpty, EveryLeverEmpty.Run());

        Quiet(view).ShouldBe([RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.Car, RiverModule.Copyright]);
    }

    [Theory]
    [MemberData(nameof(LeversMovingNothingOnTheirOwn))]
    public void From_aLeverEmptied_drawsItAloneQuiet(OptionalModule lever)
    {
        var river = Emptied(Full, lever);

        Quiet(MapView.From(river, river.Run())).ShouldBe([MapModules.Of(lever)]);
    }

    // A royalty fixed at nothing comes to nothing, as one following the pay with no creative share does.
    [Fact]
    public void From_aRoyaltyFixedAtNothing_drawsItQuiet()
    {
        var river = Full.WithRoyalty(0m);

        Quiet(MapView.From(river, river.Run())).ShouldBe([RiverModule.Copyright]);
    }

    // The royalty is quiet in its own place, a slot as wide as its box and centred on it, and nothing runs to
    // or from it.
    [Fact]
    public void From_aRoyaltyComingToNothing_isQuietInItsOwnPlaceWithNoLink()
    {
        var river = Emptied(Full, OptionalModule.Copyright);

        var view = MapView.From(river, river.Run());

        var grid = MapLayout.Grid(MapView.ModulesOf(river), badgeLines: BadgeLines(view), gateLines: GateLines(view));
        var box = grid.Slot(MapEnd.Of(RiverModule.Copyright));
        var slot = Node(view, RiverModule.Copyright).Slot;
        view.ShouldSatisfyAllConditions(
            () => view.LinksOf(RiverModule.Copyright).ShouldBeEmpty(),
            () => (slot.Left, slot.Right, Rows(slot.CenterY, view.Units)).ShouldBe((box.Left, box.Right, Rows(box.CenterY, grid.Units))));
    }

    // The partner taxed separately moves no tax base: quiet in its own place, over Director pay's right, with no
    // line, its pill as high as its one word takes.
    [Fact]
    public void From_aPartnerTaxedSeparately_isQuietInItsOwnPlaceWithNoLink()
    {
        var river = Emptied(Full, OptionalModule.Household);

        var view = MapView.From(river, river.Run());

        var grid = MapLayout.Grid(MapView.ModulesOf(river), badgeLines: BadgeLines(view), gateLines: GateLines(view));
        var box = grid.Slot(MapEnd.Of(RiverModule.Household));
        var node = Node(view, RiverModule.Household);
        view.ShouldSatisfyAllConditions(
            () => node.Quiet.ShouldBeTrue(),
            () => view.LinksOf(RiverModule.Household).ShouldBeEmpty(),
            () => (node.Slot.Left, node.Slot.Right, Rows(node.Slot.CenterY, view.Units)).ShouldBe((box.Left, box.Right, Rows(box.CenterY, grid.Units))));
    }

    // The dependants raise a tax-free amount, taxed at its own rates, which is neither money nor a tax base:
    // no line of theirs. A household declaring them still does something in this plan, so it keeps its box,
    // with no partner or with one taxed separately alike, and says nothing it does not do.
    public static TheoryData<string, RiverInputs> HouseholdsWithDependantsAndNoLine => new()
    {
        { "a single parent", Full.WithPartner(false).WithChildren(2).WithSingleParent(true) },
        { "a partner taxed separately, a child", Emptied(Full, OptionalModule.Household).WithChildren(1) },
        { "no partner, a dependant in care", Full.WithPartner(false).WithDependantsInCare(1) },
    };

    [Theory]
    [MemberData(nameof(HouseholdsWithDependantsAndNoLine))]
    public void From_aHouseholdDeclaringDependants_keepsItsBoxWithNoLine(string _, RiverInputs river)
    {
        var view = MapView.From(river, river.Run());

        view.ShouldSatisfyAllConditions(
            () => Node(view, RiverModule.Household).Quiet.ShouldBeFalse(),
            () => view.LinksOf(RiverModule.Household).ShouldBeEmpty());
    }

    // With no partner and no dependant the household does nothing in this plan: quiet, the single parent's
    // tick, with no child, declaring nobody.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void From_aHouseholdWithNoPartnerNorDependant_isQuiet(bool singleParentTicked)
    {
        var river = Full.WithPartner(false).WithSingleParent(singleParentTicked);

        Node(MapView.From(river, river.Run()), RiverModule.Household).Quiet.ShouldBeTrue();
    }

    // The office allowance is a rule the rent sets on the allowances: while a rent is paid it is a link of
    // Expense allowances, so an empty one keeps its box beside it.
    [Fact]
    public void From_emptyAllowancesBesideAPaidRent_keepsTheirBox()
    {
        var river = Emptied(Full, OptionalModule.Allowances);

        var view = MapView.From(river, river.Run());

        view.ShouldSatisfyAllConditions(
            () => view.LinksOf(RiverModule.Allowances).ShouldContain(link => link.Kind == MapLinkKind.Condition),
            () => Quiet(view).ShouldBeEmpty());
    }

    [Fact]
    public void From_emptyAllowancesWithNoRent_drawsThemQuiet()
    {
        var river = Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent);

        Quiet(MapView.From(river, river.Run())).ShouldBe([RiverModule.Allowances, RiverModule.Rent]);
    }

    // A refused run has no link to read what its levers move: it folds nothing, and the lever holding the
    // refused field keeps its box, marked.
    [Fact]
    public void From_aLeverRefused_foldsNothingAndMarksIt()
    {
        var river = Emptied(Full, OptionalModule.Vouchers, OptionalModule.Rent).WithCatalogueValue(-1m);

        var view = MapView.From(river, river.Run());

        view.ShouldSatisfyAllConditions(
            () => Quiet(view).ShouldBeEmpty(),
            () => view.Modules.Where(node => node.Refused).Select(node => node.End.Module).ShouldBe([RiverModule.Car]));
    }

    [Fact]
    public void From_aRefusalElsewhere_foldsNothing()
    {
        var river = Emptied(Full, OptionalModule.Vouchers, OptionalModule.Rent).WithReserve(-1m);

        Quiet(MapView.From(river, river.Run())).ShouldBeEmpty();
    }

    // Every lever moving money, in every composition: nothing is quiet, and the map is the one drawn without
    // a quiet lever, its boxes holding the same badges, the same slots, the same height and the same links
    // on the same routes.
    [Theory]
    [MemberData(nameof(MapModulesTests.Compositions), MemberType = typeof(MapModulesTests))]
    public void From_everyLeverMovingMoney_foldsNothing(int bits)
    {
        var river = Full with { Composition = MapModulesTests.Composition(bits) };
        var run = river.Run();
        var modules = MapView.ModulesOf(river);
        IReadOnlyList<MapLink> links = [.. MapLinks.Of(river, run), .. MapConditions.From(YearConditions.Of(run.Year!), modules)];
        var grid = MapLayout.Grid(modules, badgeLines: MapBadges.Lines(MapBadges.Of(links)), gateLines: MapLayoutTests.GateLines(river));
        var unfolded = MapLayout.Routed(grid, MapPocketLine.Of(links));

        var view = MapView.From(river, run);

        view.ShouldSatisfyAllConditions(
            () => Quiet(view).ShouldBeEmpty(),
            () => view.Modules.Select(node => node.Slot).ShouldBe(modules.Select(module => grid.Slot(MapEnd.Of(module)))),
            () => view.Units.ShouldBe(grid.Units),
            () => view.Links.Select(Line).ShouldBe(unfolded.Select(Line)));
    }

    // The company's line past quiet levers: one stretch, what the lever above them leaves, its words, card and
    // amount, on to the split, naming those it runs past; their own stretches are not drawn, and no link ends on
    // a quiet lever or at the point of the line beside it.
    [Fact]
    public void From_quietLeversOnTheCompanysLine_runTheLineAboveThemOnPastThemNamingThem()
    {
        var river = Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);
        var handOff = MapLinks.Of(river, river.Run()).Single(link => link.From == MapEnd.Beside(RiverModule.Vouchers) && link.To.Lever is not null);

        var view = MapView.From(river, river.Run());

        var line = MapLinksTests.CompanysLine(view.Links).ToList();
        view.ShouldSatisfyAllConditions(
            () => line.Select(link => (link.From.Id, link.To.Id)).ShouldBe([("company-year", "company-line-vouchers"), ("company-line-vouchers", "split")]),
            () => (line[1].Card, line[1].Carries, line[1].Amount).ShouldBe((handOff.Card, handOff.Carries, handOff.Amount)),
            () => line[1].Through.ShouldBe([RiverModule.Allowances, RiverModule.Rent, RiverModule.Car]),
            () => view.Links.Where(link => Quiet(view).Any(lever => new[] { link.From, link.To }.Any(end => end == MapEnd.Of(lever) || end.Lever == lever))).ShouldBeEmpty());
    }

    // Every lever empty: the chain is Company year's line to the split, as the map of a plan without a lever
    // draws it, past every lever of the chain.
    [Fact]
    public void From_everyLeverEmpty_runsCompanyYearsLineToTheSplitPastEveryLever()
    {
        var bare = MapLinks.Of(RiverInputs.Example, RiverInputs.Example.Run()).Single(link => link.From == MapEnd.Of(RiverModule.CompanyYear) && link.Kind == MapLinkKind.PassedOn);

        var line = MapView.From(EveryLeverEmpty, EveryLeverEmpty.Run()).Links.Single(link => link.From == MapEnd.Of(RiverModule.CompanyYear) && link.Kind == MapLinkKind.PassedOn);

        line.ShouldSatisfyAllConditions(
            () => (line.To, line.Card, line.Carries, line.Amount).ShouldBe((bare.To, bare.Card, bare.Carries, bare.Amount)),
            () => line.Through.ShouldBe(Chain));
    }

    // Quiet levers following one another down the chain share one place, its slot in the row of the first;
    // a lever with a box between them parts them, each run in its own place.
    [Fact]
    public void Places_quietLeversFollowingOneAnother_shareOnePlaceAndABoxPartsThem()
    {
        var together = Emptied(Full, OptionalModule.Vouchers, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);
        var parted = together with { Allowances = Full.Allowances };

        var (one, two) = (Places(together), Places(parted));

        one.ShouldSatisfyAllConditions(
            () => one.ShouldBe(["company-year", "vouchers+allowances+rent+car quiet", "split", "copyright", "director-pay", "household", "corporate-tax", "dividend"]),
            () => two.ShouldBe(["company-year", "vouchers quiet", "allowances", "rent+car quiet", "split", "copyright", "director-pay", "household", "corporate-tax", "dividend"]));
    }

    // A lever that moves money again gets its box back in its place: its row, as the map draws it with no
    // quiet lever around it.
    [Fact]
    public void From_aQuietLeverGivenItsAmounts_getsItsBoxBackInItsPlace()
    {
        var river = Emptied(Full, OptionalModule.Vouchers, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car) with { Allowances = Full.Allowances };

        var view = MapView.From(river, river.Run());

        var (vouchers, allowances, rent) = (Node(view, RiverModule.Vouchers).Slot, Node(view, RiverModule.Allowances).Slot, Node(view, RiverModule.Rent).Slot);
        (Node(view, RiverModule.Allowances).Quiet, vouchers.Bottom < allowances.Top, allowances.Bottom < rent.Top, allowances.Width).ShouldBe(
            (false, true, true, MapLayout.Grid(MapView.ModulesOf(river)).Slot(MapEnd.Of(RiverModule.Allowances)).Width));
    }

    // A quiet lever's links are the company's line running past it: chosen, that line stays with its amount, on
    // the line in the company's column, never in the quiet pill among the gates.
    [Fact]
    public void LinksOf_aQuietLever_isTheCompanysLineRunningPastIt()
    {
        var river = Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);
        var view = MapView.From(river, river.Run());
        var pill = Node(view, RiverModule.Rent).Slot;

        var past = view.LinksOf(RiverModule.Rent).ShouldHaveSingleItem();
        var amount = view.Amounts(MapAmounts.Of(view, MapAmountsTests.Say), RiverModule.Rent).ShouldHaveSingleItem();

        view.ShouldSatisfyAllConditions(
            () => (past.From, past.To).ShouldBe((MapEnd.Beside(RiverModule.Vouchers), MapEnd.Of(RiverModule.Split))),
            () => view.Drawn(RiverModule.Rent).Where(line => !line.Faded).Select(line => line.Bundle.Links[0]).ShouldBe([past]),
            () => amount.Bundle.Links[0].ShouldBe(past),
            () => (amount.At.X > pill.Right).ShouldBeTrue());
    }

    // The company's line past quiet levers runs straight down its own column, from the point beside the lever
    // above them into the split, clear of their pill among the gates.
    [Fact]
    public void Routed_theCompanysLinePastQuietLevers_runsStraightDownClearOfTheirPill()
    {
        var river = Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);
        var view = MapView.From(river, river.Run());
        var (pill, split) = (Node(view, RiverModule.Rent).Slot, Node(view, RiverModule.Split).Slot);

        var route = view.Links.Single(link => link.Through.Count > 0).Route!;

        route.ShouldSatisfyAllConditions(
            () => route.Points.Select(point => point.X).Distinct().ShouldHaveSingleItem(),
            () => (route.Points[0].X > pill.Right, route.End.Y).ShouldBe((true, split.Top)));
    }

    // A quiet pill is lower than the gates it replaces, the boxes holding the same badges and the other gates the
    // same lines: three levers take one pill, their names one to a line, under the rows they would take.
    [Fact]
    public void From_quietLevers_drawALowerMap()
    {
        var river = Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);

        var view = MapView.From(river, river.Run());

        view.Units.ShouldBeLessThan(MapLayout.Grid(MapView.ModulesOf(river), badgeLines: BadgeLines(view), gateLines: GateLines(view)).Units);
    }

    // The quiet levers ending the chain above the split, one to four of them.
    public static TheoryData<int> QuietRuns => new(1, 2, 3, 4);

    // The map is never less than its smallest scale, a chain row being 3.25rem (MapLayout.SmallestRowRem),
    // whatever its width: there, a quiet pill's names, each a 24 px target, take a line each on the narrowest
    // map, a column 34rem wide where a gate is 124 px; its pill is kept that tall, with its padding, in the
    // middle of its row, below the box above, and the amount of the company's line past it is clear of it and
    // of the boxes.
    [Theory]
    [MemberData(nameof(QuietRuns))]
    public void From_aQuietRowAtTheMapsSmallestScale_holdsItsNamesOnePerLineClearOfTheBoxesAndTheLinesAmount(int count)
    {
        var river = Emptied(Full, [.. Chain.TakeLast(count).Select(module => RiverModules.Lever(module)!.Value)]);
        var view = MapView.From(river, river.Run());
        var rem = 100m / (view.Units * MapLayout.SmallestRowRem);
        var (pill, split) = (Node(view, RiverModule.Car).Slot, Node(view, RiverModule.Split).Slot);
        var above = view.Modules.Last(node => !node.Quiet && node.Slot.Bottom <= pill.Top).Slot;
        var scale = MapScale.Smallest(view.Units);

        var amount = view.Amounts(MapAmounts.Of(view, MapAmountsTests.Say), RiverModule.Car).Single().Area(scale);

        view.ShouldSatisfyAllConditions(
            () => (pill.Height >= (count * MapLayout.QuietTargetRem + (count - 1) * MapLayout.QuietGapRem + 2 * MapLayout.QuietPadRem) * rem).ShouldBeTrue(),
            () => (pill.Top > above.Bottom).ShouldBeTrue(),
            () => new[] { pill, split, above }.Where(box => MapArea.Of(box, scale).Overlaps(amount)).ShouldBeEmpty());
    }

    // What the stylesheet draws a quiet row with is what the layout keeps room for: the map's smallest
    // scale, a quiet name's least height and the gap between two lines of them, the pill's padding.
    [Fact]
    public void MapTabCss_drawsAQuietRowWithTheRoomTheLayoutKeepsForIt()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css");

        css.ShouldSatisfyAllConditions(
            () => Stylesheet.Block(css, ".map").ShouldContain($"min-height: calc(var(--map-units) * {Stylesheet.Rem(MapLayout.SmallestRowRem)});"),
            () => Stylesheet.Block(css, ".map-module--quiet").ShouldContain($"min-height: {Stylesheet.Rem(MapLayout.QuietTargetRem)};"),
            () => Stylesheet.Block(css, ".map-quiet").ShouldContain($"row-gap: {Stylesheet.Rem(MapLayout.QuietGapRem)};"),
            () => Stylesheet.Block(css, ".map-quiet").ShouldContain($"padding: {Stylesheet.Rem(MapLayout.QuietPadRem)} 0.6rem;"),
            () => Stylesheet.Block(css, ".map-quiet").ShouldContain("font-size: 0.72rem;"),
            () => Stylesheet.Block(css, ".map-quiet").ShouldContain("line-height: 1.2;"),
            () => MapLayout.QuietLineRem.ShouldBe(0.72m * 1.2m),
            () => Stylesheet.Block(css, ".map-quiet").ShouldContain("border: 1px dashed"));
    }

    private static RiverModule[] Quiet(MapView view) => [.. view.Modules.Where(node => node.Quiet).Select(node => node.End.Module!.Value)];

    private static MapNode Node(MapView view, RiverModule module) => view.Modules.Single(node => node.End.Module == module);

    // How many lines of badges each module's box holds on a map, and how many lines each gate's says.
    private static Dictionary<RiverModule, int> BadgeLines(MapView view) => view.Modules.ToDictionary(node => node.End.Module!.Value, node => node.Badges.Count);

    private static Dictionary<RiverModule, int> GateLines(MapView view) => view.Modules.ToDictionary(node => node.End.Module!.Value, node => node.Lines.Count);

    // Where a slot of a map stands down it, in chain rows from its top, to the ten-thousand-millionth.
    private static decimal Rows(decimal percent, decimal units) => Math.Round(percent * units / 100m, 10);

    private static string[] Places(RiverInputs river) =>
        [.. MapView.From(river, river.Run()).Places().Select(place => string.Join('+', place.Nodes.Select(node => node.End.Id)) + (place.Quiet ? " quiet" : ""))];

    private static string Line(MapLink link) =>
        $"{link.Kind} {link.From.Id} {link.To.Id} {link.Amount} {link.Through.Count} {string.Join(' ', link.Route!.Points)}";
}
