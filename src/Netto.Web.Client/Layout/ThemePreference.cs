namespace Netto.Web.Client.Layout;

// Netto's two themes: dark, Netto's own, and light, designed beside it (NettoTheme, app.css).
public enum ThemeMode
{
    Dark,
    Light,
}

// The page the theme is drawn on: what the viewer's system asks for, as it is and each time it changes
// while the page is open, and the theme set on the page for app.css (nettoTheme in Netto.Web's
// wwwroot/theme.js). An interface because the tests stand in for the browser.
internal interface IThemePage
{
    bool SystemPrefersLight { get; }

    void Show(ThemeMode theme);

    // Called back with whether the system now prefers light, each time its setting changes.
    void WatchSystem(Action<bool> changed);
}

// The viewer's theme: the one they picked with the theme switch (ThemeSwitch), kept in the browser so a reload
// keeps it, or while they have picked none, their system's, followed as it changes. A preference of
// the viewer's, never part of a plan. theme.js reads the same key to paint the page in it before the
// app runs.
internal sealed class ThemePreference
{
    internal const string StorageKey = "netto.theme";

    private readonly IBrowserStorage _storage;
    private readonly IThemePage _page;
    private ThemeMode? _picked;
    private bool _systemPrefersLight;

    public ThemePreference(IBrowserStorage storage, IThemePage page)
    {
        _storage = storage;
        _page = page;
        _picked = Parse(storage.Get(StorageKey));
        _systemPrefersLight = page.SystemPrefersLight;
        page.WatchSystem(SystemChanged);
    }

    // The theme shown changed, picked or the system's: what draws in it beside the page's root, MudBlazor's
    // palette in the layout, draws again.
    public event Action? Changed;

    public ThemeMode Current => _picked ?? (_systemPrefersLight ? ThemeMode.Light : ThemeMode.Dark);

    public bool IsDark => Current == ThemeMode.Dark;

    // The other theme, picked: from now on the system's setting no longer decides.
    public void Switch()
    {
        var picked = IsDark ? ThemeMode.Light : ThemeMode.Dark;
        _picked = picked;
        _storage.Set(StorageKey, Stored(picked));
        _page.Show(picked);
        Changed?.Invoke();
    }

    // The system's setting changed while the page is open: followed only while nothing is picked.
    private void SystemChanged(bool prefersLight)
    {
        var shown = Current;
        _systemPrefersLight = prefersLight;
        if (Current == shown)
            return;

        _page.Show(Current);
        Changed?.Invoke();
    }

    private static string Stored(ThemeMode theme) => theme == ThemeMode.Light ? "light" : "dark";

    // Only the two words this class writes count as a pick; anything else is none.
    private static ThemeMode? Parse(string? stored) => stored switch
    {
        "dark" => ThemeMode.Dark,
        "light" => ThemeMode.Light,
        _ => null,
    };
}
