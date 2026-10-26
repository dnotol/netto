using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// theme.js sets the page's theme on the root as data-theme, which app.css draws by, while MudBlazor draws its
// own palette from ThemePreference. The server's <html> carries no theme, and each time Blazor loads a page over
// the one in the browser (after every hot reload in development) it sets the root's attributes to the server's:
// the page then fell back to dark under MudBlazor's light palette, a field's text dark on a dark card. How the
// page behaves is the browser's check; this holds the script to watching the attribute it sets.
public class ThemeScriptTests
{
    [Fact]
    public void ThemeScript_putsTheThemeBack_whenTheRootsAttributeIsReplaced()
    {
        var script = File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web", "wwwroot", "theme.js"));

        script.ShouldSatisfyAllConditions(
            () => script.ShouldContain("new MutationObserver("),
            () => Regex.IsMatch(script, @"\.observe\(\s*document\.documentElement\s*,\s*\{[^}]*attributeFilter\s*:\s*\[\s*""data-theme""\s*\]")
                .ShouldBeTrue());
    }
}
