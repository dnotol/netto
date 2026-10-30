using Netto.Web.Client;

namespace Netto.Tests.Support;

// The browser tab's own storage kept in memory, for a test to stand in for sessionStorage (IBrowserSession):
// what is set is there until it is taken, once.
internal sealed class BrowserSession : IBrowserSession
{
    private readonly Dictionary<string, string> _stored = [];

    // What is stored under the key, without taking it.
    public string? Peek(string key) => _stored.GetValueOrDefault(key);

    public string? Take(string key) => _stored.Remove(key, out var value) ? value : null;

    public void Set(string key, string value) => _stored[key] = value;
}
