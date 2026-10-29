using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the Map tab draws of one run of the river: the modules the river shows with figures, the five
// buckets of the bar whatever the plan holds, and the run's links with their routes; none while an amount
// is refused, the module holding it marked. A filter shows some of the links and hides the rest without
// moving anything, and a module chosen off the map is none.
public class MapViewTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    [Fact]
    public void From_theExample_drawsItsModulesInTheirSlotsAndTheFiveBuckets()
    {
        var view = MapView.From(RiverInputs.Example, RiverInputs.Example.Run());

        view.ShouldSatisfyAllConditions(
            () => view.Modules.Select(node => node.End.Id).ShouldBe(["company-year", "split", "director-pay", "corporate-tax", "dividend"]),
            () => view.Buckets.Select(node => node.End.Id).ShouldBe(BucketsView.Keys),
            () => view.Units.ShouldBe(ExampleUnits));
    }

    // The example's height in chain rows: Company year's row, the split's, the royalty's and Corporate tax's, 1, 1,
    // 1.6 and 1.6, and the buckets' row, its box and margin, 0.7, under the band of a whole chain row less what
    // Corporate tax's row keeps under the dividend's gate, its tallest box: the gate's name and two lines, its
    // padding and frame, 3.124rem of a 3.25rem row, a height in rows that does not end, 6.5806… in all, worked out
    // as the layout works it out, row by row.
    private static readonly decimal ExampleUnits =
        1m + 1m + 1.6m + 1.6m + (0.5m + 0.2m + Math.Max(0.2m, 1m - (1.6m - (0.85m * 1.1m + 2 * 0.68m * 1.15m + 2m * 0.25m + 2m / 16m) / 3.25m) / 2m));

    // The pay's pill is what its line says: hidden with it, when the legend hides the money passed on, and drawn
    // with it otherwise.
    [Fact]
    public void ShownPill_thePaysLineHidden_isNone()
    {
        var view = MapView.From(Full, Full.Run());

        var hidden = view with { Links = [.. view.Links.Where(link => link.Kind != MapLinkKind.PassedOn)] };

        (view.ShownPill, hidden.ShownPill).ShouldBe((view.Pill.ShouldNotBeNull(), null));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void From_anyPlan_drawsTheBarsFiveBuckets(bool refused, bool companySkipped)
    {
        var river = refused ? Full.WithReserve(-1m) : Full;
        river = companySkipped ? river.SkipCompany() : river;

        MapView.From(river, river.Run()).Buckets.Select(node => node.End.Id).ShouldBe(BucketsView.Keys);
    }

    // The company skipped: Company year stays, marked skipped, with no link to or from it; Director pay keeps
    // its lines into the buckets, the river starting at the pay.
    [Fact]
    public void From_companySkipped_drawsCompanyYearSkippedWithNoLinkAndDirectorPayWithItsLinesAndThePartner()
    {
        var river = Full.SkipCompany();

        var view = MapView.From(river, river.Run());

        view.ShouldSatisfyAllConditions(
            () => view.Modules.Select(node => (node.End.Id, node.Skipped)).ShouldBe([("company-year", true), ("director-pay", false), ("household", false)]),
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
            (MapPocketLine.Of(MapLinks.Of(Full, Full.Run())).Count + MapConditions.From(YearConditions.Of(Full.Run().Year!), MapView.ModulesOf(Full)).Count, true));
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

    // A link touches a module at one of its ends, a point of the company's line or of the pocket line beside it
    // among them, and the chain's line touches the quiet levers it runs past,
    // the rent left empty here: what a chosen module's lines are, kept and brought out (Drawn, Amounts).
    [Theory]
    [InlineData(RiverModule.Car)]
    [InlineData(RiverModule.Rent)]
    public void Touches_aModule_isEveryLinkWithItAtOneEndOrRunningPastIt(RiverModule module)
    {
        var river = MapQuietTests.Emptied(Full, OptionalModule.Rent);
        var view = MapView.From(river, river.Run());

        var links = view.Links.Where(link => link.Touches(module)).ToList();

        links.ShouldSatisfyAllConditions(
            () => links.ShouldNotBeEmpty(),
            () => links.ShouldBe(view.Links.Where(link => link.From.Module == module || link.To.Module == module || link.From.Lever == module || link.To.Lever == module
                || link.From.PocketLine == module || link.To.PocketLine == module || link.Through.Contains(module))));
    }

    // A point of the company's line, where a lever before the split takes its share, is named after the
    // lever, neither a module of the map nor a bucket.
    [Theory]
    [InlineData(RiverModule.Vouchers, "company-line-vouchers")]
    [InlineData(RiverModule.Allowances, "company-line-allowances")]
    [InlineData(RiverModule.Rent, "company-line-rent")]
    [InlineData(RiverModule.Car, "company-line-car")]
    public void Beside_aLeverBeforeTheSplit_isThePointOfTheCompanysLineBesideIt(RiverModule lever, string id)
    {
        var end = MapEnd.Beside(lever);

        (end.Id, end.Lever, end.Module, end.Bucket, end == MapEnd.Beside(lever)).ShouldBe((id, (RiverModule?)lever, (RiverModule?)null, (string?)null, true));
    }

    // The company's line runs beside the levers before the split alone.
    [Theory]
    [InlineData(RiverModule.CompanyYear)]
    [InlineData(RiverModule.Split)]
    [InlineData(RiverModule.Copyright)]
    [InlineData(RiverModule.Household)]
    [InlineData(RiverModule.Dividend)]
    public void Beside_aModuleNoLeverBeforeTheSplit_isRefused(RiverModule module)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapEnd.Beside(module));
    }

    // A link from or to the company's line beside a lever touches that lever, as one from or to its box does.
    [Fact]
    public void Touches_aLinkOfTheCompanysLineBesideALever_touchesTheLever()
    {
        var link = new MapLink(MapLinkKind.PassedOn, MapEnd.Beside(RiverModule.Vouchers), MapEnd.Beside(RiverModule.Rent), RiverModule.Vouchers,
            new MapLabel(MapText.Map, "Line", []), 1m);

        (link.Touches(RiverModule.Vouchers), link.Touches(RiverModule.Rent), link.Touches(RiverModule.Allowances)).ShouldBe((true, true, false));
    }

    [Fact]
    public void Touches_aModule_isWhatComesInAndWhatGoesOut()
    {
        var view = MapView.From(Full, Full.Run());

        var car = view.Links.Where(link => link.Touches(RiverModule.Car)).ToList();

        (car.Any(link => link.To.Lever == RiverModule.Car && link.From.Lever == RiverModule.Rent), car.Any(link => link.From == MapEnd.OnPocketLine(RiverModule.Car)),
                car.Any(link => link.To.Module == RiverModule.Split), car.Any(link => link.To.Module == RiverModule.DirectorPay))
            .ShouldBe((true, true, true, true));
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

    // A filter shows some links and leaves the others out: the modules stay where they are, and so does
    // every line it shows.
    [Fact]
    public void With_someLinksLeftOut_movesNothing()
    {
        var view = MapView.From(Full, Full.Run());

        var filtered = view with { Links = [.. view.Links.Where(link => link.Kind != MapLinkKind.IntoBucket)] };

        filtered.ShouldSatisfyAllConditions(
            () => filtered.Modules.ShouldBe(view.Modules),
            () => filtered.Links.Select(link => link.Route).ShouldBe(view.Links.Where(link => link.Kind != MapLinkKind.IntoBucket).Select(link => link.Route)));
    }

    // Links of the same ends and kind are one line, drawn once, its amount on it; the map shows no words,
    // the cards at its ends saying each line.
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

        var line = Line(MapView.From(Full, run), MapEnd.Of(RiverModule.Vouchers), MapEnd.OnPocketLine(RiverModule.Vouchers));

        (card.Count > 1, line.Links.Count, line.Amount).ShouldBe((true, card.Count, card.Sum(drain => drain.Amount)));
    }

    // The sum is of the figures as their card draws them, each to the cent, so it is the sum a viewer makes
    // of the card's lines.
    [Fact]
    public void Amount_ofLinesWithFractionsOfACent_isTheSumOfTheirFiguresToTheCent()
    {
        var line = new MapBundle(new MapRoute([new MapPoint(0m, 0m), new MapPoint(0m, 1m)]),
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

    // Every line of money is as thick as its amount beside the run's largest line of money, which is drawn
    // the thickest: with every lever, what Company year hands the first of them.
    [Fact]
    public void From_everyLever_drawsEachLineOfMoneyAsThickAsItsAmountTheLargestTheThickest()
    {
        var view = MapView.From(Full, Full.Run());

        var money = Money(view);

        var largest = money.Max(Amount);
        money.ShouldSatisfyAllConditions(
            () => money.Select(bundle => bundle.Route.Width).ShouldBe(money.Select(bundle => MapLineWidths.Money(Amount(bundle), largest))),
            () => money.Where(bundle => bundle.Route.Width == MapLineWidths.Thickest).Select(bundle => (bundle.From.Id, bundle.To.Id)).ShouldBe([("company-year", "company-line-vouchers")]));
    }

    // A line holding several of its card's lines is as thick as their sum, thicker than any of them alone:
    // what Director pay pays into the contributions and tax on pay.
    [Fact]
    public void From_aLineOfSeveral_isAsThickAsTheirSum()
    {
        var view = MapView.From(Full, Full.Run());
        var largest = Money(view).Max(Amount);

        var line = Line(view, MapEnd.Of(RiverModule.DirectorPay), MapEnd.InBucket("taxes_on_pay"));

        (line.Links.Count > 1, line.Route.Width, line.Links.All(link => MapLineWidths.Money(link.Amount ?? throw new InvalidOperationException("A line of money with no amount."), largest) < line.Route.Width))
            .ShouldBe((true, MapLineWidths.Money(Amount(line), largest), true));
    }

    // A tax base and a condition carry no money: their lines keep their kind's width, the costly car's among
    // them.
    [Fact]
    public void From_aTaxBaseOrACondition_drawsItsLineAtItsKindsWidth()
    {
        var river = MapViewConditionsTests.CostlyCar;

        var rules = MapView.From(river, river.Run()).Bundles().Where(bundle => bundle.Kind is MapLinkKind.TaxBase or MapLinkKind.Condition).ToList();

        rules.ShouldSatisfyAllConditions(
            () => rules.Select(bundle => bundle.Kind).Distinct().Count().ShouldBe(2),
            () => rules.Where(bundle => bundle.Route.Width != MapLinkStyle.Fixed(bundle.Kind)).Select(bundle => $"{bundle.From.Id} to {bundle.To.Id}").ShouldBeEmpty());
    }

    // The company skipped, the river starts at the pay: Director pay's largest line is the thickest, its line
    // into the pocket line and the pocket line's one stretch, which carries it on, alike.
    [Fact]
    public void From_companySkipped_drawsDirectorPaysLargestLineTheThickest()
    {
        var river = Full.SkipCompany();

        var money = Money(MapView.From(river, river.Run()));

        var largest = money.Where(bundle => bundle.Amount == money.Max(line => line.Amount)).ToList();
        largest.ShouldSatisfyAllConditions(
            () => largest.Select(bundle => $"{bundle.From.Id} to {bundle.To.Id}").ShouldBe(["director-pay to pocket-line-director-pay", "pocket-line-director-pay to pocket"]),
            () => largest.Select(bundle => bundle.Route.Width).ShouldAllBe(width => width == MapLineWidths.Thickest));
    }

    // What the map draws for a choice: every line of money, and the lines of a tax base or a condition with the
    // module chosen at an end, a point of the company's line or of the pocket line beside it counting as it
    // (MapLink.Touches); none of them while nothing is chosen. The lines left are the run's, on their routes, at
    // their widths, in their order; the modules, their badges and boxes, the buckets and the height are the run's.
    [Theory]
    [InlineData(null)]
    [InlineData(RiverModule.Car)]
    [InlineData(RiverModule.CorporateTax)]
    [InlineData(RiverModule.Household)]
    [InlineData(RiverModule.Vouchers)]
    public void ShowingFor_aChoice_isTheMoneyAndTheRulesOfTheModuleChosen(RiverModule? chosen)
    {
        var view = MapView.From(Full, Full.Run());

        var shown = view.ShowingFor(chosen);

        shown.ShouldSatisfyAllConditions(
            () => shown.Links.ShouldBe(view.Links.Where(link => MapLinkStyle.IsMoney(link.Kind) || (chosen is { } module && link.Touches(module)))),
            () => shown.Links.Where(link => !MapLinkStyle.IsMoney(link.Kind)).Any().ShouldBe(chosen is not null),
            () => shown.Bundles().Select(bundle => (bundle.Kind, bundle.Route, bundle.Route.Width)).ShouldBe(view.Bundles()
                .Where(bundle => MapLinkStyle.IsMoney(bundle.Kind) || (chosen is { } module && bundle.Touches(module))).Select(bundle => (bundle.Kind, bundle.Route, bundle.Route.Width))),
            () => (shown.Modules, shown.Buckets, shown.Units, shown.Pill).ShouldBe((view.Modules, view.Buckets, view.Units, view.Pill)),
            () => shown.Modules.SelectMany(node => node.Badges).ShouldNotBeEmpty());
    }

    // A refused amount leaves the map no link to draw, whatever is chosen.
    [Fact]
    public void ShowingFor_aRefusedAmount_isNoLine()
    {
        var refused = Full.WithReserve(-1m);

        MapView.From(refused, refused.Run()).ShowingFor(RiverModule.Car).Links.ShouldBeEmpty();
    }

    // A line ends in an arrowhead where it reaches a module, money passed on, what comes back out of a
    // bucket or a rule; a line into a bucket has none, running on under the
    // bucket, which is where its money stops, nor a line into the pocket line or a stretch of it, which runs on
    // into the pocket.
    [Fact]
    public void Bundles_aLineIntoABucket_endsWithNoArrowheadAndEveryOtherWithOne()
    {
        var river = MapViewConditionsTests.CostlyCar;

        var bundles = MapView.From(river, river.Run()).Bundles();

        bundles.ShouldSatisfyAllConditions(
            () => bundles.Where(bundle => bundle.To.Bucket is not null).ShouldNotBeEmpty(),
            () => bundles.Where(bundle => bundle.From.Bucket is not null || (bundle.From.PocketLine is not null && bundle.To.Bucket is null && bundle.To.PocketLine is null)).ShouldNotBeEmpty(),
            () => bundles.Where(bundle => bundle.To.PocketLine is not null).ShouldNotBeEmpty(),
            () => bundles.Where(bundle => !StretchOfTheCompanysLine(bundle) && bundle.Arrowhead == (bundle.To.Bucket is not null || bundle.To.PocketLine is not null))
                .Select(bundle => $"{bundle.From.Id} to {bundle.To.Id}").ShouldBeEmpty(),
            () => bundles.Where(StretchOfTheCompanysLine).Where(bundle => bundle.Arrowhead != (bundle.To == MapEnd.Of(RiverModule.Split))).Select(bundle => bundle.To.Id).ShouldBeEmpty());
    }

    // A stretch of the company's line has an arrowhead only into the split, which ends it: from one point of the
    // line to the next it runs on as one line.
    internal static bool StretchOfTheCompanysLine(MapBundle bundle) => MapLinksTests.CompanysLine(bundle.Links).Any();

    // A line into a bucket is drawn on under it to its middle, and a line back out of one from there, the
    // bucket drawn over them, so the line meets its edge wherever that is, at a rounded corner too; a line
    // between two modules is drawn on its route.
    [Fact]
    public void Bundles_aLineMeetingABucket_isDrawnOnUnderItToItsMiddle()
    {
        var view = MapView.From(Full, Full.Run());
        var (pocket, taxes) = (view.Buckets.Single(node => node.End.Bucket == "pocket").Slot, view.Buckets.Single(node => node.End.Bucket == "taxes_on_profit").Slot);

        var (into, back, along) = (Line(view, MapEnd.OnPocketLine(RiverModule.Dividend), MapEnd.InBucket("pocket")).Route,

            Line(view, MapEnd.InBucket("taxes_on_profit"), MapEnd.Of(RiverModule.Dividend)).Route, Line(view, MapEnd.Of(RiverModule.Split), MapEnd.Of(RiverModule.CorporateTax)).Route);

        into.ShouldSatisfyAllConditions(
            () => into.Drawn.ShouldBe([.. into.Points.SkipLast(1), new MapPoint(into.End.X, pocket.CenterY)]),
            () => back.Drawn.ShouldBe([new MapPoint(back.Points[0].X, taxes.CenterY), .. back.Points.Skip(1)]),
            () => along.Drawn.ShouldBe(along.Points));
    }

    // Nothing chosen, the map draws every line's amount where it was placed.
    [Fact]
    public void Amounts_nothingChosen_isEveryLinesWhereItWasPlaced()
    {
        var view = MapView.From(Full, Full.Run());
        var placed = MapAmounts.Of(view, MapAmountsTests.Say);

        view.Amounts(placed).ShouldBe(placed);
    }

    // A module chosen, the map draws its own lines' amounts where they were placed, whichever card draws the
    // line, and no other: Corporate tax's, the split's lines into it among them.
    [Fact]
    public void Amounts_aModuleChosen_isItsOwnLinesWhereTheyWerePlaced()
    {
        var view = MapView.From(Full, Full.Run());
        var placed = MapAmounts.Of(view, MapAmountsTests.Say);

        var amounts = view.Amounts(placed, RiverModule.CorporateTax);

        amounts.ShouldSatisfyAllConditions(
            () => amounts.ShouldBe(placed.Where(amount => amount.Bundle.Touches(RiverModule.CorporateTax))),
            () => amounts.Select(amount => amount.Bundle.Card).ShouldContain(RiverModule.Split),
            () => amounts.Count.ShouldBeLessThan(placed.Count));
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

    // The lines of money of a map, and what one carries.
    private static List<MapBundle> Money(MapView view) => [.. view.Bundles().Where(bundle => bundle.Kind is MapLinkKind.PassedOn or MapLinkKind.IntoBucket)];

    private static decimal Amount(MapBundle line) => line.Amount ?? throw new InvalidOperationException("A line of money with no amount.");

    // The one line of the map between two ends, of the kind money has between them.
    internal static MapBundle Line(MapView view, MapEnd from, MapEnd to) =>
        view.Bundles().Single(bundle => bundle.From == from && bundle.To == to && bundle.Kind != MapLinkKind.Condition);

    private static MapLink Link(decimal amount) =>
        new(MapLinkKind.IntoBucket, MapEnd.Of(RiverModule.Vouchers), MapEnd.InBucket("pocket"), RiverModule.Vouchers, new MapLabel(MapText.Map, "Line", []), amount);
}
