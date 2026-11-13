using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The band's gain, what the river leaves in the pocket against the plan starred Today, sits on the pocket's
// name line, short (« +€2,551.13 vs Today »), so it takes no line of its own, and says what it is measured by
// in a bubble beside it: the sentence of River/ObjectiveNote (ObjectiveNoteTests), the measure the Optimisation
// tab says, shorter, in its head's state line (OptimisationObjectiveTests). It sits outside the live region, which says the
// pocket and the gain in full and nothing else.
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
        { "same", Example, RiverInputs.Example.Run() },
    };

    // The same, as the pocket's line shows it.
    public static TheoryData<string, RiverRun, RiverRun, string> GainsShown => new()
    {
        { "more", BestPay, Example, "+€2,551.13 vs Today" },
        { "less", Example, BestPay, "−€2,551.13 vs Today" },
        { "same", Example, RiverInputs.Example.Run(), "Same as Today" },
    };

    [Theory]
    [MemberData(nameof(Gains))]
    public async Task BucketBar_gain_hasABubbleBesideItSayingTheObjective(string _, RiverRun run, RiverRun setup)
    {
        var html = await RenderBarAsync(run, setup);

        CardHtml.Bubbles(html).ShouldContain(("the gain over Today", Sentence));
    }

    // On the pocket's name line, after its name, short, then its bubble.
    [Theory]
    [MemberData(nameof(GainsShown))]
    public async Task BucketBar_gain_isOnThePocketsNameLine_shortThenItsBubble(string kind, RiverRun run, RiverRun setup, string shown)
    {
        var html = await RenderBarAsync(run, setup);

        var label = Regex.Match(html, "<div class=\"bucket bucket--pocket\".*?<dt[^>]*>(.*?)</dt>", RegexOptions.Singleline).Groups[1].Value;
        Regex.IsMatch(label, $"<span class=\"bucket__gain bucket__gain--{kind}\"[^>]*>{Regex.Escape(shown)}</span>\\s*<span class=\"info-bubble\".*More about the gain over Today",
            RegexOptions.Singleline).ShouldBeTrue();
    }

    // Two plans that start from different places cannot be compared: the line says so, short, and its bubble
    // says why.
    [Fact]
    public async Task BucketBar_notComparable_saysWhyInItsBubble()
    {
        var html = await RenderBarAsync(RiverInputs.Example.SkipCompany().Run(), Example);

        var label = Regex.Match(html, "<div class=\"bucket bucket--pocket\".*?<dt[^>]*>(.*?)</dt>", RegexOptions.Singleline).Groups[1].Value;
        (label.Contains(">Not comparable with Today</span>", StringComparison.Ordinal), CardHtml.Bubbles(html).Contains(
                ("the gain over Today", "Not comparable with Today, your current setup: one starts from the company's year, the other at the pay.")))
            .ShouldBe((true, true));
    }

    // No Today to measure against: no gain and nothing to say what it is measured by.
    [Fact]
    public async Task BucketBar_noToday_hasNoGainNorItsBubble()
    {
        var html = await RenderBarAsync(Example, null);

        (html.Contains("bucket__gain", StringComparison.Ordinal), CardHtml.Bubbles(html).Any(bubble => bubble.Label == "the gain over Today"))
            .ShouldBe((false, false));
    }

    // The sentence is behind its bubble, never a paragraph in sight of the sticky band.
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

    private static async Task<string> RenderBarAsync(RiverRun run, RiverRun? setup) =>
        WebUtility.HtmlDecode(await RenderAsync<BucketBar>(new Folds(new BrowserStorage()), new() { ["Run"] = run, ["Setup"] = setup }));
}
