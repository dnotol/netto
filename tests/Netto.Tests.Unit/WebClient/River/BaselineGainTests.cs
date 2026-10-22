using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The river's example leaves 61,975.74 in the pocket, and 64,526.87 at the best pay of 20,799
// (BucketsViewTests): the best pay gains 2,551.13 over the example.
public class BaselineGainTests
{
    private static readonly RiverRun Example = RiverInputs.Example.Run();
    private static readonly RiverRun BestPay = RiverInputs.Example.WithDirectorPay(20_799m).Run();

    [Fact]
    public void From_noSetup_isNothing()
    {
        BaselineGain.From(Example, null).ShouldBeNull();
    }

    [Fact]
    public void From_riverLeavesMore_isTheDifferenceMore()
    {
        var gain = BaselineGain.From(BestPay, Example);

        gain.ShouldBe(new BaselineGain(GainKind.More, 2_551.13m));
    }

    [Fact]
    public void From_riverLeavesLess_isTheDifferenceLess()
    {
        var gain = BaselineGain.From(Example, BestPay);

        gain.ShouldBe(new BaselineGain(GainKind.Less, 2_551.13m));
    }

    [Fact]
    public void From_sameRiver_isTheSame()
    {
        var gain = BaselineGain.From(Example, RiverInputs.Example.Run()).ShouldNotBeNull();

        (gain.Kind, gain.Amount, decimal.IsNegative(gain.Amount)).ShouldBe((GainKind.Same, 0m, false));
    }

    [Fact]
    public void From_refusedRiver_isNothing()
    {
        BaselineGain.From(RiverInputs.Example.WithReserve(-1m).Run(), Example).ShouldBeNull();
    }

    [Fact]
    public void From_refusedSetup_isNothing()
    {
        BaselineGain.From(Example, RiverInputs.Example.WithReserve(-1m).Run()).ShouldBeNull();
    }

    // A river that starts at the pay leaves out the dividend, so its pocket is not the year's.
    [Fact]
    public void From_riverStartsAtThePaySetupDoesNot_cannotCompare()
    {
        var gain = BaselineGain.From(RiverInputs.Example.SkipCompany().Run(), Example);

        gain.ShouldBe(new BaselineGain(GainKind.NotComparable, 0m));
    }

    [Fact]
    public void From_setupStartsAtThePayRiverDoesNot_cannotCompare()
    {
        var gain = BaselineGain.From(Example, RiverInputs.Example.SkipCompany().Run());

        gain.ShouldBe(new BaselineGain(GainKind.NotComparable, 0m));
    }

    // Both at the pay, the pockets are the net of each pay: 50,000 against 40,000.
    [Fact]
    public void From_bothStartAtThePay_comparesTheirPockets()
    {
        var river = RiverInputs.Example.SkipCompany().Run();
        var setup = RiverInputs.Example.SkipCompany().WithDirectorPay(40_000m).Run();

        var gain = BaselineGain.From(river, setup).ShouldNotBeNull();

        (gain.Kind, gain.Amount).ShouldBe((GainKind.More, river.Pay!.NetInPocket - setup.Pay!.NetInPocket));
    }

    [Fact]
    public void CanMark_example_isTrue()
    {
        BaselineGain.CanMark(Example).ShouldBeTrue();
    }

    // There is no figure to keep while the engine refuses an amount.
    [Fact]
    public void CanMark_refusedRiver_isFalse()
    {
        BaselineGain.CanMark(RiverInputs.Example.WithReserve(-1m).Run()).ShouldBeFalse();
    }
}
