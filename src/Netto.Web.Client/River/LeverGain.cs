namespace Netto.Web.Client.River;

// What a lever in the plan adds to the pocket: the pocket of the plan's own run less the pocket of the
// same river with that lever taken out of the plan, in cash (2026-10-23), the difference of two runs of
// the engine and no tax rule of its own. Both run at the pay the plan uses: the pay set, unless the
// company cannot pay so much beside the lever (YearSimulation.PayCeiling), where the plan holds it; the
// river without the lever would pay all of it, so it is given the plan's pay, or the difference would be
// the pay's as well as the lever's. PayHeld says so, the pay both run at, only then. Without is that second
// run, which the car's and the royalty's comparisons read further. None while the company is skipped, the
// engine refuses an amount, or the lever is out of the plan. A tie is a plain zero, never a negative one.
internal sealed record LeverGain(decimal PocketWith, decimal PocketWithout, decimal? PayHeld, RiverRun Without)
{
    public decimal Gain => PocketWith == PocketWithout ? 0m : PocketWith - PocketWithout;

    public static LeverGain? Of(RiverInputs inputs, RiverRun run, OptionalModule lever)
    {
        if (run.Error is not null || run.Year is not { } with || !inputs.InPlan(lever))
            return null;

        var pay = with.Scenario.DirectorPay;
        var without = inputs.TakeOutOfPlan(lever).WithDirectorPay(pay).Run();
        if (without.Year is not { } other)
            return null;

        return new LeverGain(with.NetInPocket, other.NetInPocket, pay < inputs.DirectorPay.Value ? pay : null, without);
    }
}

// A card's lever gain, kept until the card's inputs change, so that drawing the card again for anything
// else runs the river without the lever no more: the run is made only for the card on screen.
internal sealed class KeptLeverGain(OptionalModule lever)
{
    private RiverInputs? _inputs;
    private LeverGain? _gain;

    public LeverGain? For(RiverInputs inputs, RiverRun run)
    {
        if (_inputs is null || !_inputs.Equals(inputs))
        {
            _gain = LeverGain.Of(inputs, run, lever);
            _inputs = inputs;
        }

        return _gain;
    }
}
