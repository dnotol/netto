using System.Text.RegularExpressions;
using Netto.Web.Client.Layout;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// language.js names the page's language on the root, <html lang>, before anything paints: the one the viewer
// chose, kept under netto.language (LanguagePreference), or French while they have chosen none, as the app
// starts in it. The server's <html> says French, and each time Blazor loads a page over the one in the browser
// (after every hot reload in development) it sets the root's attributes to the server's: an English page would
// then be named French, so the language goes back at once. How the page behaves is the browser check's; this
// holds the script and the page that loads it to what they do.
public class LanguageScriptTests
{
    [Fact]
    public void LanguageScript_readsTheViewersChoice_underTheKeyTheAppKeepsIt()
    {
        Script().ShouldContain($"window.localStorage.getItem(\"{LanguagePreference.StorageKey}\")");
    }

    // Only the two tags the app writes are a choice; anything else, or a refused storage, is French.
    [Fact]
    public void LanguageScript_choiceOrFrench_isTheLanguageNamed()
    {
        var script = Script();

        script.ShouldSatisfyAllConditions(
            () => Regex.IsMatch(script, @"===\s*""en""").ShouldBeTrue(),
            () => Regex.IsMatch(script, @"\?\s*""en""\s*:\s*""fr""").ShouldBeTrue(),
            () => Regex.IsMatch(script, @"catch\s*\{\s*return\s+""fr""").ShouldBeTrue(),
            () => Regex.IsMatch(script, @"document\.documentElement\.lang\s*=").ShouldBeTrue());
    }

    [Fact]
    public void LanguageScript_putsTheLanguageBack_whenTheRootsAttributeIsReplaced()
    {
        var script = Script();

        script.ShouldSatisfyAllConditions(
            () => script.ShouldContain("new MutationObserver("),
            () => Regex.IsMatch(script, @"\.observe\(\s*document\.documentElement\s*,\s*\{[^}]*attributeFilter\s*:\s*\[\s*""lang""\s*\]")
                .ShouldBeTrue());
    }

    // The server's page says French, the language a first visit starts in.
    [Fact]
    public void Page_root_isFrench()
    {
        Regex.Match(Page(), "<html[^>]*>").Value.ShouldBe("<html lang=\"fr\">");
    }

    // In the head, before Blazor's script, so the root is named before anything paints and before the app runs.
    [Fact]
    public void Page_loadsTheLanguageScriptInTheHead_beforeBlazor()
    {
        var page = Page();
        var script = page.IndexOf("@Assets[\"language.js\"]", StringComparison.Ordinal);

        (script >= 0, script < page.IndexOf("</head>", StringComparison.Ordinal), script < page.IndexOf("blazor.web.js", StringComparison.Ordinal))
            .ShouldBe((true, true, true));
    }

    private static string Script() => File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web", "wwwroot", "language.js"));

    private static string Page() => File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web", "Components", "App.razor"));
}
