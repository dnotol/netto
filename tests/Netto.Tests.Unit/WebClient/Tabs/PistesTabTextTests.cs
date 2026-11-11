using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The keys PistesTab.razor builds, held to its .resx, and the words of what Netto does not model: no promise.
public class PistesTabTextTests
{
    private static readonly ResxText Resources = ResxText.For<PistesTab>();

    [Fact]
    public void PistesTabResx_everyKey_hasAText()
    {
        new[] { PistesTabText.Intro, PistesTabText.None(startsAtPay: true), PistesTabText.None(startsAtPay: false), PistesTabText.Kept, PistesTabText.GoToOptimise, PistesTabText.NotInNetto }
            .Concat(Enum.GetValues<NotInNetto>().Select(PistesTabText.NotYet))
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    // The pension is named, said not in Netto yet, and promised for no time.
    [Fact]
    public void NotYet_pension_promisesNothing()
    {
        var words = $"{Resources.Get(PistesTabText.NotYet(NotInNetto.Pension))} {Resources.Get(PistesTabText.NotInNetto)}";

        (words, words.Contains("soon", StringComparison.OrdinalIgnoreCase)).ShouldBe(("Pension: PLCI, EIP, pension savings Not in Netto yet.", false));
    }

    // The introduction says how a gain is measured and that Netto asks, never decides.
    [Fact]
    public void Intro_saysOptimisedAgainstOptimisedAndThatNettoAsks()
    {
        Resources.Get(PistesTabText.Intro).ShouldBe(
            "Each gain compares your optimised plan with the idea and without it. Netto never decides whether you are entitled to it; it asks you.");
    }

    // The French says it as one sentence, a non-breaking space before the semicolon.
    [Fact]
    public void Intro_inFrench_readsAsOneSentence()
    {
        ResxText.French<PistesTab>().Get(PistesTabText.Intro).ShouldBe(
            "Chaque gain compare votre plan optimisé avec la piste et sans elle. Netto ne décide jamais si vous y avez droit\u00a0; il vous le demande.");
    }
}
