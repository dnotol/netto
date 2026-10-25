using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the Map tab draws of one run of the river: the modules the river shows with figures, the four
// buckets of the bar whatever the plan holds, and the run's links with their routes; none while an amount
// is refused, the module holding it marked. A filter shows some of the links and hides the rest without
// moving anything, and a module chosen off the map is none.
public class MapViewTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    [Fact]
    public void From_theExample_drawsItsModulesInTheirSlotsAndTheFourBuckets()
    {
        var view = MapView.From(RiverInputs.Example, RiverInputs.Example.Run());

        view.ShouldSatisfyAllConditions(
            () => view.Modules.Select(node => node.End.Id).ShouldBe(["company-year", "split", "director-pay", "corporate-tax", "dividend"]),
            () => view.Buckets.Select(node => node.End.Id).ShouldBe(BucketsView.Keys),
            () => view.Units.ShouldBe(6.1m));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void From_anyPlan_drawsTheBarsFourBuckets(bool refused, bool companySkipped)
    {
        var river = refused ? Full.WithReserve(-1m) : Full;
        river = companySkipped ? river.SkipCompany() : river;

        MapView.From(river, river.Run()).Buckets.Select(node => node.End.Id).ShouldBe(BucketsView.Keys);
    }

    // The company skipped: Company year stays, marked skipped, with no link to or from it; Director pay keeps
    // its lines into the buckets, the river starting at the pay.
    [Fact]
    public void From_companySkipped_drawsCompanyYearSkippedWithNoLinkAndDirectorPayWithItsLines()
    {
        var river = Full.SkipCompany();

        var view = MapView.From(river, river.Run());

        view.ShouldSatisfyAllConditions(
            () => view.Modules.Select(node => (node.End.Id, node.Skipped)).ShouldBe([("company-year", true), ("director-pay", false)]),
            () => view.Links.ShouldNotBeEmpty(),
            () => view.Links.Where(link => link.Touches(RiverModule.CompanyYear)).ShouldBeEmpty(),
            () => view.Links.ShouldAllBe(link => link.Touches(RiverModule.DirectorPay)),
            () => view.Links.ShouldAllBe(link => link.Route != null));
    }

    [Fact]
    public void From_companyEntered_marksNoModuleSkipped()
    {
        MapView.From(Full, Full.Run()).Modules.Any(node => node.Skipped).ShouldBeFalse();
    }

    [Fact]
    public void From_everyLever_routesEveryLinkOfTheRun()
    {
        var view = MapView.From(Full, Full.Run());

        (view.Links.Count, view.Links.All(link => link.Route is not null)).ShouldBe(
            (MapLinks.Of(Full, Full.Run()).Count + MapConditions.From(YearConditions.Of(Full.Run().Year!), MapView.ModulesOf(Full)).Count, true));
    }

    // A refused amount leaves the map without a figure, as it leaves the bar: its modules and buckets
    // drawn, no link, and the module holding the refused field marked.
    [Fact]
    public void From_aRefusedAmount_drawsNoLinkAndMarksTheModuleHoldingIt()
    {
        var river = Full.WithReserve(-1m);

        var view = MapView.From(river, river.Run());

        view.ShouldSatisfyAllConditions(
            () => (view.Links.Count, view.Refusal, view.Modules.Count).ShouldBe((0, river.Run().Error, MapView.ModulesOf(river).Count)),
            () => view.Modules.Where(node => node.Refused).Select(node => node.End.Module).ShouldBe([RiverModule.Dividend]));
    }

    // The income year no card holds: the refusal is said above the map, no module is marked.
    [Fact]
    public void From_aRefusalNoCardHolds_marksNoModule()
    {
        var river = RiverInputs.Example with { IncomeYear = Fixtures.UnsupportedIncomeYear };

        var view = MapView.From(river, river.Run());

        (view.Refusal is not null, view.Modules.Any(node => node.Refused), view.Links.Count).ShouldBe((true, false, 0));
    }

    [Fact]
    public void From_aRent_namesTheTaxesOnPayWithTheRentAsTheBarDoes()
    {
        (MapView.From(Full, Full.Run()).TaxesOnPayIncludeRent, MapView.From(RiverInputs.Example, RiverInputs.Example.Run()).TaxesOnPayIncludeRent)
            .ShouldBe((true, false));
    }

    [Fact]
    public void LinksOf_aModule_isEveryLinkWithItAtOneEnd()
    {
        var view = MapView.From(Full, Full.Run());

        view.LinksOf(RiverModule.Car).ShouldBe(view.Links.Where(link => link.From.Module == RiverModule.Car || link.To.Module == RiverModule.Car));
    }

    [Fact]
    public void LinksOf_aModule_holdsWhatComesInAndWhatGoesOut()
    {
        var view = MapView.From(Full, Full.Run());

        var car = view.LinksOf(RiverModule.Car).ToList();

        (car.Any(link => link.To.Module == RiverModule.Car && link.From.Module == RiverModule.Rent), car.Any(link => link.From == MapEnd.InBucket("pocket")),
                car.Any(link => link.To.Module == RiverModule.Split))
            .ShouldBe((true, true, true));
    }

    [Fact]
    public void Choice_aModuleOnTheMap_isIt()
    {
        MapView.From(Full, Full.Run()).Choice(RiverModule.Car).ShouldBe(RiverModule.Car);
    }

    // A lever taken out of the plan leaves the map, and its choice with it.
    [Fact]
    public void Choice_aModuleOffTheMap_isNone()
    {
        MapView.From(RiverInputs.Example, RiverInputs.Example.Run()).Choice(RiverModule.Car).ShouldBeNull();
    }

    [Fact]
    public void Choice_none_isNone()
    {
        MapView.From(Full, Full.Run()).Choice(null).ShouldBeNull();
    }

    // A filter shows some links and hides the others: the modules stay where they are, and so does every
    // line it shows; what it hides is still each module's, out of sight.
    [Fact]
    public void With_someLinksHidden_movesNothing()
    {
        var view = MapView.From(Full, Full.Run());

        var filtered = view with
        {
            Links = [.. view.Links.Where(link => link.Kind != MapLinkKind.IntoBucket)],
            Hidden = [.. view.Links.Where(link => link.Kind == MapLinkKind.IntoBucket)],
        };

        filtered.ShouldSatisfyAllConditions(
            () => filtered.Modules.ShouldBe(view.Modules),
            () => filtered.Links.Select(link => link.Route).ShouldBe(view.Links.Where(link => link.Kind != MapLinkKind.IntoBucket).Select(link => link.Route)),
            () => filtered.HiddenOf(RiverModule.DirectorPay).ShouldBe(view.LinksOf(RiverModule.DirectorPay).Where(link => link.Kind == MapLinkKind.IntoBucket)));
    }

    // Links of the same ends and kind are one line, drawn once with all their words.
    [Fact]
    public void Bundles_linksSharingARoute_areOneLine()
    {
        var view = MapView.From(Full, Full.Run());

        var bundles = view.Bundles();

        (bundles.Count, bundles.Sum(bundle => bundle.Links.Count)).ShouldBe((view.Links.Select(link => link.Route).Distinct().Count(), view.Links.Count));
    }

    // Each line's words are drawn once: with the other lines its card draws on that side of its box, or
    // beside it in the gap between two boxes.
    [Fact]
    public void Blocks_everyLine_isWordedOnceBesideItsCard()
    {
        var view = MapView.From(Full, Full.Run());

        var blocks = view.Blocks();

        blocks.ShouldSatisfyAllConditions(
            () => blocks.SelectMany(block => block.Bundles).Select(bundle => bundle.Route).ShouldBe(view.Bundles().Select(bundle => bundle.Route), ignoreOrder: true),
            () => blocks.Where(block => block.Side == MapLabelSide.Gap).ShouldAllBe(block => block.Bundles.Count == 1),
            () => blocks.Where(block => block.Side != MapLabelSide.Gap).ShouldAllBe(block => block.Bundles.All(bundle => bundle.Card == block.Card)));
    }

    [Fact]
    public void Blocks_aCardsLeftSide_endsLeftOfItsBox()
    {
        var view = MapView.From(Full, Full.Run());
        var rent = view.Modules.Single(node => node.End.Module == RiverModule.Rent).Slot;

        var block = view.Blocks().Single(block => block.Card == RiverModule.Rent && block.Side == MapLabelSide.Left);

        (block.At.X < rent.Left, block.At.Y).ShouldBe((true, rent.CenterY));
    }

    [Fact]
    public void Blocks_aCardsRightSide_startsRightOfItsBox()
    {
        var view = MapView.From(Full, Full.Run());
        var car = view.Modules.Single(node => node.End.Module == RiverModule.Car).Slot;

        var block = view.Blocks().Single(block => block.Card == RiverModule.Car && block.Side == MapLabelSide.Right);

        (block.At.X > car.Right, block.At.Y).ShouldBe((true, car.CenterY));
    }

    // Choosing a module brings its own lines out, the others fading, and draws them last, over the rest.
    [Fact]
    public void Drawn_aModuleChosen_fadesTheOtherLinesAndDrawsItsOwnLast()
    {
        var view = MapView.From(Full, Full.Run());

        var drawn = view.Drawn(RiverModule.Car);

        drawn.ShouldSatisfyAllConditions(
            () => drawn.Where(line => !line.Faded).Select(line => line.Bundle).ShouldAllBe(bundle => bundle.Touches(RiverModule.Car)),
            () => drawn.Where(line => line.Faded).Select(line => line.Bundle).ShouldAllBe(bundle => !bundle.Touches(RiverModule.Car)),
            () => drawn.SkipWhile(line => line.Faded).ShouldAllBe(line => !line.Faded));
    }

    [Fact]
    public void Drawn_nothingChosen_fadesNothing()
    {
        MapView.From(Full, Full.Run()).Drawn(null).ShouldAllBe(line => !line.Faded);
    }
}
