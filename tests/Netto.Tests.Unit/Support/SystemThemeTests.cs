using Netto.Web.Client.Layout;
using Shouldly;

namespace Netto.Tests.Support;

public class SystemThemeTests
{
    [Fact]
    public void ChangeSystem_tellsEveryWatcherAndIsWhatItPrefersFromThen()
    {
        var system = new SystemTheme(prefersLight: false);
        var told = new List<bool>();
        system.WatchSystem(told.Add);
        system.WatchSystem(told.Add);

        system.ChangeSystem(prefersLight: true);

        (system.SystemPrefersLight, told.Count(prefersLight => prefersLight)).ShouldBe((true, 2));
    }

    [Fact]
    public void Show_keepsTheThemesShownInOrder()
    {
        var system = new SystemTheme(prefersLight: true);

        system.Show(ThemeMode.Dark);
        system.Show(ThemeMode.Light);

        system.Shown.ShouldBe([ThemeMode.Dark, ThemeMode.Light]);
    }
}
