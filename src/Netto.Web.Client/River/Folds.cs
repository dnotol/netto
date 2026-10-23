namespace Netto.Web.Client.River;

// Which folds of the river are open. Two things decide it, and they are not the same kind:
//
//  - What the viewer did (Set): opened or closed a fold, kept in the browser so a reload keeps it, a
//    preference of the viewer's and never part of the plan's inputs. A fold the viewer has not touched
//    has its default, which for a card is folded to its summary and for what a card holds besides its
//    figures, such as its conditions, folded too.
//  - What the page does for the viewer (Reveal): a fold it must show, a lever just added so it can be
//    filled, a card that holds a refusal. It is open for the session only, over the fold's default and
//    over a fold the viewer made, and is never stored: after a reload the river is back to its
//    summaries. The viewer's next Set replaces it.
//
// Neither notifies anything: a fold reads its state when it is drawn, so a change made outside it shows
// the next time its page draws.
internal sealed class Folds(IBrowserStorage storage)
{
    internal const string StorageKey = "netto.folds";

    private readonly Dictionary<string, bool> _open = Parse(storage.Get(StorageKey));
    private readonly HashSet<string> _revealed = [];

    public bool IsOpen(string name, bool openByDefault) => _revealed.Contains(name) || _open.GetValueOrDefault(name, openByDefault);

    // The viewer's word: remembered, and it replaces a reveal.
    public void Set(string name, bool open)
    {
        _revealed.Remove(name);
        _open[name] = open;
        storage.Set(StorageKey, string.Join(';', _open.Select(fold => $"{fold.Key}={(fold.Value ? 1 : 0)}")));
    }

    // The page's: open for the session, not remembered.
    public void Reveal(string name) => _revealed.Add(name);

    // Each fold the viewer touched as name=1, open, or name=0, folded; anything else is ignored.
    private static Dictionary<string, bool> Parse(string? stored)
    {
        var open = new Dictionary<string, bool>();
        foreach (var entry in (stored ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (entry.Split('=') is [{ Length: > 0 } name, ("0" or "1") and var state])
                open[name] = state == "1";
        }

        return open;
    }
}
