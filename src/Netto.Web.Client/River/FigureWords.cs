using Microsoft.Extensions.Localization;

namespace Netto.Web.Client.River;

// A line of a figure list once worded by its card's texts: its label and what its bubble says, with
// its amount, its share and how its amount reads as its view model gave them. Public because
// FigureList, a component, takes it.
public sealed record Figure(string Label, decimal Amount)
{
    public decimal? Share { get; init; }

    // None for a line with no bubble.
    public string? Help { get; init; }

    public FigureFormat Format { get; init; }
}

// The lines of a figure list worded by the texts of the card that shows it, as SettingWords words a
// disclosure's line: the view model gives keys and amounts, the card its words, so the list has no text
// of its own.
internal static class FigureWords
{
    public static IReadOnlyList<Figure> Of(IStringLocalizer texts, IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => new Figure(texts[line.Key, [.. line.Args]].Value, line.Amount)
        {
            Share = line.Share,
            Help = line.Help is { } help ? texts[help, [.. line.HelpArgs]].Value : null,
            Format = line.Format,
        })];
}
