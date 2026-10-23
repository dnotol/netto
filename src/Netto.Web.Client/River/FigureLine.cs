namespace Netto.Web.Client.River;

// One line of how a card's figure is built, as its view model gives it, with keys of the card's texts
// rather than words (FigureWords words it, FigureList draws it): the key of its label, with the label's
// arguments already formatted (a deduction rate, a period's dates), and its amount; then, when it has
// them, its share of what its card receives (Shares.Of, made by the view model, never by the list), the
// key of what its bubble says with that text's arguments, and how its amount reads. A line that takes
// from the figure carries its sign in its amount: a negative amount reads −€1,333.33.
internal sealed record FigureLine(string Key, decimal Amount)
{
    public IReadOnlyList<string> Args { get; init; } = [];

    public decimal? Share { get; init; }

    // None for a line whose label needs no explaining.
    public string? Help { get; init; }

    public IReadOnlyList<string> HelpArgs { get; init; } = [];

    public FigureFormat Format { get; init; }
}

// How a figure line's amount reads. Public because Figure, which FigureList takes, carries it.
public enum FigureFormat
{
    // In euros, through Formats.Euros: €3,333.33.
    Euros,

    // A share such as a car's CO2 percentage, with its decimals, through Formats.Percentage: 9.75%.
    Percentage,
}
