namespace Netto.Web.Client.River;

// The cards on the page, for the river's outline (RiverOutline). Each card's frame (ModuleCard) reports
// its name, title, glance and default fold every time it is drawn, and leaves as it goes, so the outline
// lists exactly the cards on screen, an optional module only while it is in the plan, and only the
// figures the cards show: it computes nothing of its own. The page makes one for its river and hands it
// down to the cards (Plan, CascadingValue).
//
// It also opens and folds the cards for the outline, as the viewer's word (Folds.Set), and draws again
// the card that changed, since a fold reads its state only when it is drawn; and it says which card the
// river has in view, from where the cards stand in the window (InView).
internal sealed class OutlineRows(Folds folds)
{
    // The cards in the order a reader meets them, the page's: the levers before the split, then the
    // director's stream, the royalty first, before the company's. A new card takes its place here, or
    // drawing it fails.
    public static IReadOnlyList<string> Order { get; } =
    [
        "moves",
        "company-year",
        ModuleFolds.Name(OptionalModule.Vouchers),
        ModuleFolds.Name(OptionalModule.Allowances),
        ModuleFolds.Name(OptionalModule.Rent),
        ModuleFolds.Name(OptionalModule.Car),
        "split",
        ModuleFolds.Name(OptionalModule.Copyright),
        "director-pay",
        "corporate-tax",
        "dividend",
        "calendar",
    ];

    // How far past the line a card's top may sit and still count as on it: a card scrolled to the line
    // lands there to the nearest pixel.
    private const double Tolerance = 1;

    private readonly Dictionary<string, (OutlineRow Row, Action Redraw)> _cards = [];

    // A card came, went, or shows another title or glance.
    public event Action? Changed;

    public IReadOnlyList<OutlineRow> Rows => [.. Order.Where(_cards.ContainsKey).Select(name => _cards[name].Row)];

    // Whether every card listed is open, by the viewer, for the visit or by default.
    public bool AllOpen => _cards.Values.All(card => IsOpen(card.Row));

    // The card as it is drawn, with how to draw it again.
    public void Report(OutlineRow row, Action redraw)
    {
        if (!Order.Contains(row.Name))
            throw new ArgumentOutOfRangeException(nameof(row), row.Name, "A card of the river has no place in the outline's order.");

        var changed = !_cards.TryGetValue(row.Name, out var known) || known.Row != row;
        _cards[row.Name] = (row, redraw);
        if (changed)
            Changed?.Invoke();
    }

    public void Leave(string name)
    {
        if (_cards.Remove(name))
            Changed?.Invoke();
    }

    // Opens a folded card, remembered as the viewer's fold; a card already open is left as it is.
    public void Open(string name)
    {
        if (!_cards.TryGetValue(name, out var card) || IsOpen(card.Row))
            return;

        folds.Set(name, true);
        card.Redraw();
    }

    // « Open all » or « Fold all »: the viewer's word on every card listed.
    public void SetAll(bool open)
    {
        foreach (var (name, card) in _cards.ToList())
        {
            folds.Set(name, open);
            card.Redraw();
        }
    }

    // The card the river has in view, from the cards' boxes in the river's order, measured from the
    // window's top, and the line under the bar of buckets: of the cards whose top has reached the line,
    // the one whose heading passed it last among those still reaching below it, or the last to pass
    // when the line falls between two cards. Of two streams starting together, the first in the river.
    // Before any card reaches the line, the first; once the page has reached its end, the last, which
    // may never reach the line.
    public static string? InView(IReadOnlyList<CardBox> boxes, double line, bool atEnd)
    {
        if (boxes.Count == 0)
            return null;

        if (atEnd)
            return boxes[^1].Name;

        var passed = boxes.Where(box => box.Top <= line + Tolerance).ToList();
        if (passed.Count == 0)
            return boxes[0].Name;

        var reaching = passed.Where(box => box.Bottom > line).ToList();

        return (reaching.Count > 0 ? reaching : passed).MaxBy(box => box.Top)!.Name;
    }

    private bool IsOpen(OutlineRow row) => folds.IsOpen(row.Name, row.OpenByDefault);
}

// A card as the outline lists it: its fold's name, its title, its glance, none while its summary has no
// figure, and whether it opens by default.
internal sealed record OutlineRow(string Name, string Title, CardGlance? Glance, bool OpenByDefault);

// Where a card stands in the window: its top and its bottom, in pixels from the window's top.
internal sealed record CardBox(string Name, double Top, double Bottom);
