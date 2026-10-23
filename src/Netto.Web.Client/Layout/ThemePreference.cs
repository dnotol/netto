namespace Netto.Web.Client.Layout;

// Netto's two themes: dark, Netto's own, and light, designed beside it (NettoTheme, app.css).
public enum ThemeMode
{
    Dark,
    Light,
}

// The page the theme is drawn on: what the viewer's system asks for, and the theme set on the page
// for app.css (nettoTheme in Netto.Web's wwwroot/theme.js). An interface because the tests stand in
// for the browser.
internal interface IThemePage
{
    bool SystemPrefersLight { get; }

    void Show(ThemeMode theme);
}

// The viewer's theme: the one they picked with the header's switch, kept in the browser so a reload
// keeps it, or while they have picked none, their system's, followed as it changes. A preference of
// the viewer's, never part of a plan. theme.js reads the same key to paint the page in it before the
// app runs.
internal sealed class ThemePreference(IBrowserStorage storage, IThemePage page)
{
    internal const string StorageKey = "netto.theme";

    private ThemeMode? _picked = Parse(storage.Get(StorageKey));
    private bool _systemPrefersLight = page.SystemPrefersLight;

    public ThemeMode Current => _picked ?? (_systemPrefersLight ? ThemeMode.Light : ThemeMode.Dark);

    public bool IsDark => Current == ThemeMode.Dark;

    // The other theme, picked: from now on the system's setting no longer decides.
    public void Switch()
    {
        var picked = IsDark ? ThemeMode.Light : ThemeMode.Dark;
        _picked = picked;
        storage.Set(StorageKey, Stored(picked));
        page.Show(picked);
    }

    // The system's setting changed while the page is open: followed only while nothing is picked.
    public void SystemChanged(bool prefersLight)
    {
        _systemPrefersLight = prefersLight;
        if (_picked is null)
            page.Show(Current);
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
