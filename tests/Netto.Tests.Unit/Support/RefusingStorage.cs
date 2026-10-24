using Netto.Web.Client;

namespace Netto.Tests.Support;

// A browser that refuses storage, as a private window or blocked site data does: nothing is read and
// nothing is kept, and no call fails.
internal sealed class RefusingStorage : IBrowserStorage
{
    public string? Get(string key) => null;

    public void Set(string key, string value)
    {
    }
}
