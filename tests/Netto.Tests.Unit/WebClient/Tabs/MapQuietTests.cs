using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The levers that move no money of their own in a plan, as the map reads them from its own links: a lever
// in the plan whose every link is a hand-off along the chain, or the royalty with no link at all, is quiet.
// Consecutive quiet levers share one quiet row, the chain's line runs through it with its amount once, into
// the next module with a box, and a lever gets its box back, in its place, once it moves money. A refused
// run folds nothing, and a plan whose levers all move money draws as it always did.
public class MapQuietTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // Every lever in the plan and none with an amount: the example's inputs for each.
    private static readonly RiverInputs EveryLeverEmpty = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module));

    private static readonly RiverModule[] Chain = [RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.Car];

    public static TheoryData<OptionalModule> LeversMovingNothingOnTheirOwn => new(OptionalModule.Vouchers, OptionalModule.Rent, OptionalModule.Car, OptionalModule.Copyright);

    // A river with some of its levers emptied: each as the example holds it, with no amount, still in the
    // plan. The royalty then follows the pay with no creative share, so it comes to nothing.
    internal static RiverInputs Emptied(RiverInputs river, params OptionalModule[] levers) => levers.Aggregate(river, (inputs, lever) => lever switch
    {
        OptionalModule.Vouchers => inputs with { Vouchers = RiverInputs.Example.Vouchers },
        OptionalModule.Allowances => inputs with { Allowances = RiverInputs.Example.Allowances },
        OptionalModule.Rent => inputs with { Rent = RiverInputs.Example.Rent },
        OptionalModule.Car => inputs with { Car = RiverInputs.Example.Car },
        OptionalModule.Copyright => inputs with { Copyright = RiverInputs.Example.Copyright },
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

    // The royalty is quiet in its own place, a slot inside its box's, and nothing runs to or from it.
    [Fact]
    public void From_aRoyaltyComingToNothing_isQuietInItsOwnPlaceWithNoLink()
    {
        var river = Emptied(Full, OptionalModule.Copyright);
        var box = MapLayout.Grid(MapView.ModulesOf(river)).Slot(MapEnd.Of(RiverModule.Copyright));

        var view = MapView.From(river, river.Run());

        var slot = Node(view, RiverModule.Copyright).Slot;
        view.ShouldSatisfyAllConditions(
            () => view.LinksOf(RiverModule.Copyright).ShouldBeEmpty(),
            () => (slot.Left >= box.Left, slot.Right <= box.Right, slot.Top >= box.Top, slot.Bottom <= box.Bottom, slot.CenterY).ShouldBe((true, true, true, true, box.CenterY)));
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
    // a quiet lever, the same slots, the same height and the same links on the same routes.
    [Theory]
    [MemberData(nameof(MapModulesTests.Compositions), MemberType = typeof(MapModulesTests))]
    public void From_everyLeverMovingMoney_foldsNothing(int bits)
    {
        var river = Full with { Composition = MapModulesTests.Composition(bits) };
        var run = river.Run();
        var modules = MapView.ModulesOf(river);
        var grid = MapLayout.Grid(modules);
        var unfolded = MapLayout.Routed(grid, [.. MapLinks.Of(river, run), .. MapConditions.From(YearConditions.Of(run.Year!), modules)]);

        var view = MapView.From(river, run);

        view.ShouldSatisfyAllConditions(
            () => Quiet(view).ShouldBeEmpty(),
            () => view.Modules.Select(node => node.Slot).ShouldBe(modules.Select(module => grid.Slot(MapEnd.Of(module)))),
            () => view.Units.ShouldBe(grid.Units),
            () => view.Links.Select(Line).ShouldBe(unfolded.Select(Line)));
    }

    // The chain's line past quiet levers: one link, the hand-off of the module above them, its words, card
    // and amount, on to the next module with a box, naming those it runs past; their own hand-offs are not
    // drawn, and no link ends on a quiet lever.
    [Fact]
    public void From_quietLeversInTheChain_runTheLineAboveThemOnToTheNextBoxNamingThem()
    {
        var river = Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);
        var handOff = MapLinks.Of(river, river.Run()).Single(link => link.From == MapEnd.Of(RiverModule.Vouchers) && link.Kind == MapLinkKind.PassedOn);

        var view = MapView.From(river, river.Run());

        var chain = view.Links.Where(link => link.Kind == MapLinkKind.PassedOn && link.From.Module is { } from && (from == RiverModule.CompanyYear || Chain.Contains(from))).ToList();
        view.ShouldSatisfyAllConditions(
            () => chain.Select(link => (link.From.Module, link.To.Module)).ShouldBe([(RiverModule.CompanyYear, RiverModule.Vouchers), (RiverModule.Vouchers, RiverModule.Split)]),
            () => (chain[1].Card, chain[1].Carries, chain[1].Amount).ShouldBe((handOff.Card, handOff.Carries, handOff.Amount)),
            () => chain[1].Through.ShouldBe([RiverModule.Allowances, RiverModule.Rent, RiverModule.Car]),
            () => view.Links.Where(link => Quiet(view).Contains(link.From.Module ?? RiverModule.River) || Quiet(view).Contains(link.To.Module ?? RiverModule.River)).ShouldBeEmpty());
    }

    // Every lever empty: the chain is Company year's line to the split, as the map of a plan without a lever
    // draws it, past every lever of the chain.
    [Fact]
    public void From_everyLeverEmpty_runsCompanyYearsLineToTheSplitPastEveryLever()
    {
        var bare = MapLinks.Of(RiverInputs.Example, RiverInputs.Example.Run()).Single(link => link.From == MapEnd.Of(RiverModule.CompanyYear));

        var line = MapView.From(EveryLeverEmpty, EveryLeverEmpty.Run()).Links.Single(link => link.From == MapEnd.Of(RiverModule.CompanyYear));

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
            () => one.ShouldBe(["company-year", "vouchers+allowances+rent+car quiet", "split", "copyright", "director-pay", "corporate-tax", "dividend"]),
            () => two.ShouldBe(["company-year", "vouchers quiet", "allowances", "rent+car quiet", "split", "copyright", "director-pay", "corporate-tax", "dividend"]));
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

    // A quiet lever's links are the line running past it: chosen, that line stays and is worded, its words
    // on its last stretch, under the quiet row, never on it.
    [Fact]
    public void LinksOf_aQuietLever_isTheLineRunningPastIt()
    {
        var river = Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);
        var view = MapView.From(river, river.Run());
        var pill = Node(view, RiverModule.Rent).Slot;

        var past = view.LinksOf(RiverModule.Rent).ShouldHaveSingleItem();
        var words = view.Blocks(RiverModule.Rent).ShouldHaveSingleItem();

        view.ShouldSatisfyAllConditions(
            () => (past.From.Module, past.To.Module).ShouldBe((RiverModule.Vouchers, RiverModule.Split)),
            () => view.Drawn(RiverModule.Rent).Where(line => !line.Faded).Select(line => line.Bundle.Links[0]).ShouldBe([past]),
            () => (words.Side, words.At.Y > pill.Bottom, words.At.Y < Node(view, RiverModule.Split).Slot.Top).ShouldBe((MapLabelSide.Gap, true, true)));
    }

    // The line past a quiet row runs straight down the chain's middle, into the row and out of it, from the
    // box above to the box below, and the row is no wider than the room between the gutters each side.
    [Fact]
    public void Routed_theLinePastAQuietRow_runsStraightDownThroughIt()
    {
        var river = Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);
        var view = MapView.From(river, river.Run());
        var (vouchers, pill, split) = (Node(view, RiverModule.Vouchers).Slot, Node(view, RiverModule.Rent).Slot, Node(view, RiverModule.Split).Slot);

        var route = view.Links.Single(link => link.Through.Count > 0).Route!;

        route.ShouldSatisfyAllConditions(
            () => route.Points.ShouldAllBe(point => point.X == vouchers.CenterX),
            () => route.Points.Select(point => point.Y).ShouldBe([vouchers.Bottom, pill.Top, pill.Bottom, split.Top]),
            () => (pill.CenterX, pill.Height < vouchers.Height).ShouldBe((vouchers.CenterX, true)));
    }

    // A quiet row is lower than a lever's row: the map is shorter by what the rows it replaces took.
    [Fact]
    public void From_quietLevers_drawALowerMap()
    {
        var river = Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);

        (MapView.From(river, river.Run()).Units < MapLayout.Grid(MapView.ModulesOf(river)).Units - 2m).ShouldBeTrue();
    }

    private static RiverModule[] Quiet(MapView view) => [.. view.Modules.Where(node => node.Quiet).Select(node => node.End.Module!.Value)];

    private static MapNode Node(MapView view, RiverModule module) => view.Modules.Single(node => node.End.Module == module);

    private static string[] Places(RiverInputs river) =>
        [.. MapView.From(river, river.Run()).Places().Select(place => string.Join('+', place.Nodes.Select(node => node.End.Id)) + (place.Quiet ? " quiet" : ""))];

    private static string Line(MapLink link) =>
        $"{link.Kind} {link.From.Id} {link.To.Id} {link.Amount} {link.Through.Count} {string.Join(' ', link.Route!.Points)}";
}
