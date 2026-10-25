using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The bar's gain, « €2,551.13 more than Today, your current setup », says what it is measured by in a
// bubble beside it: the sentence of River/ObjectiveNote (ObjectiveNoteTests), the same as the Optimisation
// tab says under its figures (OptimisationObjectiveTests). A bubble and not a sentence in sight: the bar
// sticks under the header on every tab, and a sentence of this length would take two lines of it on a
// desktop and five on a phone; the bubble takes none. It sits outside the live region, which says the
// pocket and the gain and nothing else.
public class BucketBarGainBubbleTests
{
    // The sentence as the one component holds it.
    private static readonly string Sentence = ResxText.For<ObjectiveNote>().Get("Sentence").ShouldNotBeNull();

    // The example is Today; the best pay leaves 2,551.13 more than it (BaselineGainTests).
    private static readonly RiverRun Example = RiverInputs.Example.Run();

    private static readonly RiverRun BestPay = RiverInputs.Example.WithDirectorPay(20_799m).Run();

    // A gain that is a figure: more, less or the same as Today.
    public static TheoryData<string, RiverRun, RiverRun> Gains => new()
    {
        { "more", BestPay, Example },
        { "less", Example, BestPay },
        { "the same", Example, RiverInputs.Example.Run() },
    };

    [Theory]
    [MemberData(nameof(Gains))]
    public async Task BucketBar_gain_hasABubbleBesideItSayingTheObjective(string _, RiverRun run, RiverRun setup)
    {
        var html = await RenderBarAsync(run, setup);

        CardHtml.Bubbles(html).ShouldBe([("the gain over Today", Sentence)]);
    }

    // Right after the gain's own text, on the gain's line, before « Unsaved changes ».
    [Theory]
    [MemberData(nameof(Gains))]
    public async Task BucketBar_gain_hasItsBubbleAfterItsTextOnItsOwnLine(string _, RiverRun run, RiverRun setup)
    {
        var html = await RenderBarAsync(run, setup, unsaved: true);

        var line = Regex.Match(html, "<p class=\"bucket-bar__gain\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value;
        Regex.IsMatch(line, "^\\s*<span class=\"bucket-bar__gain--(more|less|same)\"[^>]*>[^<]+</span>\\s*<span class=\"info-bubble\".*</span>\\s*<span class=\"bucket-bar__unsaved\"[^>]*>Unsaved changes\\.</span>\\s*$",
            RegexOptions.Singleline).ShouldBeTrue();
    }

    // No gain, so nothing to say what it is measured by: the two cannot be compared, there is no Today to
    // measure against, or the line holds « Unsaved changes » alone.
    public static TheoryData<string, RiverRun, RiverRun?, bool> NoGain => new()
    {
        { "not comparable", RiverInputs.Example.SkipCompany().Run(), Example, false },
        { "no Today", Example, null, false },
        { "unsaved changes alone", Example, null, true },
    };

    [Theory]
    [MemberData(nameof(NoGain))]
    public async Task BucketBar_noGainFigure_hasNoBubbleBesideIt(string _, RiverRun run, RiverRun? setup, bool unsaved)
    {
        var html = await RenderBarAsync(run, setup, unsaved);

        CardHtml.Bubbles(html).ShouldBeEmpty();
    }

    // The bar's height never depends on the plan: the sentence is behind its bubble, never a paragraph in
    // sight of the sticky bar.
    [Theory]
    [MemberData(nameof(Gains))]
    public async Task BucketBar_gain_keepsTheSentenceOutOfSight(string _, RiverRun run, RiverRun setup)
    {
        var html = await RenderBarAsync(run, setup);

        CardHtml.Prose(html).Where(text => text.Contains("pension accrual", StringComparison.Ordinal)).ShouldBeEmpty();
    }

    // The live region is the river's only announcement: the pocket and the gain, once the river settles,
    // and nothing else. It is empty as the page opens, and holds no bubble.
    [Theory]
    [MemberData(nameof(Gains))]
    public async Task BucketBar_gain_leavesTheLiveRegionEmptyAndFreeOfTheBubble(string _, RiverRun run, RiverRun setup)
    {
        var html = await RenderBarAsync(run, setup);

        var live = Regex.Match(html, "<p class=\"bucket-bar__live\"[^>]*>.*?</p>", RegexOptions.Singleline).Value;
        Regex.IsMatch(live, "^<p class=\"bucket-bar__live\"[^>]*aria-live=\"polite\"[^>]*>\\s*</p>$").ShouldBeTrue();
    }

    // What the bubble's trigger is named, after « More about ».
    [Fact]
    public void BucketBarResx_gainLabel_namesTheGainOverToday()
    {
        ResxText.For<BucketBar>().Get("GainLabel").ShouldBe("the gain over Today");
    }

    private static async Task<string> RenderBarAsync(RiverRun run, RiverRun? setup, bool unsaved = false) =>
        WebUtility.HtmlDecode(await RenderAsync<BucketBar>(new Folds(new BrowserStorage()), new() { ["Run"] = run, ["Setup"] = setup, ["Unsaved"] = unsaved }));
}
