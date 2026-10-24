using Netto.Web.Client;

namespace Netto.Tests.Support;

// The browser's storage kept in memory, for a test to stand in for localStorage (IBrowserStorage), and
// counting what is written to it, for a test that holds a read to write nothing.
internal sealed class BrowserStorage : IBrowserStorage
{
    private readonly Dictionary<string, string> _stored = [];

    public int Writes { get; private set; }

    public string? Get(string key) => _stored.GetValueOrDefault(key);

    public void Set(string key, string value)
    {
        Writes++;
        _stored[key] = value;
    }
}
