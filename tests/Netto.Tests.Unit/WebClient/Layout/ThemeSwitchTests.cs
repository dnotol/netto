using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client.Layout;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Layout;

// The theme switch, one component in the site's header and in the plan page's band: named by what it does,
// so its name changes with the theme rather than a pressed state, the theme shown being the viewer's pick or,
// while there is none, their system's (ThemePreference). What a click does is ThemePreferenceTests'.
public class ThemeSwitchTests
{
    [Theory]
    [InlineData(false, null, "Switch to light theme")]
    [InlineData(true, null, "Switch to dark theme")]
    [InlineData(true, "dark", "Switch to light theme")]
    [InlineData(false, "light", "Switch to dark theme")]
    public async Task ThemeSwitch_theThemeShown_namesTheOther(bool systemPrefersLight, string? picked, string name)
    {
        var storage = new BrowserStorage();
        if (picked is not null)
            storage.Set(ThemePreference.StorageKey, picked);

        var html = await RenderAsync(typeof(ThemeSwitch), new Folds(new BrowserStorage()), [],
            services => services.AddSingleton(new ThemePreference(storage, new SystemTheme(systemPrefersLight))));

        Regex.Matches(html, "<button[^>]*aria-label=\"([^\"]*)\"").Select(match => match.Groups[1].Value).ShouldBe([name]);
    }
}
