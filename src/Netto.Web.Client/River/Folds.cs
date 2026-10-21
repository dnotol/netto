namespace Netto.Web.Client.River;

// Which folds of the river the viewer opened or closed, kept in the browser so a reload keeps them:
// a preference of the viewer's, never part of the plan's inputs. A fold the viewer has not touched
// has its default, a card open and its conditions folded.
internal sealed class Folds(IBrowserStorage storage)
{
    internal const string StorageKey = "netto.folds";

    private readonly Dictionary<string, bool> _open = Parse(storage.Get(StorageKey));

    public bool IsOpen(string name, bool openByDefault) => _open.GetValueOrDefault(name, openByDefault);

    public void Set(string name, bool open)
    {
        _open[name] = open;
        storage.Set(StorageKey, string.Join(';', _open.Select(fold => $"{fold.Key}={(fold.Value ? 1 : 0)}")));
    }

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
