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

    // The system's setting changes while the page is open: the page tells the preference, which watches it
    // from the start.
    [Fact]
    public void SystemChange_nothingPicked_isFollowed()
    {
        var page = new Page(systemPrefersLight: false);
        var theme = new ThemePreference(new BrowserStorage(), page);

        page.ChangeSystem(prefersLight: true);

        theme.Current.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public void SystemChange_afterAPick_isIgnored()
    {
        var page = new Page(systemPrefersLight: false);
        var theme = new ThemePreference(new BrowserStorage(), page);
        theme.Switch();

        page.ChangeSystem(prefersLight: false);

        theme.Current.ShouldBe(ThemeMode.Light);
    }

    // What draws in the theme from outside the page's root, MudBlazor's palette in the layout, hears of each
    // change, whoever made it.
    [Fact]
    public void SystemChange_nothingPicked_tellsListeners()
    {
        var page = new Page(systemPrefersLight: false);
        var theme = new ThemePreference(new BrowserStorage(), page);
        var told = 0;
        theme.Changed += () => told++;

        page.ChangeSystem(prefersLight: true);

        told.ShouldBe(1);
    }

    [Fact]
    public void SystemChange_afterAPick_tellsNoOne()
    {
        var page = new Page(systemPrefersLight: false);
        var theme = new ThemePreference(new BrowserStorage(), page);
        theme.Switch();
        var told = 0;
        theme.Changed += () => told++;

        page.ChangeSystem(prefersLight: true);

        told.ShouldBe(0);
    }

    [Fact]
    public void Switch_tellsListeners()
    {
        var theme = new ThemePreference(new BrowserStorage(), new Page(systemPrefersLight: false));
        var told = 0;
        theme.Changed += () => told++;

        theme.Switch();

        told.ShouldBe(1);
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
    public void SystemChange_nothingPicked_showsTheSystemsTheme()
    {
        var page = new Page(systemPrefersLight: false);
        _ = new ThemePreference(new BrowserStorage(), page);

        page.ChangeSystem(prefersLight: true);

        page.Shown.ShouldBe([ThemeMode.Light]);
    }

    [Fact]
    public void SystemChange_afterAPick_showsNothing()
    {
        var page = new Page(systemPrefersLight: false);
        var theme = new ThemePreference(new BrowserStorage(), page);
        theme.Switch();

        page.ChangeSystem(prefersLight: true);

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

    // The system's setting starts as given and changes when the test says so, as the browser's would.
    private sealed class Page(bool systemPrefersLight) : IThemePage
    {
        private readonly List<Action<bool>> _watchers = [];

        public List<ThemeMode> Shown { get; } = [];

        public bool SystemPrefersLight { get; private set; } = systemPrefersLight;

        public void Show(ThemeMode theme) => Shown.Add(theme);

        public void WatchSystem(Action<bool> changed) => _watchers.Add(changed);

        public void ChangeSystem(bool prefersLight)
        {
            SystemPrefersLight = prefersLight;
            foreach (var watcher in _watchers)
                watcher(prefersLight);
        }
    }
}
