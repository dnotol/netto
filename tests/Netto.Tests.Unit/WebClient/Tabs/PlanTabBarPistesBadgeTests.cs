using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The tab bar's badge for Ideas, drawn without a browser (RiverRendering): the seam the Ideas view fills. Short
// after its name, « 1 · 3? », and in full out of sight, so the link's name is the tab's name followed by what
// the badge means.
public class PlanTabBarPistesBadgeTests
{
    [Fact]
    public async Task PlanTabBar_pistesBadge_isShortAfterTheNameAndSpokenInFull()
    {
        var html = await RenderBarAsync(new Dictionary<PlanTab, TabBadge> { [PlanTab.Pistes] = new(TabBadgeKind.Pistes, Count: 1, Questions: 3) });

        var link = Regex.Matches(html, "<a [^>]*class=\"plan-tab-bar__tab\"[^>]*>.*?</a>", RegexOptions.Singleline).Select(match => match.Value).Last();
        (Span(link, "plan-tab-bar__name"), Span(link, "plan-tab-bar__badge"), Span(link, "plan-tab-bar__spoken"))
            .ShouldBe(("Ideas", "1 · 3?", "ideas priced: 1, questions: 3"));
    }

    // While an idea is being measured: « … », which a screen reader skips, and said in full.
    [Fact]
    public async Task PlanTabBar_pistesBeingMeasured_showsAnEllipsisAndSaysSo()
    {
        var html = await RenderBarAsync(new Dictionary<PlanTab, TabBadge> { [PlanTab.Pistes] = new(TabBadgeKind.PistesSearching) });

        var link = Regex.Matches(html, "<a [^>]*class=\"plan-tab-bar__tab\"[^>]*>.*?</a>", RegexOptions.Singleline).Select(match => match.Value).Last();
        (Span(link, "plan-tab-bar__badge"), Span(link, "plan-tab-bar__spoken")).ShouldBe(("…", "measuring ideas"));
    }

    [Fact]
    public async Task PlanTabBar_noPistesBadge_drawsNoneAfterIdeas()
    {
        var html = await RenderBarAsync(new Dictionary<PlanTab, TabBadge>());

        html.ShouldNotContain("plan-tab-bar__badge");
    }

    private static async Task<string> RenderBarAsync(IReadOnlyDictionary<PlanTab, TabBadge> badges) =>
        WebUtility.HtmlDecode(await RenderAsync<PlanTabBar>(new Folds(new BrowserStorage()), new() { ["Current"] = PlanTab.Plan, ["Badges"] = badges }));

    private static string? Span(string link, string @class)
    {
        var found = Regex.Match(link, $"<span [^>]*class=\"{@class}[ \"][^>]*>(.*?)</span>", RegexOptions.Singleline);
        return found.Success ? Regex.Replace(found.Groups[1].Value, "<[^>]*>", "").Trim() : null;
    }
}
