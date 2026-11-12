namespace Netto.Web.Client.River;

// The lines a drain list shows, on every card: only those that move money. A line of nothing, a
// credit lost of nothing or a surcharge of nothing, is left out of the list, while its step stays in
// the ledger and in the card's sources. Nothing is what Formats reads €0.00, rounding half away from
// zero to the cent, the negative zero the engine gives for a credit of nothing among it.
internal static class DrainLines
{
    public static IReadOnlyList<Drain> Shown(IEnumerable<Drain> drains) =>
        [.. drains.Where(drain => !FigureLines.IsNothing(drain.Amount))];
}
