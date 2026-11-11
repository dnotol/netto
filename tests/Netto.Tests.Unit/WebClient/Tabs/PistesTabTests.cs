using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Ideas view while nothing finds and prices ideas: its head and one honest sentence, no figure, no
// count, no promise of when.
public class PistesTabTests
{
    [Fact]
    public async Task PistesTab_draws_itsTitleAndSaysNothingIsMeasuredYet()
    {
        var html = await RenderAsync<PistesTab>(new Folds(new BrowserStorage()), []);

        Regex.Matches(html, "<h1[^>]*>([^<]*)</h1>").Select(match => match.Groups[1].Value).ShouldBe(["Ideas"]);
        html.ShouldContain("Nothing is measured here yet.");
    }

    // It says what the view is for, and holds no number and no date: a count of ideas is the view that finds
    // them to supply.
    [Fact]
    public async Task PistesTab_emptyState_holdsNoFigureAndNoPromiseOfWhen()
    {
        var html = await RenderAsync<PistesTab>(new Folds(new BrowserStorage()), []);

        var sentence = Regex.Match(html, "<p class=\"pistes__empty\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value;
        (sentence.Contains("levers you do not use yet"), Regex.IsMatch(sentence, "[0-9€]|soon|later|coming", RegexOptions.IgnoreCase)).ShouldBe((true, false));
    }

    [Fact]
    public async Task PistesTab_inFrench_saysItInFrench()
    {
        using var french = InLanguage.French();

        var html = System.Net.WebUtility.HtmlDecode(await RenderAsync<PistesTab>(new Folds(new BrowserStorage()), []));

        (html.Contains(">Pistes</h1>"), html.Contains("Rien n'est encore chiffré ici.")).ShouldBe((true, true));
    }
}
