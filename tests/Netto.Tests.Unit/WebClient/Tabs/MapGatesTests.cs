using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What each gate of the map says in its box, from the one run of the river: what crosses and what reaches
// the viewer, each a figure its card shows, what reaches the viewer the very amount of the gate's lines into
// the pocket, which carry none of their own; and the words of the pay's pill. A line of nothing is left out,
// so a lever that moves no money says nothing. That each figure is one its card draws is MapGatesCardsTests'.
public class MapGatesTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // Every lever in the plan, none with an amount: each moves no money.
    private static readonly RiverInputs EveryLeverEmpty = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module));

    private static readonly MapEnd Pocket = MapEnd.InBucket("pocket");

    public static TheoryData<string> Rivers => new()
    {
        "every lever", "the company paying the contributions", "the example", "every lever empty", "a rent under its floor", "a car without contribution",
        "the dividend all kept",
    };

    internal static RiverInputs River(string name) => name switch
    {
        "every lever" => Full,
        "the company paying the contributions" => Full.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.035m),
        "the example" => RiverInputs.Example,
        "every lever empty" => EveryLeverEmpty,
        "a rent under its floor" => Full.WithRent(1m),
        "a car without contribution" => Full.WithCarContribution(0m),
        "the dividend all kept" => Full.WithReserve(1_000_000m),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    [Fact]
    public void Of_aRefusedRun_saysNothing()
    {
        var river = Full.WithReserve(-1m);
        var run = river.Run();
        run.Error.ShouldNotBeNull();

        var gates = MapGates.Of(river, run);

        (gates.Gates.Count, gates.Pay).ShouldBe((0, null));
    }

    [Fact]
    public void Of_theCompanySkipped_hasNoGateAndNoPill()
    {
        var river = Full.SkipCompany();

        var gates = MapGates.Of(river, river.Run());

        (gates.Gates.Count, gates.Pay).ShouldBe((0, null));
    }

    // Every lever with figures: each gate, in the river's order, says what crosses and, in gold, what reaches
    // the viewer; the vouchers and the allowances what reaches the viewer, untaxed; the car its benefit on the
    // pay and what the viewer pays for it.
    [Fact]
    public void Of_everyLever_saysWhatCrossesAndWhatReachesYou()
    {
        var run = Full.Run();
        var year = run.Year!;
        var links = MapLinks.Of(Full, run);

        var gates = MapGates.Of(Full, run);

        gates.Gates.Select(gate => (gate.Module, Lines: string.Join(" | ", gate.Lines.Select(Say)))).ShouldBe([
            (RiverModule.Vouchers, $"{MapGateText.ToYou} {ToPocket(links, RiverModule.Vouchers)} gold | {MapGateText.Untaxed}"),
            (RiverModule.Allowances, $"{MapGateText.ToYou} {ToPocket(links, RiverModule.Allowances)} gold | {MapGateText.Untaxed}"),
            (RiverModule.Rent, $"{MapGateText.Across} {year.Rent.Rent} | {MapGateText.ToYou} {RentView.From(year, Full.Composition).AfterTax} gold"),
            (RiverModule.Car, $"{MapGateText.Benefit} {year.Car.Benefit} | {MapGateText.YouPay} {year.Car.Contribution}"),
            (RiverModule.Copyright, $"{MapGateText.Across} {year.Copyright.Royalty} | {MapGateText.ToYou} {CopyrightView.From(year).AfterTax} gold"),
            (RiverModule.Dividend, $"{MapGateText.Across} {year.Dividend.Dividend} | {MapGateText.ToYou} {year.Dividend.NetDividend} gold"),
        ]);
    }

    // A gold line from a gate into the pocket carries no amount of its own: its gate's box says it, the very
    // sum the map makes of the gate's lines into the pocket (MapBundle.Sum). No gate says what reaches the
    // viewer without such a line, and none has such a line without saying it.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void Of_whatReachesYou_isWhatTheGatesGoldLinesCarry(string name)
    {
        var river = River(name);
        var run = river.Run();
        var links = MapLinks.Of(river, run);

        var gates = MapGates.Of(river, run);

        var said = gates.Gates.SelectMany(gate => gate.Lines.Where(line => line.ToYou).Select(line => (gate.Module, line.Amount))).ToList();
        var drawn = gates.Gates.Select(gate => gate.Module)
            .Select(module => (Module: module, Links: links.Where(link => link.From == MapEnd.Of(module) && link.To == Pocket).ToList()))
            .Where(gate => gate.Links.Count > 0).Select(gate => (gate.Module, (decimal?)MapBundle.Sum(gate.Links))).ToList();
        said.ShouldBe(drawn);
    }

    // Every lever in the plan and none with an amount: each is quiet, and says nothing.
    [Fact]
    public void Of_aQuietLever_saysNothing()
    {
        var gates = MapGates.Of(EveryLeverEmpty, EveryLeverEmpty.Run());

        gates.Gates.Where(gate => RiverModules.Lever(gate.Module) is not null).Select(gate => (gate.Module, gate.Lines.Count)).ShouldBe([
            (RiverModule.Vouchers, 0), (RiverModule.Allowances, 0), (RiverModule.Rent, 0), (RiverModule.Car, 0), (RiverModule.Copyright, 0)]);
    }

    // The office rule keeps an Expense allowances paying nothing in its box beside a rent, with no money: it
    // says nothing.
    [Fact]
    public void Of_allowancesPayingNothingBesideARent_sayNothing()
    {
        var river = EveryLeverEmpty.WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

        var gates = MapGates.Of(river, river.Run());

        gates.For(RiverModule.Allowances).ShouldBeEmpty();
    }

    // The car's benefit alone, when the viewer pays nothing for it: a line of nothing is left out.
    [Fact]
    public void Of_aCarWithoutContribution_saysItsBenefitAlone()
    {
        var river = Full.WithCarContribution(0m);

        var gates = MapGates.Of(river, river.Run());

        gates.For(RiverModule.Car).Select(line => line.Key).ShouldBe([MapGateText.Benefit]);
    }

    // A rent under its floor is taxed on the floor, but the company pays the rent: that is what crosses. Its tax
    // can be more than the rent, and what reaches the viewer is then the card's own figure, below nothing.
    [Fact]
    public void Of_aRentUnderItsFloor_saysTheRentAcrossAndWhatTheTaxLeavesSigned()
    {
        var river = Full.WithRent(1m);
        var year = river.Run().Year!;
        var view = RentView.From(year, river.Composition);
        view.TaxedOnTheFloor.ShouldBeTrue();
        view.AfterTax.ShouldBeLessThan(0m);

        var gates = MapGates.Of(river, river.Run());

        gates.For(RiverModule.Rent).Select(line => (line.Key, line.Amount)).ShouldBe([(MapGateText.Across, 1m), (MapGateText.ToYou, view.AfterTax)]);
    }

    // A royalty beyond its limits crosses whole; only its part within them, after its tax, reaches the viewer
    // from the gate, the part taxed as pay reaching them through Director pay.
    [Fact]
    public void Of_aRoyaltyBeyondItsLimits_saysTheWholeRoyaltyAcrossAndItsPartWithinToYou()
    {
        var year = Full.Run().Year!;
        year.Copyright.TaxedAsPay.ShouldBeGreaterThan(0m);

        var gates = MapGates.Of(Full, Full.Run());

        gates.For(RiverModule.Copyright).Select(line => line.Amount)
            .ShouldBe([year.Copyright.Royalty, CopyrightView.From(year).AfterTax]);
    }

    // The withholding refunded on the exempt dividend comes back to it: what reaches the viewer is the net
    // dividend with the refund in it.
    [Fact]
    public void Of_aDividendWithARefund_saysTheNetDividendRefundIncluded()
    {
        var year = Full.Run().Year!;
        year.Dividend.ExemptionRefund.ShouldBeGreaterThan(0m);

        var gates = MapGates.Of(Full, Full.Run());

        gates.For(RiverModule.Dividend).Single(line => line.ToYou).Amount.ShouldBe(year.Dividend.NetDividend);
    }

    // A company that keeps it all pays no dividend: the gate says nothing.
    [Fact]
    public void Of_theDividendAllKept_saysNothing()
    {
        var river = Full.WithReserve(1_000_000m);
        river.Run().Year!.Dividend.Dividend.ShouldBe(0m);

        var gates = MapGates.Of(river, river.Run());

        gates.For(RiverModule.Dividend).ShouldBeEmpty();
    }

    // The pay crosses the middle in its pill, its amount the split's line to Director pay as the map bundles it.
    [Fact]
    public void Of_thePay_isThePillsAmount()
    {
        var run = Full.Run();

        var pay = MapGates.Of(Full, run).Pay.ShouldNotBeNull();

        (pay.Key, pay.Amount, pay.ToYou).ShouldBe((MapGateText.Pay(withContributions: false), run.Year!.DirectorPay.GrossPay, false));
    }

    // The contributions the company pays on the director's behalf, with the fund's fee, cross with the pay: the
    // pill says so, and their sum.
    [Fact]
    public void Of_theCompanyPayingTheContributions_pillsThePayAndContributions()
    {
        var river = River("the company paying the contributions");
        var run = river.Run();
        var links = MapLinks.Of(river, run).Where(link => link.From == MapEnd.Of(RiverModule.Split) && link.To == MapEnd.Of(RiverModule.DirectorPay)).ToList();
        links.Count.ShouldBe(3);

        var pay = MapGates.Of(river, run).Pay.ShouldNotBeNull();

        (pay.Key, pay.Amount).ShouldBe((MapGateText.Pay(withContributions: true), MapBundle.Sum(links)));
    }

    [Fact]
    public void Of_noPay_hasNoPill()
    {
        var river = RiverInputs.Example.WithDirectorPay(0m);
        river.Run().Error.ShouldBeNull();

        MapGates.Of(river, river.Run()).Pay.ShouldBeNull();
    }

    public static TheoryData<RiverModule> Modules => new(MapModules.Order);

    // Every module of the map: a gate its lines, any other none.
    [Theory]
    [MemberData(nameof(Modules))]
    public void For_aModuleOfTheMap_isItsLinesOrNone(RiverModule module)
    {
        var gates = MapGates.Of(Full, Full.Run());

        var lines = gates.For(module);

        (lines.Count > 0).ShouldBe(module is RiverModule.Vouchers or RiverModule.Allowances or RiverModule.Rent or RiverModule.Car
            or RiverModule.Copyright or RiverModule.Dividend);
    }

    [Fact]
    public void For_theRiver_throws()
    {
        var gates = MapGates.Of(Full, Full.Run());

        Should.Throw<ArgumentOutOfRangeException>(() => gates.For(RiverModule.River));
    }

    private static string Say(MapGateLine line) => $"{line.Key}{(line.Amount is { } amount ? $" {amount}" : "")}{(line.ToYou ? " gold" : "")}";

    private static decimal ToPocket(IReadOnlyList<MapLink> links, RiverModule module) =>
        MapBundle.Sum(links.Where(link => link.From == MapEnd.Of(module) && link.To == Pocket));
}
