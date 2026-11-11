using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// How My plan is read, « by bucket · by date »: two links, each with its own address, drawn without a
// browser (RiverRendering). That the focus stays on the link pressed, since the nav is not redrawn from
// scratch, is PlanLensTests', on the page as the browser runs it.
public class PlanLensBarTests
{
    [Fact]
    public async Task PlanLensBar_draws_aNamedNavOfTheTwoLensesInOrder()
    {
        var html = await RenderBarAsync(PlanLens.ByBucket);

        Regex.IsMatch(html, "<nav [^>]*aria-label=\"How your plan is read\"").ShouldBeTrue();
        Links(html).Select(link => (link.Href, link.Name)).ShouldBe([("plan", "By bucket"), ("plan?by=date", "By date")]);
    }

    [Theory]
    [InlineData(PlanLens.ByBucket, "By bucket")]
    [InlineData(PlanLens.ByDate, "By date")]
    public async Task PlanLensBar_aLens_isTheOnlyOneCurrent(PlanLens lens, string name)
    {
        var html = await RenderBarAsync(lens);

        Links(html).Where(link => link.Current).Select(link => link.Name).ShouldBe([name]);
    }

    // A lens is a state of the tab, not the page: the tab's own current mark is « page ».
    [Fact]
    public async Task PlanLensBar_current_isMarkedTrueNotPage()
    {
        var html = await RenderBarAsync(PlanLens.ByDate);

        (html.Contains("aria-current=\"true\""), html.Contains("aria-current=\"page\"")).ShouldBe((true, false));
    }

    [Fact]
    public async Task PlanLensBar_inFrench_isNamedForTheOperator()
    {
        using var french = InLanguage.French();

        var html = await RenderBarAsync(PlanLens.ByBucket);

        Links(html).Select(link => link.Name).ShouldBe(["Par compartiment", "Par date"]);
    }

    [Fact]
    public void PlanLensBarResx_everyLens_hasItsNameInBothLanguages()
    {
        var (english, french) = (ResxText.For<PlanLensBar>(), ResxText.French<PlanLensBar>());

        Enum.GetValues<PlanLens>().Select(lens => (english.Get(PlanLensBarText.Name(lens)), french.Get(PlanLensBarText.Name(lens))))
            .ShouldBe([("By bucket", "Par compartiment"), ("By date", "Par date")]);
    }

    private static Task<string> RenderBarAsync(PlanLens current) =>
        RenderAsync<PlanLensBar>(new Folds(new BrowserStorage()), new() { ["Current"] = current });

    private static List<(string Href, string Name, bool Current)> Links(string html) =>
    [
        .. Regex.Matches(html, "<a [^>]*class=\"plan-lens__link\"[^>]*>(.*?)</a>", RegexOptions.Singleline).Select(match => (
            Regex.Match(match.Value, "href=\"([^\"]*)\"").Groups[1].Value,
            Regex.Replace(match.Groups[1].Value, "<[^>]*>", "").Trim(),
            match.Value.Contains("aria-current=\"true\""))),
    ];
}
