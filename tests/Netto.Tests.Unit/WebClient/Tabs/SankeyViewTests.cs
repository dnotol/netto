using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The Sankey's bands, gathered from the map's links of money (MapLinks.Of): money conserved at every node of every
// plan the map's layout is held on, each bucket receiving what the bar says to the cent, no band a figure of the
// Sankey's own.
public class SankeyViewTests
{
    // Every plan the map's amounts are held on, and plans with profits kept from earlier years, which none of
    // those has.
    public static TheoryData<string> Plans()
    {
        var plans = new TheoryData<string>();
        foreach (var plan in AllPlans())
            plans.Add(plan);

        return plans;
    }

    // The plans the Sankey draws: those whose bar draws shares.
    public static TheoryData<string> DrawnPlans()
    {
        var plans = new TheoryData<string>();
        foreach (var plan in AllPlans().Where(plan => View(plan) is not null))
            plans.Add(plan);

        return plans;
    }

    [Theory]
    [MemberData(nameof(Plans))]
    public void From_everyPlan_isDrawnExactlyWhereTheBarDrawsShares(string plan)
    {
        var river = River(plan);
        var run = river.Run();

        (SankeyView.From(river, run) is not null).ShouldBe(run.Error is null && BucketsView.From(run)?.Buckets.All(bucket => bucket.Share is not null) == true);
    }

    // The dividend all kept in the company: the Sankey draws Company year on to what is kept, as the bar does.
    [Fact]
    public void From_dividendAllKept_isDrawn_intoWhatIsKept()
    {
        var river = RiverInputs.Example.WithReserve(1_000_000m);
        var bar = BucketsView.From(river.Run())!;

        var view = SankeyView.From(river, river.Run());

        view?.In(SankeyNode.InBucket("kept_in_company")).ShouldBe(bar.AmountOf("kept_in_company"));
    }

    // No pay and no dividend: the director still owes the self-employed's minimum contributions, which the pocket
    // pays, so the pocket is below nothing and neither the bar nor the Sankey draws shares.
    [Fact]
    public void From_planPayingNothingOut_drawsNothing_thePocketBelowNothing()
    {
        var river = RiverInputs.Example.WithDirectorPay(0m).WithReserve(1_000_000m);
        var bar = BucketsView.From(river.Run())!;

        var view = SankeyView.From(river, river.Run());

        (bar.AmountOf("pocket") < 0m, bar.Buckets.All(bucket => bucket.Share is null), view).ShouldBe((true, true, (SankeyView?)null));
    }

    // Money back out of a bucket into a module sending nothing into it has no band to net it on, and the car's
    // contribution is the one loop the Sankey draws: no honest picture.
    [Fact]
    public void Gather_aCreditBackIntoAModuleWithNoBandIntoItsBucket_drawsNothing()
    {
        var bar = BucketsView.From(RiverInputs.Example.Run())!;
        var label = new MapLabel(MapText.Map, "Line", []);
        MapLink[] links =
        [
            new(MapLinkKind.PassedOn, MapEnd.Of(RiverModule.DirectorPay), MapEnd.InBucket("pocket"), RiverModule.DirectorPay, label, 100m),
            new(MapLinkKind.PassedOn, MapEnd.InBucket("taxes_on_pay"), MapEnd.Of(RiverModule.DirectorPay), RiverModule.DirectorPay, label, 10m),
        ];

        SankeyView.Gather(links, bar, 0m).ShouldBeNull();
    }

    [Fact]
    public void Gather_theSameCreditWithABandIntoItsBucket_isNettedOnIt()
    {
        var bar = BucketsView.From(RiverInputs.Example.Run())!;
        var label = new MapLabel(MapText.Map, "Line", []);
        MapLink[] links =
        [
            new(MapLinkKind.PassedOn, MapEnd.Of(RiverModule.DirectorPay), MapEnd.InBucket("taxes_on_pay"), RiverModule.DirectorPay, label, 30m),
            new(MapLinkKind.PassedOn, MapEnd.InBucket("taxes_on_pay"), MapEnd.Of(RiverModule.DirectorPay), RiverModule.DirectorPay, label, 10m),
        ];

        var band = SankeyView.Gather(links, bar, 0m)!.Bands.ShouldHaveSingleItem();

        (band.Amount, band.Back, band.Loop).ShouldBe((20m, 10m, false));
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_everyNodeBetweenTheSourcesAndTheBuckets_passesOnWhatItReceives(string plan)
    {
        var view = View(plan)!;

        var unbalanced = view.Nodes.Where(node => node.Bucket is null && !view.IsSource(node) && view.In(node) != view.Out(node))
            .Select(node => $"{node.Id}: in {view.In(node)}, out {view.Out(node)}");

        unbalanced.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_eachBucket_receivesWhatTheBarSays(string plan)
    {
        var river = River(plan);
        var bar = BucketsView.From(river.Run())!;
        var view = View(plan)!;

        var buckets = BucketsView.Keys.Select(key => (key, view.In(SankeyNode.InBucket(key)) - view.Out(SankeyNode.InBucket(key)))).ToList();

        buckets.ShouldBe(BucketsView.Keys.Select(key => (key, bar.AmountOf(key))).ToList());
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_theSources_sendWhatTheBarStartsFrom(string plan)
    {
        var bar = BucketsView.From(River(plan).Run())!;
        var view = View(plan)!;

        var sent = view.Nodes.Where(view.IsSource).Sum(node => view.Out(node) - view.In(node));

        sent.ShouldBe(bar.Start);
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_companyYear_sendsWhatTheCompanyEarns_beforeEarlierProfits(string plan)
    {
        var bar = BucketsView.From(River(plan).Run())!;
        var view = View(plan)!;
        var company = SankeyNode.Of(RiverModule.CompanyYear);

        var sent = view.Nodes.Contains(company) ? view.Out(company) - view.In(company) : (decimal?)null;

        sent.ShouldBe(bar.StartsAtPay ? null : bar.Start - bar.EarlierProfits);
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_everyMoneyLinkOfTheMap_isGatheredOnce_theCarsCostsThroughTheCar(string plan)
    {
        var river = River(plan);
        var view = View(plan)!;
        var money = MapLinks.Of(river, river.Run()).Where(link => MapLinkStyle.IsMoney(link.Kind) && link.Amount != 0m).ToList();

        var gathered = view.Bands.SelectMany(band => band.Links).ToList();

        var expected = money.Where(link => !Stretch(link)).SelectMany(link => CarCosts(link) ? [link, link] : new[] { link });
        gathered.ShouldBe(expected, ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_eachBand_isTheSumOfItsLinks_lessWhatCameBack(string plan)
    {
        var view = View(plan)!;

        var wrong = view.Bands.Where(band => band.Links.Count > 0)
            .Where(band => band.Amount != MapBundle.Sum(band.Links.Where(link => link.From.Bucket is null || band.Loop)) - band.Back
                || band.Back != (band.Loop ? 0m : MapBundle.Sum(band.Links.Where(link => link.From.Bucket is not null))));

        wrong.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_noBand_isNothingOrLessThanNothing(string plan)
    {
        var view = View(plan)!;

        view.Bands.ShouldAllBe(band => band.Amount > 0m);
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_theOnlyLoop_isTheContributions_fromThePocketIntoCompanyYear(string plan)
    {
        var view = View(plan)!;

        view.Bands.Where(band => band.Loop).ShouldAllBe(band =>
            band.From == SankeyNode.InBucket("pocket") && band.To == SankeyNode.Of(RiverModule.CompanyYear)
            && band.Links.All(link => link.Card == RiverModule.Car || link.Card == RiverModule.Equipment));
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_nothingThatMovesNoMoney_isANode(string plan)
    {
        var view = View(plan)!;

        view.Nodes.ShouldAllBe(node => node.Module != RiverModule.Household && node.Module != RiverModule.River);
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_theNodes_areInTheStagesOrder(string plan)
    {
        var view = View(plan)!;

        view.Nodes.Select(SankeyLayout.Rank).ShouldBeInOrder();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void From_theFiveBuckets_areTheLastNodes_whatEverTheyHold(string plan)
    {
        var view = View(plan)!;

        view.Nodes.TakeLast(5).Select(node => node.Bucket).ShouldBe(BucketsView.Keys);
    }

    [Fact]
    public void From_theCarsAndTheITAndPhonesContributions_areOneLoopOfTheirSum()
    {
        var river = MapLinksTests.Full;
        var run = river.Run();
        (run.Year!.Car.Contribution, run.Year.Equipment.Contribution).ShouldBe((600m, 20m));

        var loop = SankeyView.From(river, run)!.Bands.Single(band => band.Loop);

        loop.Amount.ShouldBe(620m);
    }

    [Fact]
    public void From_theITAndPhonesContributionAlone_isTheLoop()
    {
        var river = MapRendering.River("a car without contribution");

        var loop = SankeyView.From(river, river.Run())!.Bands.Single(band => band.Loop);

        (loop.Amount, loop.Links.Select(link => link.Card).Distinct().Single()).ShouldBe((20m, RiverModule.Equipment));
    }

    [Fact]
    public void From_noContribution_hasNoLoop()
    {
        var river = MapRendering.River("a car without contribution").WithEquipmentContribution(0m);

        SankeyView.From(river, river.Run())!.Bands.ShouldNotContain(band => band.Loop);
    }

    // The IT and phone takes no share of the company's line, as the car: its costs run through it into the expenses.
    [Fact]
    public void From_theITAndPhonesCosts_runThroughItIntoTheExpenses()
    {
        var river = MapLinksTests.Full;
        var view = SankeyView.From(river, river.Run())!;

        var through = (Band(view, SankeyNode.Of(RiverModule.CompanyYear), SankeyNode.Of(RiverModule.Equipment))?.Amount,
            Band(view, SankeyNode.Of(RiverModule.Equipment), SankeyNode.InBucket("expenses"))?.Amount);

        through.ShouldBe((900m, 900m));
    }

    [Theory]
    [InlineData(true, true, "Loop_Both")]
    [InlineData(true, false, "Loop_Car")]
    [InlineData(false, true, "Loop_Equipment")]
    public void LoopText_namesWhoseContributionsTheLoopHolds(bool car, bool equipment, string expected)
    {
        var river = MapLinksTests.Full.WithCarContribution(car ? 600m : 0m).WithEquipmentContribution(equipment ? 20m : 0m);

        var loop = SankeyView.From(river, river.Run())!.Bands.Single(band => band.Loop);

        SankeyView.LoopText(loop).ShouldBe(expected);
    }

    [Fact]
    public void From_carsCosts_runThroughTheCarIntoTheExpenses()
    {
        var river = MapLinksTests.Full;
        var costs = river.Run().Year!.Car.Costs;
        var view = SankeyView.From(river, river.Run())!;

        var through = (Band(view, SankeyNode.Of(RiverModule.CompanyYear), SankeyNode.Of(RiverModule.Car))?.Amount,
            Band(view, SankeyNode.Of(RiverModule.Car), SankeyNode.InBucket("expenses"))?.Amount);

        through.ShouldBe((costs, costs));
    }

    [Theory]
    [MemberData(nameof(DividendTaxedPlans))]
    public void From_refundOnTheExemptDividend_isNettedOnItsBand_whatCameBackKept(string plan)
    {
        var river = MapRendering.River(plan);
        var view = SankeyView.From(river, river.Run())!;

        var band = Band(view, SankeyNode.Of(RiverModule.Dividend), SankeyNode.InBucket("taxes_on_profit"))!;

        (band.Back > 0m, band.Loop).ShouldBe((true, false));
    }

    public static TheoryData<string> DividendTaxedPlans() => new("every lever", "the example");

    [Fact]
    public void From_earlierProfits_areASourceIntoDividend_ofTheDividendCardsAmount()
    {
        var river = MapLinksTests.Full.WithEarlierProfits(20_000m);
        var run = river.Run();

        var band = SankeyView.From(river, run)!.Bands.Single(band => band.From == SankeyNode.EarlierProfits);

        (band.To, band.Amount).ShouldBe((SankeyNode.Of(RiverModule.Dividend), DividendView.From(run.Year!).EarlierProfits));
    }

    [Fact]
    public void From_noEarlierProfits_hasNoSourceForThem()
    {
        var river = MapLinksTests.Full;

        SankeyView.From(river, river.Run())!.Nodes.ShouldNotContain(SankeyNode.EarlierProfits);
    }

    [Fact]
    public void From_refusal_drawsNothing()
    {
        var river = MapLinksTests.Full.WithReserve(-1m);

        SankeyView.From(river, river.Run()).ShouldBeNull();
    }

    [Fact]
    public void From_lossYear_drawsNothing()
    {
        var river = RiverInputs.Example.WithRevenue(10_000m);

        SankeyView.From(river, river.Run()).ShouldBeNull();
    }

    [Fact]
    public void From_companySkipped_startsAtDirectorPay()
    {
        var river = MapLinksTests.Full.SkipCompany();
        var view = SankeyView.From(river, river.Run())!;

        view.Nodes.Where(view.IsSource).ShouldBe([SankeyNode.Of(RiverModule.DirectorPay)]);
    }

    [Theory]
    [InlineData("every lever")]
    [InlineData("the company skipped")]
    public void Says_aBucket_theBarsAmount(string plan)
    {
        var river = MapRendering.River(plan);
        var bar = BucketsView.From(river.Run())!;
        var view = SankeyView.From(river, river.Run())!;

        BucketsView.Keys.Select(key => view.Says(SankeyNode.InBucket(key))).ShouldBe(BucketsView.Keys.Select(bar.AmountOf));
    }

    [Fact]
    public void Says_companyYear_theCardsProfitBeforeLevers_notWhatTheContributionComesOutOf()
    {
        var river = MapLinksTests.Full;
        var run = river.Run();

        SankeyView.From(river, run)!.Says(SankeyNode.Of(RiverModule.CompanyYear)).ShouldBe(run.Year!.ProfitBeforeLevers);
    }

    [Fact]
    public void Says_corporateTax_theTaxItPays_notTheProfitEnteringIt()
    {
        var river = MapLinksTests.Full;
        var run = river.Run();
        var view = SankeyView.From(river, run)!;
        var taxes = Band(view, SankeyNode.Of(RiverModule.CorporateTax), SankeyNode.InBucket("taxes_on_profit"))!;

        (view.Says(SankeyNode.Of(RiverModule.CorporateTax)), taxes.Amount > 0m).ShouldBe((taxes.Amount, true));
        view.Says(SankeyNode.Of(RiverModule.CorporateTax)).ShouldNotBe(run.Year!.ProfitAfterPay);
    }

    [Fact]
    public void Says_dividend_whatIsPaidOut_notWhatCouldBe()
    {
        var river = MapLinksTests.Full.WithReserve(1_000m);
        var run = river.Run();
        var dividend = DividendView.From(run.Year!);

        dividend.Kept.ShouldBeGreaterThan(0m);
        SankeyView.From(river, run)!.Says(SankeyNode.Of(RiverModule.Dividend)).ShouldBe(dividend.Dividend);
    }

    [Fact]
    public void Says_aModule_whatItReceives()
    {
        var river = MapLinksTests.Full;
        var view = SankeyView.From(river, river.Run())!;
        var pay = SankeyNode.Of(RiverModule.DirectorPay);

        view.Says(pay).ShouldBe(view.Bands.Where(band => band.To == pay).Sum(band => band.Amount));
    }

    [Fact]
    public void Touches_theCarContributionsLoop_theCar()
    {
        var river = MapLinksTests.Full;

        SankeyView.From(river, river.Run())!.Bands.Single(band => band.Loop).Touches(RiverModule.Car).ShouldBeTrue();
    }

    [Fact]
    public void Touches_aBand_theModulesAtItsEnds_notOthers()
    {
        var river = MapLinksTests.Full;
        var view = SankeyView.From(river, river.Run())!;

        var shared = Band(view, SankeyNode.Of(RiverModule.CompanyYear), SankeyNode.Of(RiverModule.Split))!;

        (shared.Touches(RiverModule.Split), shared.Touches(RiverModule.CompanyYear), shared.Touches(RiverModule.Car)).ShouldBe((true, true, false));
    }

    [Theory]
    [InlineData(RiverModule.CompanyYear, "company-year")]
    [InlineData(RiverModule.Split, "split")]
    public void Choice_aModulesNode_choosesTheModule(RiverModule module, string name)
    {
        var node = SankeyNode.Of(module);

        (node.Choice, node.Id).ShouldBe((MapChoice.Of(module), name));
    }

    [Fact]
    public void Choice_aBucketsNode_choosesTheBucket()
    {
        SankeyNode.InBucket("pocket").Choice.ShouldBe(MapChoice.InBucket("pocket"));
    }

    // Earlier profits are typed on the Dividend card.
    [Fact]
    public void Choice_earlierProfits_choosesDividend()
    {
        SankeyNode.EarlierProfits.Choice.ShouldBe(MapChoice.Of(RiverModule.Dividend));
    }

    [Fact]
    public void InBucket_aKeyTheBarHasNot_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => SankeyNode.InBucket("savings"));
    }

    internal static IEnumerable<string> AllPlans() =>
    [
        .. MapAmountsTests.Plans().Select(row => row.Data),
        EarlierProfits, EarlierProfitsWithTheExample,
    ];

    internal const string EarlierProfits = "every lever, earlier profits";

    internal const string EarlierProfitsWithTheExample = "the example, earlier profits";

    internal static RiverInputs River(string plan) => plan switch
    {
        EarlierProfits => MapLinksTests.Full.WithEarlierProfits(20_000m),
        EarlierProfitsWithTheExample => RiverInputs.Example.WithEarlierProfits(35_000m),
        _ => MapAmountsTests.River(plan),
    };

    internal static SankeyView? View(string plan)
    {
        var river = River(plan);
        return SankeyView.From(river, river.Run());
    }

    private static SankeyBand? Band(SankeyView view, SankeyNode from, SankeyNode to) => view.Bands.SingleOrDefault(band => band.From == from && band.To == to);

    // A stretch of the company's line, which the Sankey folds into Company year: from a point of it, or Company
    // year, to the next one, the last into the split staying the band into it.
    private static bool Stretch(MapLink link) =>
        (link.From.Lever is not null || link.From == MapEnd.Of(RiverModule.CompanyYear)) && link.To.Lever is not null;

    private static bool CarCosts(MapLink link) => link.From.Lever is { } lever && link.To.Bucket is not null && link.Card == lever;
}
