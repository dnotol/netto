using Shouldly;

namespace Netto.Tests.WebClient;

// front-page.js puts the front page at a section when the page is opened at one. MudBlazor's own scroll
// centres the element in the window (block: "center"), which left the section 415 px down instead of under the
// header; scrolling to the start of the section honours the page's scroll-padding-top, and an instant scroll is
// not cut short as the page settles. How the browser behaves is the browser check's; this holds the script to the
// names the app calls and the way it scrolls, and the page to loading it.
public class FrontPageScriptTests
{
    private static string Script => File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web", "wwwroot", "front-page.js"));

    [Fact]
    public void Script_scrollTo_namesTheFunctionTheFrontPageCalls()
    {
        Script.ShouldContain("window.nettoFrontPage");
        Script.ShouldContain("scrollTo(id)");
    }

    [Fact]
    public void Script_scrollTo_putsTheSectionAtTheStartOfTheWindowAtOnce()
    {
        Script.ShouldContain("block: \"start\"");
        Script.ShouldContain("behavior: \"instant\"");
        Script.ShouldNotContain("block: \"center\"");
    }

    [Fact]
    public void App_loadsTheScript()
    {
        var app = File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web", "Components", "App.razor"));

        app.ShouldContain("@Assets[\"front-page.js\"]");
    }
}
