namespace Netto.Web.Client.River;

// What the river leaves in the pocket against the setup the user marked as theirs (RiverState.Setup):
// the difference of the two runs' net in pocket, to the cent. Nothing without a setup, or while the
// engine refuses either, the bar then having no figure. A river that starts at the pay and one that
// starts at the company's year are not compared, since the first leaves out the dividend. The gain is
// in cash and nothing else, a plan with a company car's as any other's (operator, 2026-10-23).
internal sealed record BaselineGain(GainKind Kind, decimal Amount)
{
    public static BaselineGain? From(RiverRun river, RiverRun? setup)
    {
        if (setup is null || Pocket(river) is not { } pocket || Pocket(setup) is not { } setupPocket)
            return null;
        if ((river.Year is null) != (setup.Year is null))
            return new BaselineGain(GainKind.NotComparable, 0m);

        // A plain zero when the two leave the same, never a negative one (0m - 0.00m).
        return pocket.CompareTo(setupPocket) switch
        {
            > 0 => new BaselineGain(GainKind.More, pocket - setupPocket),
            < 0 => new BaselineGain(GainKind.Less, setupPocket - pocket),
            _ => new BaselineGain(GainKind.Same, 0m),
        };
    }

    // A setup is the river with its figures: none to keep while the engine refuses an amount.
    public static bool CanMark(RiverRun river) => Pocket(river) is not null;

    // As the bar's pocket reads it (BucketsView): the year's, or the pay's with the company skipped.
    private static decimal? Pocket(RiverRun run) =>
        run.Error is not null ? null : run.Year?.NetInPocket ?? run.Pay?.NetInPocket;
}

public enum GainKind
{
    More,
    Less,
    Same,
    NotComparable,
}
