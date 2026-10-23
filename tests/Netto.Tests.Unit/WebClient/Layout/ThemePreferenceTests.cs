using Netto.Web.Client;
using Netto.Web.Client.Layout;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// The theme is a viewer's preference: the one picked with the header's switch, kept in the browser's
// storage, or the system's while none is picked. A new ThemePreference over the same storage is the
// page after a reload.
public class ThemePreferenceTests
{
    [Theory]
    [InlineData(true, ThemeMode.Light)]
    [InlineData(false, ThemeMode.Dark)]
    public void Current_nothingPicked_followsTheSystem(bool systemPrefersLight, ThemeMode expected)
    {
        var theme = new ThemePreference(new BrowserStorage(), new Page(systemPrefersLight));

        theme.Current.ShouldBe(expected);
    }

    [Fact]
    public void Switch_fromTheSystemsDark_turnsLight()
    {
        var theme = new ThemePreference(new BrowserStorage(), new Page(systemPrefersLight: false));

        theme.Switch();

        theme.Current.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public void Switch_twice_isBackToWhereItStarted()
    {
        var theme = new ThemePreference(new BrowserStorage(), new Page(systemPrefersLight: true));

        theme.Switch();
        theme.Switch();

        theme.Current.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public void Switch_pickKeptAfterAReload()
    {
        var storage = new BrowserStorage();
        new ThemePreference(storage, new Page(systemPrefersLight: false)).Switch();

        new ThemePreference(storage, new Page(systemPrefersLight: false)).Current.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public void Switch_pickedToTheSystemsOwn_stillKeptWhenTheSystemChanges()
    {
        var storage = new BrowserStorage();
        var theme = new ThemePreference(storage, new Page(systemPrefersLight: true));
        theme.Switch();
        theme.Switch();

        var reloaded = new ThemePreference(storage, new Page(systemPrefersLight: false));

        reloaded.Current.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public void SystemChanged_nothingPicked_isFollowed()
    {
        var theme = new ThemePreference(new BrowserStorage(), new Page(systemPrefersLight: false));

        theme.SystemChanged(prefersLight: true);

        theme.Current.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public void SystemChanged_afterAPick_isIgnored()
    {
        var theme = new ThemePreference(new BrowserStorage(), new Page(systemPrefersLight: false));
        theme.Switch();

        theme.SystemChanged(prefersLight: false);

        theme.Current.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public void Switch_showsTheNewThemeOnThePage()
    {
        var page = new Page(systemPrefersLight: false);
        var theme = new ThemePreference(new BrowserStorage(), page);

        theme.Switch();
        theme.Switch();

        page.Shown.ShouldBe([ThemeMode.Light, ThemeMode.Dark]);
    }

    [Fact]
    public void SystemChanged_nothingPicked_showsTheSystemsTheme()
    {
        var page = new Page(systemPrefersLight: false);
        var theme = new ThemePreference(new BrowserStorage(), page);

        theme.SystemChanged(prefersLight: true);

        page.Shown.ShouldBe([ThemeMode.Light]);
    }

    [Fact]
    public void SystemChanged_afterAPick_showsNothing()
    {
        var page = new Page(systemPrefersLight: false);
        var theme = new ThemePreference(new BrowserStorage(), page);
        theme.Switch();

        theme.SystemChanged(prefersLight: true);

        page.Shown.ShouldBe([ThemeMode.Light]);
    }

    // Whatever else sits under Netto's key, from an older version or a hand edit, is no pick.
    [Theory]
    [InlineData("")]
    [InlineData("Light")]
    [InlineData("blue")]
    [InlineData("system")]
    public void Current_storedValueNotATheme_followsTheSystem(string stored)
    {
        var storage = new BrowserStorage();
        storage.Set(ThemePreference.StorageKey, stored);

        new ThemePreference(storage, new Page(systemPrefersLight: true)).Current.ShouldBe(ThemeMode.Light);
    }

    // A private window or blocked site data: nothing is kept across a reload, but the pick holds while
    // the page is open.
    [Fact]
    public void Switch_storageRefused_pickHoldsUntilTheReload()
    {
        var storage = new RefusingStorage();
        var theme = new ThemePreference(storage, new Page(systemPrefersLight: false));

        theme.Switch();

        (theme.Current, new ThemePreference(storage, new Page(systemPrefersLight: false)).Current)
            .ShouldBe((ThemeMode.Light, ThemeMode.Dark));
    }

    private sealed class Page(bool systemPrefersLight) : IThemePage
    {
        public List<ThemeMode> Shown { get; } = [];

        public bool SystemPrefersLight { get; } = systemPrefersLight;

        public void Show(ThemeMode theme) => Shown.Add(theme);
    }

    private sealed class BrowserStorage : IBrowserStorage
    {
        private readonly Dictionary<string, string> _stored = [];

        public string? Get(string key) => _stored.GetValueOrDefault(key);

        public void Set(string key, string value) => _stored[key] = value;
    }

    private sealed class RefusingStorage : IBrowserStorage
    {
        public string? Get(string key) => null;

        public void Set(string key, string value)
        {
        }
    }
}
