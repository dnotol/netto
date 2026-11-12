using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What a lever in the plan adds to the pocket: the plan's own run less the run with that lever taken out,
// at the pay the plan uses, on the river's example (RiverInputsTests).
public class LeverGainTests
{
    private static readonly RiverInputs Vouchers = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers).WithMealVoucherDays(200).WithEcoVouchers(250m);

    // A royalty of 18,750 beside a pay set at 90,000: the company has 81,250 left for the pay, which the
    // plan holds there (CopyrightViewTests.FixedHeld).
    private static readonly RiverInputs Held = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(18_750m).WithDirectorPay(90_000m);

    [Fact]
    public void Of_aLeverInThePlan_isThePocketWithItLessThePocketWithoutIt()
    {
        var run = Vouchers.Run();
        var without = Vouchers.TakeOutOfPlan(OptionalModule.Vouchers).Run();

        var gain = LeverGain.Of(Vouchers, run, OptionalModule.Vouchers).ShouldNotBeNull();

        gain.ShouldSatisfyAllConditions(
            () => (gain.PocketWith, gain.PocketWithout, gain.PayHeld).ShouldBe((run.Year!.NetInPocket, without.Year!.NetInPocket, (decimal?)null)),
            () => gain.Gain.ShouldBe(run.Year!.NetInPocket - without.Year!.NetInPocket),
            () => gain.Gain.ShouldBeGreaterThan(0m));
    }

    // Without the lever the company could pay more than the pay the plan holds; both runs are at the pay
    // held, or the difference would be the pay's as well as the lever's, and the gain says that pay.
    [Fact]
    public void Of_aPlanHoldingThePay_runsBothAtThePayHeld()
    {
        var run = Held.Run();
        var pay = run.Year!.Scenario.DirectorPay;

        var gain = LeverGain.Of(Held, run, OptionalModule.Copyright).ShouldNotBeNull();

        (gain.PayHeld, gain.PocketWithout).ShouldBe(((decimal?)pay, Held.TakeOutOfPlan(OptionalModule.Copyright).WithDirectorPay(pay).Run().Year!.NetInPocket));
    }

    // A lever that changes nothing in the pocket gains a plain zero, never a negative one.
    [Fact]
    public void Of_aLeverGrantingNothing_isAPlainZero()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);

        var gain = LeverGain.Of(inputs, inputs.Run(), OptionalModule.Vouchers).ShouldNotBeNull().Gain;

        (gain, decimal.IsNegative(gain)).ShouldBe((0m, false));
    }

    [Fact]
    public void Of_aLeverOutOfThePlan_isNone()
    {
        LeverGain.Of(RiverInputs.Example, RiverInputs.Example.Run(), OptionalModule.Vouchers).ShouldBeNull();
    }

    [Fact]
    public void Of_aRefusedPlan_isNone()
    {
        var inputs = Vouchers.WithEcoVouchers(-1m);

        LeverGain.Of(inputs, inputs.Run(), OptionalModule.Vouchers).ShouldBeNull();
    }

    [Fact]
    public void Of_theCompanySkipped_isNone()
    {
        var inputs = Vouchers.SkipCompany();

        LeverGain.Of(inputs, inputs.Run(), OptionalModule.Vouchers).ShouldBeNull();
    }

    // The card keeps the gain until its inputs change: the same inputs give the same gain, unmeasured
    // again; other inputs measure it again.
    [Fact]
    public void Kept_theSameInputs_keepsTheGainAndOtherInputsMeasureItAgain()
    {
        var kept = new KeptLeverGain(OptionalModule.Vouchers);
        var first = kept.For(Vouchers, Vouchers.Run());
        var other = Vouchers.WithEcoVouchers(100m);

        kept.ShouldSatisfyAllConditions(
            () => kept.For(Vouchers, Vouchers.Run()).ShouldBeSameAs(first),
            () => kept.For(other, other.Run()).ShouldNotBeNull().Gain.ShouldBeLessThan(first.ShouldNotBeNull().Gain));
    }
}
