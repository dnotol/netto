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
            .ShouldBe(("Ideas", "1 · 3?", "1 priced, 3 questions"));
    }

    // Only what there is: no question open, the priced count alone; nothing priced, the questions alone.
    [Theory]
    [InlineData(1, 0, "1", "1 priced")]
    [InlineData(2, 0, "2", "2 priced")]
    [InlineData(0, 2, "2?", "2 questions")]
    [InlineData(0, 1, "1?", "1 question")]
    public async Task PlanTabBar_pistesBadge_saysOnlyTheCountsThatAreNotZero(int priced, int questions, string shown, string spoken)
    {
        var html = await RenderBarAsync(new Dictionary<PlanTab, TabBadge> { [PlanTab.Pistes] = new(TabBadgeKind.Pistes, Count: priced, Questions: questions) });

        var link = Regex.Matches(html, "<a [^>]*class=\"plan-tab-bar__tab\"[^>]*>.*?</a>", RegexOptions.Singleline).Select(match => match.Value).Last();
        (Span(link, "plan-tab-bar__badge"), Span(link, "plan-tab-bar__spoken")).ShouldBe((shown, spoken));
    }

    // In French, each count agrees with its number.
    [Fact]
    public async Task PlanTabBar_pistesBadgeInFrench_agreesWithEachCount()
    {
        using var french = InLanguage.French();

        var html = await RenderBarAsync(new Dictionary<PlanTab, TabBadge> { [PlanTab.Pistes] = new(TabBadgeKind.Pistes, Count: 2, Questions: 1) });

        var link = Regex.Matches(html, "<a [^>]*class=\"plan-tab-bar__tab\"[^>]*>.*?</a>", RegexOptions.Singleline).Select(match => match.Value).Last();
        Span(link, "plan-tab-bar__spoken").ShouldBe("2 chiffrées, 1 question");
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
