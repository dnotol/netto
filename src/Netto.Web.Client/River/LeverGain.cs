namespace Netto.Web.Client.River;

// What a lever in the plan adds to the pocket: the pocket of the plan's own run less the pocket of the
// same river with that lever taken out of the plan, in cash as every gain is, the difference of two runs of
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

// How a lever's card says its gain, the key of its summary in the card's own texts: more in the pocket, less,
// or no difference, a gain Formats reads €0.00 being none. The summary states the gain's size, its way in the
// words; it ends on the gain over what the company pays out for the lever, only while the lever adds more
// than nothing: a lever that takes from the pocket, or changes nothing, ends on no rate, never a negative
// one beside « less », and none while it pays out nothing.
internal static class LeverGainText
{
    public static string Summary(decimal gain) => gain switch
    {
        _ when FigureLines.IsNothing(gain) => "Summary_None",
        > 0m => "Summary_More",
        _ => "Summary_Less",
    };

    public static string Amount(decimal gain) => Formats.Euros(Math.Abs(gain));

    public static decimal? OfThePayout(LeverGain? gain, decimal payout) =>
        gain is { Gain: > 0m } && !FigureLines.IsNothing(gain.Gain) ? Shares.Of(gain.Gain, payout) : null;
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
