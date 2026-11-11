using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The one line in My plan that points at the other two views, as counts only, drawn without a browser
// (RiverRendering): what Optimise can add to the plan on screen, and how many ideas are priced and how many
// questions wait. It suggests nothing and draws nothing when it has nothing to say.
public class PlanNudgesTests
{
    [Fact]
    public async Task PlanNudges_aGain_saysWhatOptimiseCanAddAndLinksIt()
    {
        var html = await RenderAsync(gain: 3_309.67m);

        Nudges(html).ShouldBe([("plan/optimisation", "Optimise can add +€3,309.67 without changing anything else")]);
    }

    [Fact]
    public async Task PlanNudges_pistes_saysHowManyArePricedAndHowManyQuestionsWait()
    {
        var html = await RenderAsync(pistes: new PistesCount(1, 3));

        Nudges(html).ShouldBe([("plan/pistes", "Ideas: priced 1, questions 3")]);
    }

    [Fact]
    public async Task PlanNudges_both_areOneLineOfTwoLinksInTheViewsOrder()
    {
        var html = await RenderAsync(gain: 100m, pistes: new PistesCount(0, 2));

        Regex.Count(html, "<ul ").ShouldBe(1);
        Nudges(html).Select(nudge => nudge.Href).ShouldBe(["plan/optimisation", "plan/pistes"]);
    }

    // Nothing to say, nothing drawn: no gain, no ideas supplied.
    [Fact]
    public async Task PlanNudges_nothingToSay_drawsNothing()
    {
        (await RenderAsync()).Trim().ShouldBeEmpty();
    }

    // A gain of nothing is not a nudge, nor is a count of nothing.
    [Fact]
    public async Task PlanNudges_aGainOfNothingAndNoIdeas_drawsNothing()
    {
        (await RenderAsync(gain: 0m, pistes: new PistesCount(0, 0))).Trim().ShouldBeEmpty();
    }

    // The line is made of links, said by no live region: the bar's is the river's one announcement.
    [Fact]
    public async Task PlanNudges_line_isNoLiveRegion()
    {
        var html = await RenderAsync(gain: 100m, pistes: new PistesCount(1, 1));

        Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"").ShouldBeFalse();
    }

    [Fact]
    public async Task PlanNudges_inFrench_isWordedForTheOperator()
    {
        using var french = InLanguage.French();

        var html = await RenderAsync(gain: 3_309.67m, pistes: new PistesCount(1, 3));

        Nudges(html).Select(nudge => nudge.Text).ShouldBe([
            "Optimiser peut ajouter +3.309,67 € sans rien changer d'autre",
            "Pistes : chiffrées : 1, questions : 3",
        ]);
    }

    private static Task<string> RenderAsync(decimal? gain = null, PistesCount? pistes = null) =>
        RenderAsync<PlanNudges>(new Folds(new BrowserStorage()), new() { ["Gain"] = gain, ["Pistes"] = pistes });

    private static List<(string Href, string Text)> Nudges(string html) =>
    [
        .. Regex.Matches(html, "<a [^>]*class=\"plan-nudges__link\"[^>]*>(.*?)</a>", RegexOptions.Singleline).Select(match => (
            Regex.Match(match.Value, "href=\"([^\"]*)\"").Groups[1].Value,
            WebUtility.HtmlDecode(match.Groups[1].Value).Trim())),
    ];
}
