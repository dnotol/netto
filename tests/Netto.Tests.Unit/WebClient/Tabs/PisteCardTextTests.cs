using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The keys PisteCard.razor builds, held to its .resx, and the words that carry the view's rules: a gain said
// « if yes » before the yes, an idea measured plan against plan, and Keep adding the idea alone.
public class PisteCardTextTests
{
    private static readonly ResxText Resources = ResxText.For<PisteCard>();

    private static readonly ResxText French = ResxText.French<PisteCard>();

    [Fact]
    public void PisteCardResx_everyState_hasAWord()
    {
        Enum.GetValues<PisteState>().Select(PisteCardText.State)
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    // Only a state a trial can show opens it with a line: an idea asking or answered is not tried.
    [Fact]
    public void PisteCardResx_everyStateATrialShows_hasItsLine()
    {
        new[] { PisteState.Searching, PisteState.Priced, PisteState.NoGain, PisteState.Refused }.Select(PisteCardText.Trial)
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    [Fact]
    public void PisteCardResx_everyFixedKey_hasAText()
    {
        new[]
            {
                PisteCardText.Gain(awaitsYes: false), PisteCardText.Gain(awaitsYes: true), PisteCardText.VouchersGranted, PisteCardText.TrialWithout,
                PisteCardText.TrialWith, PisteCardText.TrialGain, PisteCardText.KeepNote, PisteCardText.ToAnswer,
            }
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData(PisteState.Asking)]
    [InlineData(PisteState.Ready)]
    public void Trial_aStateNoTrialShows_isRefused(PisteState state)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PisteCardText.Trial(state));
    }

    // What is still to answer before Try, in both languages.
    [Fact]
    public void ToAnswer_namesWhatIsStillToAnswer()
    {
        (Resources.Format(PisteCardText.ToAnswer, "Share let to the company"), French.Format(PisteCardText.ToAnswer, "Part louée à la société"))
            .ShouldBe(("Still to answer: Share let to the company.", "Reste à répondre\u00a0: Part louée à la société."));
    }

    // A priced idea says how it was measured, as one phrase.
    [Fact]
    public void State_priced_saysHowItWasMeasured()
    {
        (Resources.Get(PisteCardText.State(PisteState.Priced)), French.Get(PisteCardText.State(PisteState.Priced)))
            .ShouldBe(("Measured: optimised plan against optimised plan", "Mesuré\u00a0: plan optimisé contre plan optimisé"));
    }

    // Before the yes, the vouchers' gain is conditional, in both languages.
    [Fact]
    public void Gain_awaitingTheYes_saysIfYes()
    {
        (Resources.Format(PisteCardText.Gain(awaitsYes: true), "+€63.00"), French.Format(PisteCardText.Gain(awaitsYes: true), "+63,00 €"))
            .ShouldBe(("If yes, +€63.00 more in your pocket", "Si oui, +63,00 € de plus dans votre poche"));
    }

    // While an idea is being measured it shows « … », never a figure.
    [Fact]
    public void State_searching_isAnEllipsisAndNoFigure()
    {
        Resources.Get(PisteCardText.State(PisteState.Searching)).ShouldBe("Being measured…");
    }

    // Keep adds the idea alone, and leaves the retunes to Optimise.
    [Fact]
    public void KeepNote_saysKeepAddsTheIdeaAloneAndOptimiseRetunes()
    {
        (Resources.Get(PisteCardText.KeepNote), French.Get(PisteCardText.KeepNote)).ShouldBe((
            "Keep adds this idea alone to your plan; Optimise then retunes the rest.",
            "Garder ajoute cette piste seule à votre plan\u00a0; Optimiser ajuste ensuite le reste."));
    }
}
