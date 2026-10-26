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

    // Links of the same ends and kind are one line, drawn once, its amount beside it, its words while a
    // module it joins is chosen.
    [Fact]
    public void Bundles_linksSharingARoute_areOneLine()
    {
        var view = MapView.From(Full, Full.Run());

        var bundles = view.Bundles();

        (bundles.Count, bundles.Sum(bundle => bundle.Links.Count)).ShouldBe((view.Links.Select(link => link.Route).Distinct().Count(), view.Links.Count));
    }

    // A line holding several of its card's lines carries their sum: the vouchers into the pocket, each a
    // line of the vouchers' card, one line of the map.
    [Fact]
    public void Bundles_aLineOfSeveral_carriesTheSumOfItsCardsFigures()
    {
        var run = Full.Run();
        var card = VoucherView.From(run.Year!, Full.Composition).Drains.Where(drain => drain.To == "pocket").ToList();

        var line = Line(MapView.From(Full, run), MapEnd.Of(RiverModule.Vouchers), MapEnd.InBucket("pocket"));

        (card.Count > 1, line.Links.Count, line.Amount).ShouldBe((true, card.Count, card.Sum(drain => drain.Amount)));
    }

    // The sum is of the figures as their card draws them, each to the cent, so it is the sum a viewer makes
    // of the card's lines.
    [Fact]
    public void Amount_ofLinesWithFractionsOfACent_isTheSumOfTheirFiguresToTheCent()
    {
        var line = new MapBundle(new MapRoute([new MapPoint(0m, 0m), new MapPoint(0m, 1m)], MapLabelSide.Gap),
            [Link(1.005m), Link(2.005m)]);

        line.Amount.ShouldBe(3.02m);
    }

    // A condition carries no amount: its words say whether it holds.
    [Fact]
    public void Bundles_aCondition_carriesNoAmount()
    {
        var river = MapViewConditionsTests.CostlyCar;

        MapView.From(river, river.Run()).Bundles().Where(bundle => bundle.Kind == MapLinkKind.Condition).ShouldAllBe(bundle => bundle.Amount == null);
    }

    // What comes back out of a bucket is a line of its own with its own amount, never taken off the way in:
    // the withholding refunded on the exempt dividend beside the withholding.
    [Fact]
    public void Bundles_aLineBackOutOfABucket_isItsOwnLineNeverNettedAgainstTheWayIn()
    {
        var run = Full.Run();
        var drains = DividendView.From(run.Year!).Drains.Where(drain => drain.To == "taxes_on_profit").ToList();
        var view = MapView.From(Full, run);

        var (into, back) = (Line(view, MapEnd.Of(RiverModule.Dividend), MapEnd.InBucket("taxes_on_profit")),
            Line(view, MapEnd.InBucket("taxes_on_profit"), MapEnd.Of(RiverModule.Dividend)));

        (into.Amount, back.Amount).ShouldBe((drains.Where(drain => drain.Kind == DrainKind.PaidOut).Sum(drain => drain.Amount),
            drains.Single(drain => drain.Kind == DrainKind.Back).Amount));
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

    // A card's lines beside one side of its box are in the order they meet the box, top to bottom then
    // left to right, so each amount beside the box is read with its line: the rent's into the pocket over
    // its tax into the taxes on pay, as they leave its left side.
    [Fact]
    public void Blocks_aCardsLines_areInTheOrderTheyMeetItsBox()
    {
        var view = MapView.From(Full, Full.Run());

        var rent = view.Blocks().Single(block => block.Card == RiverModule.Rent && block.Side == MapLabelSide.Left);

        rent.Bundles.Select(bundle => bundle.To.Id).ShouldBe(["pocket", "taxes_on_pay"]);
    }

    [Fact]
    public void Blocks_everyCardsLines_areInTheOrderTheyMeetItsBox()
    {
        var view = MapView.From(Full, Full.Run());

        var blocks = view.Blocks().Where(block => block.Bundles.Count > 1).ToList();

        blocks.ShouldSatisfyAllConditions(
            () => blocks.ShouldNotBeEmpty(),
            () => blocks.Where(block => !block.Bundles.Select(Meets).SequenceEqual(block.Bundles.Select(Meets).OrderBy(point => point.Y).ThenBy(point => point.X)))
                .Select(block => $"{block.Card} {block.Side}").ShouldBeEmpty());
    }

    // Nothing chosen, every line is beside its card; a module chosen, only its own lines are, whichever card
    // draws them, and a card with none of them has no words on the map.
    [Fact]
    public void Blocks_aModuleChosen_holdsOnlyItsOwnLines()
    {
        var view = MapView.From(Full, Full.Run());

        var blocks = view.Blocks(RiverModule.CorporateTax);

        blocks.ShouldSatisfyAllConditions(
            () => blocks.SelectMany(block => block.Bundles).Select(bundle => bundle.Route)
                .ShouldBe(view.Bundles().Where(bundle => bundle.Touches(RiverModule.CorporateTax)).Select(bundle => bundle.Route), ignoreOrder: true),
            () => blocks.Select(block => block.Card).ShouldContain(RiverModule.Split),
            () => blocks.ShouldAllBe(block => block.Bundles.Count > 0));
    }

    [Fact]
    public void Blocks_nothingChosen_holdsEveryLine()
    {
        var view = MapView.From(Full, Full.Run());

        view.Blocks(null).SelectMany(block => block.Bundles).Count().ShouldBe(view.Bundles().Count);
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

    // The one line of the map between two ends, of the kind money has between them.
    private static MapBundle Line(MapView view, MapEnd from, MapEnd to) =>
        view.Bundles().Single(bundle => bundle.From == from && bundle.To == to && bundle.Kind != MapLinkKind.Condition);

    // Where a line meets its card's box: where it leaves it, or where it comes back into it.
    private static MapPoint Meets(MapBundle bundle) => bundle.From.Module == bundle.Card ? bundle.Route.Points[0] : bundle.Route.End;

    private static MapLink Link(decimal amount) =>
        new(MapLinkKind.IntoBucket, MapEnd.Of(RiverModule.Vouchers), MapEnd.InBucket("pocket"), RiverModule.Vouchers, new MapLabel(MapText.Map, "Line", []), amount);
}
