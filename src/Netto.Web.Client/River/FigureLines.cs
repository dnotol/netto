namespace Netto.Web.Client.River;

// The lines a figure list shows, on every card: only those that move its figure. A line of nothing is
// left out, as a drain list leaves out a line that moves no money (DrainLines, which says why nothing is
// what Formats reads €0.00, the engine's negative zero among it).
internal static class FigureLines
{
    public static IReadOnlyList<FigureLine> Shown(IEnumerable<FigureLine> lines) => [.. lines.Where(line => !IsNothing(line.Amount))];

    // What Formats reads €0.00, whatever its sign: no line is worth showing for it, wherever the line is drawn.
    public static bool IsNothing(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero) == 0m;
}
