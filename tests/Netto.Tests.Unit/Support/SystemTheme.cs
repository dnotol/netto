using Netto.Web.Client.Layout;

namespace Netto.Tests.Support;

// The viewer's system and the page the theme is drawn on, for a test to stand in for the browser
// (IThemePage): the system prefers light or not as given, and changes when the test says so, as the
// browser's setting would, telling whoever watches it; the themes the page was told to show are kept in
// order.
internal sealed class SystemTheme(bool prefersLight) : IThemePage
{
    private readonly List<Action<bool>> _watchers = [];

    public List<ThemeMode> Shown { get; } = [];

    public bool SystemPrefersLight { get; private set; } = prefersLight;

    public void Show(ThemeMode theme) => Shown.Add(theme);

    public void WatchSystem(Action<bool> changed) => _watchers.Add(changed);

    public void ChangeSystem(bool prefersLight)
    {
        SystemPrefersLight = prefersLight;
        foreach (var watcher in _watchers)
            watcher(prefersLight);
    }
}
