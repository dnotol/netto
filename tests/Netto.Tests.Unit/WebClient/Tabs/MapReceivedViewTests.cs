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

    private static MapReceivedView Of(RiverInputs river, RiverModule module) => Of(MapView.From(river, river.Run()), module);

    private static MapReceivedView Of(MapView view, RiverModule module) => MapReceivedView.Of(view, module, end => end.Id);

    // Each part is a line of money the map draws into the module, in the map's order, with the amount the
    // map says on it.
    [Theory]
    [MemberData(nameof(FullPlansModules))]
    public void Of_aModule_isEveryLineOfMoneyIntoItWithTheAmountTheMapSaysOnIt(RiverModule module)
    {
        var view = MapView.From(Full, Full.Run());
        var into = view.Bundles().Where(bundle => MapLinkStyle.IsMoney(bundle.Kind) && bundle.To.Module == module).ToList();

        var received = Of(view, module);

        received.Lines.Select(line => (line.Amount, line.From)).ShouldBe(into.Select(bundle => (Formats.Euros(bundle.Amount!.Value), bundle.From.Id)));
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
    [Fact]
    public void Of_aModuleReceivingFromTwoEnds_marksItsFirstPartAlone()
    {
        var received = Of(Full, RiverModule.Car);

        received.Lines.Select(line => line.First).ShouldBe([true, false]);
    }

    // What comes back out of a bucket is received from it, a bucket being named only.
    [Fact]
    public void Of_theCar_receivesYourContributionFromThePocket()
    {
        var received = Of(Full, RiverModule.Car);

        received.Lines.ShouldContain(line => line.From == "pocket" && line.Module == null);
    }

    // A module on the map is an end the viewer can go to; a bucket, the taxes on profit a credit comes back
    // out of, is named only.
    [Fact]
    public void Of_anEndThatIsAModuleOnTheMap_canBeGoneTo()
    {
        var received = Of(Full, RiverModule.Dividend);

        received.Lines.Select(line => (line.From, line.Module)).ShouldBe([("corporate-tax", RiverModule.CorporateTax), ("taxes_on_profit", null)]);
    }

    // Company year starts the river: it receives nothing.
    [Fact]
    public void Of_companyYear_receivesNothing()
    {
        Of(Full, RiverModule.CompanyYear).Lines.ShouldBeEmpty();
    }

    // A lever that moves no money of its own receives nothing: the chain's line runs on past it, and the box
    // under the quiet levers receives it from the box above them.
    [Fact]
    public void Of_aQuietLever_receivesNothing_andTheBoxUnderItReceivesFromTheBoxAbove()
    {
        var (quiet, split) = (Of(MapReceivedHtml.ChainQuiet, RiverModule.Rent), Of(MapReceivedHtml.ChainQuiet, RiverModule.Split));

        quiet.ShouldSatisfyAllConditions(
            () => quiet.Lines.ShouldBeEmpty(),
            () => split.Lines.Select(line => line.Module).ShouldBe([RiverModule.Vouchers]));
    }

    // While the engine refuses an amount the map draws no line, so nothing is received.
    [Fact]
    public void Of_aRefusedAmount_receivesNothing()
    {
        Of(Full.WithReserve(-1m), RiverModule.Split).Lines.ShouldBeEmpty();
    }
}
