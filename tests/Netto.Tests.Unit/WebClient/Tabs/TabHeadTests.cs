using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The head every tab of the plan page opens on, drawn without a browser (RiverRendering): an eyebrow
// with the planner's « In development » chip, the tab's title and a lede. Where the focus goes on
// arriving is the browser's to check: a static render runs nothing after drawing.
public class TabHeadTests
{
    [Fact]
    public async Task TabHead_drawsTheEyebrowTheChipTheTitleAndTheLede()
    {
        var html = await RenderHeadAsync();

        (html.Contains(">Against Today<"), html.Contains("In development"), html.Contains(">Optimisation</h1>"), html.Contains(">What Netto would change<"))
            .ShouldBe((true, true, true, true));
    }

    // The tab's title is the page's one h1, which the focus can be put on without making it a stop of
    // the keyboard.
    [Fact]
    public async Task TabHead_title_isTheOneH1AndTakesTheFocusOnlyByScript()
    {
        var html = await RenderHeadAsync();

        (Regex.Count(html, "<h1[ >]"), Regex.IsMatch(html, "<h1 [^>]*tabindex=\"-1\"[^>]*>Optimisation</h1>")).ShouldBe((1, true));
    }

    private static Task<string> RenderHeadAsync() =>
        RenderAsync<TabHead>(new Folds(new BrowserStorage()), new()
        {
            ["Eyebrow"] = "Against Today", ["Title"] = "Optimisation", ["Lede"] = "What Netto would change",
        });
}
