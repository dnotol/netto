using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the module chosen on the Map tab receives, and from where, as the line above its card says it: a part
// for each line of money the map draws into it, with the amount the map says on that line and the end it
// comes from, read from every link of the run. Its words are the tab's, so these tests name an end by its id;
// the line as a viewer reads it is MapTabTests'.
public class MapReceivedViewTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    public static TheoryData<RiverModule> FullPlansModules => new(MapView.ModulesOf(Full));

    private static MapReceivedView Of(RiverInputs river, RiverModule module) => Of(MapView.From(river, river.Run(), MapNames.English), module);

    private static MapReceivedView Of(MapView view, RiverModule module) => MapReceivedView.Of(view, module, end => end.Id);

    // Each part is a line of money the map draws into the module from another module, in the map's order, with the amount the
    // map says on it; what comes off the company's line comes from Company year, whose line it is.
    [Theory]
    [MemberData(nameof(FullPlansModules))]
    public void Of_aModule_isEveryLineOfMoneyIntoItWithTheAmountTheMapSaysOnIt(RiverModule module)
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English);
        var into = view.Bundles().Where(bundle => MapLinkStyle.IsMoney(bundle.Kind) && bundle.To.Module == module && bundle.From.Bucket is null).ToList();

        var received = Of(view, module);

        received.Lines.Select(line => (line.Amount, line.From))
            .ShouldBe(into.Select(bundle => (Formats.Euros(bundle.Amount!.Value), bundle.From.Lever is null ? bundle.From.Id : "company-year")));
    }

    // A lever receives its share of the company's line, what its card says the company pays, from Company year,
    // an end the viewer can go to; nothing from the lever above it.
    [Theory]
    [InlineData(RiverModule.Vouchers)]
    [InlineData(RiverModule.Allowances)]
    [InlineData(RiverModule.Rent)]
    public void Of_aLever_receivesItsShareFromCompanyYear(RiverModule lever)
    {
        var share = HandOff.Cost(Full.Run().Year!, RiverModules.Lever(lever)!.Value);

        Of(Full, lever).Lines.ShouldBe([new MapReceived(Formats.Euros(share), "company-year", RiverModule.CompanyYear, First: true)]);
    }

    // A rule reaching a module, a tax base or a condition, moves no money into it: it is not received.
    [Fact]
    public void Of_corporateTax_receivesTheCompanysProfitFromTheSplitAndNoRule()
    {
        var received = Of(Full, RiverModule.CorporateTax);

        received.Lines.Select(line => line.From).ShouldBe(["split"]);
    }

    // The split of the example receives what the company earns before it pays its director, from Company
    // year, the map's line between them.
    [Fact]
    public void Of_theSplitOfTheExample_receivesWhatTheCompanyEarnsBeforePayFromCompanyYear()
    {
        var year = RiverInputs.Example.Run().Year!;

        var received = Of(RiverInputs.Example, RiverModule.Split);

        received.Lines.ShouldBe([new MapReceived(Formats.Euros(year.ProfitBeforeLevers), "company-year", RiverModule.CompanyYear, First: true)]);
    }

    // The first part is marked so, the line saying a separator before each of the others alone.
    [Theory]
    [MemberData(nameof(FullPlansModules))]
    public void Of_aModule_marksItsFirstPartAlone(RiverModule module)
    {
        var received = Of(Full, module);

        received.Lines.Select(line => line.First).ShouldBe(received.Lines.Select((_, index) => index == 0));
    }

    // The car takes no share of the company's line, and its contribution joins that line: the car receives no
    // money, only what crosses it, its benefit, which is a tax base.
    [Fact]
    public void Of_theCar_receivesNothing()
    {
        Of(Full, RiverModule.Car).Lines.ShouldBeEmpty();
    }

    // A module on the map is an end the viewer can go to. A bucket is never an origin: what comes back out of
    // the taxes on profit (the exempt dividend's refund) is the card's drain line, not money received.
    [Fact]
    public void Of_anEndThatIsAModuleOnTheMap_canBeGoneTo()
    {
        var received = Of(Full, RiverModule.Dividend);

        received.Lines.Select(line => (line.From, line.Module)).ShouldBe([("corporate-tax", RiverModule.CorporateTax)]);
    }

    [Theory]
    [MemberData(nameof(FullPlansModules))]
    public void Of_aModule_namesNoBucketAsWhereMoneyComesFrom(RiverModule module)
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English);
        var buckets = view.Bundles().Where(bundle => bundle.From.Bucket is not null).Select(bundle => bundle.From.Id).Distinct().ToList();

        Of(view, module).Lines.Select(line => line.From).ShouldNotContain(origin => buckets.Contains(origin));
    }

    // Company year starts the river: it receives nothing.
    [Fact]
    public void Of_companyYear_receivesNothing()
    {
        Of(Full, RiverModule.CompanyYear).Lines.ShouldBeEmpty();
    }

    // A lever that moves no money of its own receives nothing: the company's line runs on past it, and the split
    // receives it from Company year, whose line it is.
    [Fact]
    public void Of_aQuietLever_receivesNothing_andTheSplitReceivesTheCompanysLineFromCompanyYear()
    {
        var (quiet, split) = (Of(MapReceivedHtml.ChainQuiet, RiverModule.Rent), Of(MapReceivedHtml.ChainQuiet, RiverModule.Split));

        quiet.ShouldSatisfyAllConditions(
            () => quiet.Lines.ShouldBeEmpty(),
            () => split.Lines.Select(line => line.Module).ShouldBe([RiverModule.CompanyYear]));
    }

    // While the engine refuses an amount the map draws no line, so nothing is received.
    [Fact]
    public void Of_aRefusedAmount_receivesNothing()
    {
        Of(Full.WithReserve(-1m), RiverModule.Split).Lines.ShouldBeEmpty();
    }
}
