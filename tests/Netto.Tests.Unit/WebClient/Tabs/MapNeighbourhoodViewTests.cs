using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapNeighbourhoodHtml;

namespace Netto.Tests.WebClient.Tabs;

// The neighbourhood of the module chosen on the Map tab, as the column beside the map lists it: what comes
// into it, what goes out of it, the line running past it while it is quiet and the rules it sets or is
// under, each a link of the map's run, read from the run's every link whatever the legend ticks. Its words
// are given by the tab, so these tests word a link by its key; the words as a viewer reads them are
// MapNeighbourhoodRenderingTests'.
public class MapNeighbourhoodViewTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // The costly car's benefit is beyond 20% of the pay: a condition that does not hold.
    private static readonly RiverInputs CostlyCar = MapViewConditionsTests.CostlyCar;

    public static TheoryData<RiverModule> FullPlansModules => new(MapView.ModulesOf(Full));

    // A link worded by its key and its arguments, a module end by its name in the map's elements.
    private static string Words(MapLabel label) => string.Join('|', [label.Key, .. label.Args]);

    private static string Name(MapEnd end) => end.Id;

    private static MapNeighbourhoodView Of(RiverInputs river, RiverModule module) => Of(MapView.From(river, river.Run()), module);

    private static MapNeighbourhoodView Of(MapView view, RiverModule module) => MapNeighbourhoodView.Of(view, module, Words, Name);

    // What comes into a module and what goes out of it are its money links with it at one end, one row for
    // each, in the order of the map's links.
    [Theory]
    [MemberData(nameof(FullPlansModules))]
    public void Of_aModule_comesInIsEveryMoneyLinkIntoItAndGoesOutEveryMoneyLinkOutOfIt(RiverModule module)
    {
        var view = MapView.From(Full, Full.Run());
        var money = view.LinksOf(module).Where(IsMoney).ToList();

        var hood = Of(view, module);

        hood.ShouldSatisfyAllConditions(
            () => hood.ComesIn.Select(row => row.Words).ShouldBe(money.Where(link => link.To.Module == module).Select(link => Words(link.Carries))),
            () => hood.GoesOut.Select(row => row.Words).ShouldBe(money.Where(link => link.From.Module == module).Select(link => Words(link.Carries))),
            () => hood.ComesIn.Count.ShouldBe(money.Count(link => link.To.Module == module)),
            () => hood.PassesThrough.ShouldBeEmpty());
    }

    // A card's lines between the same two ends are one line on the map but each its own row here.
    [Fact]
    public void Of_directorPay_hasARowForEachOfItsCardsLinesIntoABucket()
    {
        var view = MapView.From(Full, Full.Run());
        var lines = view.LinksOf(RiverModule.DirectorPay).Where(link => link is { Kind: MapLinkKind.IntoBucket, From.Module: RiverModule.DirectorPay, To.Bucket: "taxes_on_pay" }).ToList();

        var hood = Of(view, RiverModule.DirectorPay);

        hood.ShouldSatisfyAllConditions(
            () => lines.Count.ShouldBeGreaterThan(1),
            () => view.Bundles().Count(bundle => bundle.Links.Any(link => link.Carries.Key == lines[0].Carries.Key)).ShouldBe(1),
            () => hood.GoesOut.Select(row => row.Words).ShouldBeSubsetOf(view.LinksOf(RiverModule.DirectorPay).Select(link => Words(link.Carries))),
            () => lines.Select(link => Words(link.Carries)).ShouldBeSubsetOf(hood.GoesOut.Select(row => row.Words)));
    }

    // Every amount is a link's own, which is a figure its card draws (MapCardsTests): none is made here.
    [Theory]
    [MemberData(nameof(FullPlansModules))]
    public void Of_aModule_everyRowSaysItsLinksOwnAmount(RiverModule module)
    {
        var view = MapView.From(Full, Full.Run());

        var hood = Of(view, module);

        hood.ComesIn.Concat(hood.GoesOut).Select(row => (row.Words, row.Amount)).ShouldBe(
            view.LinksOf(module).Where(IsMoney).OrderBy(link => link.To.Module == module ? 0 : 1)
                .Select(link => (Words(link.Carries), Formats.Euros(Money(link)))));
    }

    // A row's bar is its amount's share of the largest amount listed for the module, in and out: the
    // largest fills the bar, none is above it.
    [Theory]
    [MemberData(nameof(FullPlansModules))]
    public void Of_aModule_aRowsShareIsItsAmountOverTheLargestListed(RiverModule module)
    {
        var view = MapView.From(Full, Full.Run());
        var money = view.LinksOf(module).Where(IsMoney).OrderBy(link => link.To.Module == module ? 0 : 1).ToList();
        var largest = money.Count == 0 ? 0m : money.Max(link => Math.Abs(Money(link)));

        var hood = Of(view, module);

        var shares = hood.ComesIn.Concat(hood.GoesOut).Select(row => row.Share).ToList();
        shares.ShouldSatisfyAllConditions(
            () => shares.ShouldBe(money.Select(link => Shares.Of(Math.Abs(Money(link)), largest))),
            () => shares.ShouldAllBe(share => share >= 0m && share <= 1m),
            () => (shares.Count == 0 || shares.Max() == 1m).ShouldBeTrue());
    }

    // A plan at a loss has lines below nothing: a net pay under zero is still a line, its bar by its size.
    [Fact]
    public void Of_aLineBelowNothing_isBarredByItsSize()
    {
        var view = MapView.From(CostlyCar, CostlyCar.Run());
        var netPay = Money(view.LinksOf(RiverModule.DirectorPay).Single(link => link.Carries.Key == "Drain_net_pay"));
        var largest = view.LinksOf(RiverModule.DirectorPay).Where(IsMoney).Max(link => Math.Abs(Money(link)));

        var row = Of(view, RiverModule.DirectorPay).GoesOut.Single(row => row.Words == "Drain_net_pay");

        (netPay < 0m, row.Amount, row.Share).ShouldBe((true, Formats.Euros(netPay), Shares.Of(-netPay, largest)));
    }

    // What reaches the pocket is the one row drawn in gold.
    [Fact]
    public void Of_aRowToThePocket_isMarkedForTheGold()
    {
        var hood = Of(Full, RiverModule.DirectorPay);

        hood.GoesOut.Where(row => row.ToPocket).Select(row => row.Words).ShouldBe(["Drain_net_pay"]);
    }

    // A module on the map is an end the viewer can go to; a bucket and the river's edge are named only.
    [Fact]
    public void Of_anEnd_isChoosableOnlyWhenItIsAModuleOnTheMap()
    {
        var hood = Of(Full, RiverModule.Car);

        var costs = hood.GoesOut.Single(row => row.Words == "Drain_car_costs");
        var contribution = hood.ComesIn.Single(row => row.Words == "Drain_car_contribution");
        var toTheSplit = hood.GoesOut.Single(row => row.To.Module == RiverModule.Split);
        (costs.To, contribution.From, toTheSplit.To).ShouldBe((new MapNeighbourhoodEnd("out", null), new MapNeighbourhoodEnd("pocket", null),
            new MapNeighbourhoodEnd("split", RiverModule.Split)));
    }

    // A module that has left the map is named, not chosen.
    [Fact]
    public void Of_anEndThatIsAModuleOffTheMap_isNamedNotChoosable()
    {
        var view = MapView.From(Full, Full.Run());
        var withoutTheSplit = view with { Modules = [.. view.Modules.Where(node => node.End.Module != RiverModule.Split)] };

        var hood = Of(withoutTheSplit, RiverModule.Car);

        hood.GoesOut.Single(row => row.To.Name == "split").To.ShouldBe(new MapNeighbourhoodEnd("split", null));
    }

    // The module itself is named as the tab names a module.
    [Fact]
    public void Of_aModule_isNamed()
    {
        Of(Full, RiverModule.Car).ShouldSatisfyAllConditions(
            hood => hood.Module.ShouldBe(RiverModule.Car),
            hood => hood.Name.ShouldBe("car"));
    }

    // The rules a module sets or is under are its tax-base and condition links: a tax base with its amount
    // and no bar, a condition in its sentence with no amount, each saying which way it acts.
    [Fact]
    public void Of_aModule_itsRulesAreTheTaxBasesAndConditionsItSetsOrIsUnder()
    {
        var view = MapView.From(CostlyCar, CostlyCar.Run());
        var rules = view.LinksOf(RiverModule.Car).Where(link => !IsMoney(link)).ToList();

        var hood = Of(view, RiverModule.Car);

        hood.ShouldSatisfyAllConditions(
            () => rules.Select(link => link.Kind).Distinct().Order().ShouldBe([MapLinkKind.TaxBase, MapLinkKind.Condition]),
            () => hood.Rules.Select(rule => rule.Words).ShouldBe(rules.Select(link => Words(link.Carries))),
            () => hood.Rules.ShouldAllBe(rule => rule.SetsOn),
            () => hood.Rules.Select(rule => rule.Other.Module).ShouldBe(rules.Select(link => link.To.Module)),
            () => hood.Rules.Select(rule => rule.Amount).ShouldBe(rules.Select(link => link.Amount is { } amount ? Formats.Euros(amount) : null)));
    }

    [Fact]
    public void Of_aModuleUnderRules_saysWhoSetsThemOnIt()
    {
        var view = MapView.From(CostlyCar, CostlyCar.Run());
        var rules = view.LinksOf(RiverModule.CorporateTax).Where(link => !IsMoney(link)).ToList();

        var hood = Of(view, RiverModule.CorporateTax);

        hood.ShouldSatisfyAllConditions(
            () => rules.Select(link => link.From.Module).Distinct().Count().ShouldBeGreaterThan(1),
            () => hood.Rules.ShouldAllBe(rule => !rule.SetsOn),
            () => hood.Rules.Select(rule => rule.Other.Module).ShouldBe(rules.Select(link => link.From.Module)),
            () => hood.Rules.Select(rule => rule.Words).ShouldBe(rules.Select(link => Words(link.Carries))));
    }

    // The rule that is lost is marked and no other is: the costly car's benefits are over their share of the
    // pay, so the rate is lost; the pay's condition, which holds, and the tax bases are not.
    [Fact]
    public void Of_aConditionThatIsLost_isMarkedAndNoOtherRuleIs()
    {
        var hood = Of(CostlyCar, RiverModule.CorporateTax);

        hood.ShouldSatisfyAllConditions(
            () => hood.Rules.Where(rule => rule.Lost).Select(Condition).ShouldBe(["Condition_Benefits_Lost"]),
            () => hood.Rules.Where(rule => !rule.Lost).Select(Condition).ShouldContain("Condition_Pay_MinimumPayMet"),
            () => hood.Rules.Where(rule => rule.Amount is not null).ShouldNotContain(rule => rule.Lost));
    }

    // The key a rule's words are said by.
    private static string Condition(MapNeighbourhoodRule rule) => rule.Words.Split('|')[0];

    // A condition each kind in each state, and whether its rule is marked: only where the rate or the reserve
    // is lost, as its badge says it. A royalty that lowers no pay does not hold either, but loses nothing.
    public static TheoryData<YearCondition, bool> Conditions => new()
    {
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayNotMet, 51_000m, 40_000m, 60_000m), true },
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayMet, 51_000m, 60_000m, 40_000m), false },
        { new PayKeepsReducedRate(ReducedRateCondition.TaxableIncomeNotAbovePay, 51_000m, 50_000m, 50_000m), false },
        { new BenefitsWithinShareOfPay(false, 1_714.29m, 6_714.29m, 0.20m), true },
        { new BenefitsWithinShareOfPay(true, 1_714.29m, 51_714.29m, 0.20m), false },
        { new NoOfficeAllowanceBesideRent(1_380m, 0m), false },
        { new NoOfficeAllowanceBesideRent(0m, 1_938.44m), false },
        { new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false), false },
        { new RoyaltyLowersPayKeepingReducedRate(40_000m, null, true), false },
        { new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false), false },
        { new RoyaltyLowersPayKeepingReducedRate(null, null, true), true },
        { new PayOutCoversReserve(40_000m, 40_000m), false },
        { new PayOutCoversReserve(40_000.01m, 40_000m), true },
    };

    [Theory]
    [MemberData(nameof(Conditions))]
    public void Of_aCondition_isMarkedOnlyWhereTheRateOrTheReserveIsLost(YearCondition condition, bool marked)
    {
        var link = MapConditions.Link(condition);
        var view = MapView.From(Full, Full.Run()) with { Links = [link] };

        var rule = Of(view, link.From.Module.ShouldNotBeNull()).Rules.ShouldHaveSingleItem();

        rule.Lost.ShouldBe(marked);
    }

    // The legend ticks the lines of the map and nothing here: the view is made from the run's every link, so
    // a module's rules are listed whether or not their lines are drawn.
    [Fact]
    public void Of_theRunsEveryLink_listsTheRulesThoughTheirLinesAreNotDrawn()
    {
        var view = MapView.From(Full, Full.Run());
        var quiet = new MapLinkFilter(new BrowserStorage()).Showing(view);

        quiet.LinksOf(RiverModule.Car).Where(link => !IsMoney(link)).ShouldBeEmpty();
        Of(view, RiverModule.Car).Rules.ShouldNotBeEmpty();
    }

    // A quiet lever moves no money of its own: the chain's line running past it, with its own words and
    // amount, is neither an in nor an out.
    [Fact]
    public void Of_aQuietLever_isTheLineRunningPastItAndNeitherAnInNorAnOut()
    {
        var view = MapView.From(ChainQuiet, ChainQuiet.Run());
        var past = view.LinksOf(RiverModule.Rent).ShouldHaveSingleItem();

        var hood = Of(view, RiverModule.Rent);

        hood.ShouldSatisfyAllConditions(
            () => hood.ComesIn.ShouldBeEmpty(),
            () => hood.GoesOut.ShouldBeEmpty(),
            () => hood.Rules.ShouldBeEmpty(),
            () => hood.PassesThrough.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
                row => row.Words.ShouldBe(Words(past.Carries)),
                row => row.Amount.ShouldBe(Formats.Euros(Money(past))),
                row => row.From.ShouldBe(new MapNeighbourhoodEnd("vouchers", RiverModule.Vouchers)),
                row => row.To.ShouldBe(new MapNeighbourhoodEnd("split", RiverModule.Split))),
            () => hood.IsEmpty.ShouldBeFalse());
    }

    // The modules a line runs past are named on the row of each end of the line, which would otherwise seem to
    // skip them.
    [Fact]
    public void Of_theEndsOfALineRunningPastQuietLevers_saySoOnTheRow()
    {
        var view = MapView.From(ChainQuiet, ChainQuiet.Run());

        var (vouchers, split) = (Of(view, RiverModule.Vouchers), Of(view, RiverModule.Split));

        vouchers.GoesOut.Single(row => row.To.Module == RiverModule.Split).Past.ShouldBe(["allowances", "rent", "car"]);
        split.ComesIn.Single(row => row.From.Module == RiverModule.Vouchers).Past.ShouldBe(["allowances", "rent", "car"]);
    }

    [Fact]
    public void Of_aRowThatRunsPastNothing_saysNothingOfIt()
    {
        Of(Full, RiverModule.Car).GoesOut.ShouldAllBe(row => row.Past.Count == 0);
    }

    // A royalty that comes to nothing has no link: there is nothing to list.
    [Fact]
    public void Of_aQuietRoyalty_hasNothingToList()
    {
        var river = MapQuietTests.Emptied(Full, OptionalModule.Copyright);

        var hood = Of(river, RiverModule.Copyright);

        (hood.IsEmpty, hood.Refused).ShouldBe((true, false));
    }

    // Company year skipped has no link either.
    [Fact]
    public void Of_aSkippedCompanyYear_hasNothingToList()
    {
        Of(Full.SkipCompany(), RiverModule.CompanyYear).IsEmpty.ShouldBeTrue();
    }

    // The river starts at Company year: nothing comes in.
    [Fact]
    public void Of_companyYear_hasNothingComingIn()
    {
        var hood = Of(Full, RiverModule.CompanyYear);

        (hood.ComesIn.Count, hood.GoesOut.Count, hood.IsEmpty).ShouldBe((0, 1, false));
    }

    // While the engine refuses an amount there is no link: the neighbourhood says there is no figure.
    [Fact]
    public void Of_aRefusedAmount_isRefusedWithNothingListed()
    {
        var river = Full.WithReserve(-1m);

        var hood = Of(river, RiverModule.Car);

        (hood.Refused, hood.ComesIn.Count + hood.GoesOut.Count + hood.PassesThrough.Count + hood.Rules.Count).ShouldBe((true, 0));
    }

    // Every plan's every module: each link is listed once, and no bar is above its whole or below nothing.
    [Fact]
    public void Of_everyModuleOfEveryPlan_listsEachLinkOnceAndKeepsEveryShareWithinTheWhole()
    {
        RiverInputs[] rivers = [Full, CostlyCar, ChainQuiet, RiverInputs.Example, Full.SkipCompany()];
        var problems = new List<string>();

        foreach (var river in rivers)
        {
            var view = MapView.From(river, river.Run());
            foreach (var module in MapView.ModulesOf(river))
            {
                var hood = Of(view, module);
                var rows = hood.ComesIn.Concat(hood.GoesOut).Concat(hood.PassesThrough).ToList();
                if (rows.Count + hood.Rules.Count != view.LinksOf(module).Count() || rows.Any(row => row.Share is < 0m or > 1m))
                    problems.Add($"{module}: {rows.Count} rows + {hood.Rules.Count} rules of {view.LinksOf(module).Count()} links, shares {string.Join(' ', rows.Select(row => row.Share))}");
            }
        }

        problems.ShouldBeEmpty();
    }
}
