namespace Netto.Web.Client.River;

// The lines a figure list shows, on every card: only those that move its figure. A line of nothing is
// left out, as a drain list leaves out a line that moves no money (DrainLines, which says why nothing is
// what Formats reads €0.00, the engine's negative zero among it).
internal static class FigureLines
{
    public static IReadOnlyList<FigureLine> Shown(IEnumerable<FigureLine> lines) =>
        [.. lines.Where(line => Math.Round(line.Amount, 2, MidpointRounding.AwayFromZero) != 0m)];
}
