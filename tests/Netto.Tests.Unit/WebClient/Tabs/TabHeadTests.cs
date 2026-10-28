using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The head every tab of the plan page opens on, drawn without a browser (RiverRendering): the tab's name
// as a small title and nothing else, no eyebrow, no « In development » chip, no introduction. Where the
// focus goes on arriving is the browser's to check: a static render runs nothing after drawing.
public class TabHeadTests
{
    [Fact]
    public async Task TabHead_draws_theTitleAlone()
    {
        var html = await RenderHeadAsync();

        (html.Contains(">Optimisation</h1>"), html.Contains("tab-head__eyebrow"), html.Contains("tab-head__lede"), html.Contains("mud-chip"),
                html.Contains("In development", StringComparison.OrdinalIgnoreCase))
            .ShouldBe((true, false, false, false, false));
    }

    // The tab's title is the page's one h1, which the focus can be put on without making it a stop of
    // the keyboard.
    [Fact]
    public async Task TabHead_title_isTheOneH1AndTakesTheFocusOnlyByScript()
    {
        var html = await RenderHeadAsync();

        (Regex.Count(html, "<h1[ >]"), Regex.IsMatch(html, "<h1 [^>]*tabindex=\"-1\"[^>]*>Optimisation</h1>")).ShouldBe((1, true));
    }

    // No tab words an eyebrow or an introduction any more: a text of one would be drawn by nobody.
    [Theory]
    [InlineData(typeof(MapTab))]
    [InlineData(typeof(OptimisationTab))]
    [InlineData(typeof(WhenToPayTab))]
    [InlineData(typeof(CompareTab))]
    public void TabResx_everyTab_holdsNoEyebrowNorLede(Type tab)
    {
        var texts = ResxText.For(tab);

        (texts.Get("Eyebrow"), texts.Get("Lede"), string.IsNullOrWhiteSpace(texts.Get("Title"))).ShouldBe((null, null, false));
    }

    // A compact title: a size in rem a tab's name does not need more than, and no rule left for the eyebrow,
    // the chip or the lede.
    [Fact]
    public void TabHeadCss_title_isCompactAndTheEyebrowAndTheLedeHaveNoRule()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "TabHead.razor.css");

        var size = Regex.Match(Stylesheet.Block(css, ".tab-head__title"), @"font-size:\s*([0-9.]+)rem");
        (size.Success && decimal.Parse(size.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) <= 1.75m,
                css.Contains("eyebrow"), css.Contains("lede"), css.Contains("mud-chip"))
            .ShouldBe((true, false, false, false));
    }

    private static Task<string> RenderHeadAsync() =>
        RenderAsync<TabHead>(new Folds(new BrowserStorage()), new() { ["Title"] = "Optimisation" });
}
