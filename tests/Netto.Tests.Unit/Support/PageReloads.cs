using Netto.Web.Client.Layout;

namespace Netto.Tests.Support;

// The page a language is switched on, for a test to stand in for the browser (ILanguagePage): reloading it is
// counted, and what was done before it can be checked against when it came.
internal sealed class PageReloads : ILanguagePage
{
    // Called as the page reloads, for a test to see what was kept by then.
    private readonly Action? _reloading;

    public PageReloads(Action? reloading = null) => _reloading = reloading;

    public int Reloads { get; private set; }

    public void Reload()
    {
        _reloading?.Invoke();
        Reloads++;
    }
}
