using System.Net;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The sentence that says what Netto looks for and what it leaves out, which the Optimisation tab draws
// beside its figures and the bar beside its gain (OptimisationObjectiveTests, BucketBarGainBubbleTests):
// one component with one text, so the two places cannot say different things.
public class ObjectiveNoteTests
{
    // The name IStringLocalizer<ObjectiveNote> reads River/ObjectiveNote.resx under.
    private static readonly ResxText Resources = ResxText.For<ObjectiveNote>();

    // The wording the operator chose (2026-10-25): one sentence.
    private const string Sentence = "Netto looks for the most cash in your pocket this year, after the reserve you keep in the company; "
        + "pension accrual and reserved profit are not weighed yet, so check the plan with your accountant before you act on it.";

    [Fact]
    public async Task ObjectiveNote_draws_theSentenceAlone()
    {
        var html = await RenderAsync<ObjectiveNote>(new Folds(new BrowserStorage()), []);

        WebUtility.HtmlDecode(html).Trim().ShouldBe(Sentence);
    }

    // What the sentence has to say, whatever its wording: the objective, what is not weighed yet, and that
    // the accountant checks the plan before it is acted on.
    [Theory]
    [InlineData("most cash in your pocket this year")]
    [InlineData("reserve")]
    [InlineData("pension accrual")]
    [InlineData("reserved profit")]
    [InlineData("not weighed yet")]
    [InlineData("accountant")]
    public void ObjectiveNoteResx_sentence_saysTheObjectiveWhatIsNotWeighedAndTheAccountant(string part)
    {
        Resources.Get("Sentence").ShouldNotBeNull().ShouldContain(part);
    }

    // One sentence, as the item asks: no full stop but its last.
    [Fact]
    public void ObjectiveNoteResx_sentence_isOneSentence()
    {
        var text = Resources.Get("Sentence").ShouldNotBeNull();

        (text.EndsWith('.'), text.Count(character => character == '.')).ShouldBe((true, 1));
    }

    // The tab and the bar draw this component: none of their own resx types the sentence again, or the two
    // places could say different things.
    [Fact]
    public void Resources_everyText_saysTheObjectiveInOneResourceOnly()
    {
        var resources = InterfaceTexts.All()
            .Where(text => text.Value.Contains("pension accrual", StringComparison.OrdinalIgnoreCase))
            .Select(text => text.Resource)
            .ToList();

        resources.ShouldBe(["Netto.Web.Client.River.ObjectiveNote.resources"]);
    }
}
