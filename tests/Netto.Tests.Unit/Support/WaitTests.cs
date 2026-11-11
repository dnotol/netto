using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.Support;

// The paint or pause a source of the plan page waits for, and the plan on screen it reads, as the tests stand
// in for them.
public class WaitTests
{
    [Fact]
    public void After_notHeld_isDoneAndCounted()
    {
        var wait = new Wait();

        var waited = wait.After();

        (waited.IsCompleted, wait.Asked).ShouldBe((true, 1));
    }

    [Fact]
    public async Task After_held_isDoneOnlyOnceReleased()
    {
        var wait = new Wait(held: true);

        var waited = wait.After();
        var before = waited.IsCompleted;
        wait.Release();
        await waited;

        (before, waited.IsCompleted, wait.Asked).ShouldBe((false, true, 1));
    }

    [Fact]
    public void Screen_plan_isThePlanLastSet()
    {
        var screen = new Screen(RiverInputs.Example);

        screen.Plan = RiverInputs.Example.WithDirectorPay(30_000m);

        screen.Plan.DirectorPay.Value.ShouldBe(30_000m);
    }
}
