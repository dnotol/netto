using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Ideas view as it draws (RiverRendering, its first draw without a browser): one card per idea not in the
// plan, its h2 the lever's card title, asking what its card asks, then its tuned gain, « … » while it is
// measured; a trial inside the view, Keep only on an idea that gains; the pension one muted line. The ideas
// are measured by the engine (PistesSource over the river's own search): the figures are the engine's own,
// a regression pin (PisteSearchTests holds the derivation of the vouchers' 63.00).
public class PistesTabTests
{
    private static readonly DateOnly On11November = new(2026, 11, 11);

    // The position paper's plan: the example with a royalty following the pay at a quarter of the time.
    private static readonly RiverInputs ExampleWithRoyalty =
        RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.25m).WithRoyaltyFollowingPay();

    [Fact]
    public async Task PistesTab_example_drawsItsTitleTheIntroductionAndOneCardPerIdea()
    {
        var html = await DrawAsync(await MeasuredAsync(RiverInputs.Example));

        Headings(html, 1).ShouldBe(["Ideas"]);
        Headings(html, 2).ShouldBe(["Vouchers", "Home office rent", "Copyright royalty"]);
        html.ShouldContain("Each gain compares your optimised plan with the idea and without it.");
    }

    // Before the Optimiser has searched, nothing is measured: « … », and no figure.
    [Fact]
    public async Task PistesTab_beforeAnySearch_saysBeingMeasuredAndShowsNoGain()
    {
        var source = new PistesSource(() => RiverInputs.Example, _ => null);

        var html = await DrawAsync(source.View(On11November));

        (html.Contains("Being measured…"), html.Contains("more in your pocket")).ShouldBe((true, false));
    }

    // The vouchers' tuned gain is said « if yes », and Try waits for the yes.
    [Fact]
    public async Task PistesTab_vouchersBeforeTheYes_sayIfYesAndCannotBeTried()
    {
        var html = await DrawAsync(await MeasuredAsync(ExampleWithRoyalty));

        var vouchers = Card(html, "Vouchers");
        vouchers.ShouldContain("If yes, +€63.00 more in your pocket");
        vouchers.ShouldContain("The company grants them on the conditions of the Vouchers card");
        vouchers.ShouldContain("Still to answer: The company grants them on the conditions of the Vouchers card.");
        Regex.IsMatch(Button(vouchers, "Try"), "disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task PistesTab_vouchersAfterTheYes_canBeTried()
    {
        var html = await DrawAsync(await MeasuredAsync(ExampleWithRoyalty, source => source.Answer(source.Trial(LeverKind.Vouchers).WithGranted(true))));

        var vouchers = Card(html, "Vouchers");
        (vouchers.Contains("+€63.00 more in your pocket"), vouchers.Contains("If yes"), vouchers.Contains("Still to answer"), Regex.IsMatch(Button(vouchers, "Try"), "disabled"))
            .ShouldBe((true, false, false, false));
    }

    // The rent asks its card's own questions, with the card's own labels, before any gain.
    [Fact]
    public async Task PistesTab_rentAsking_asksTheCardsQuestions()
    {
        var html = await DrawAsync(await MeasuredAsync(RiverInputs.Example));

        var rent = Card(html, "Home office rent");
        rent.ShouldContain("Cadastral income of your home");
        rent.ShouldContain("Share let to the company");
        rent.ShouldContain("id=\"piste-cadastral-income\"");
        rent.ShouldContain("id=\"piste-share-let\"");
        rent.ShouldContain("Gain measured once you answer");
        rent.ShouldContain("Still to answer: Cadastral income of your home, Share let to the company.");
        Regex.IsMatch(Button(rent, "Try"), "disabled").ShouldBeTrue();
    }

    // The royalty asks the creative share and its two statuses, as its card does.
    [Fact]
    public async Task PistesTab_royaltyAsking_asksTheShareAndItsStatuses()
    {
        var html = await DrawAsync(await MeasuredAsync(RiverInputs.Example));

        var royalty = Card(html, "Copyright royalty");
        royalty.ShouldContain("Share of my working time spent creating protected works");
        royalty.ShouldContain("I hold an attestation du travail des arts covering these works");
        royalty.ShouldContain("My copyright income of the four previous years averaged at most");
    }

    // Tried, the trial is inside the view: the optimised plan, with the idea, the gain, the plan's moves, and
    // Keep beside Drop.
    [Fact]
    public async Task PistesTab_vouchersTried_showTheTrialWithKeepAndDrop()
    {
        var html = await DrawAsync(await MeasuredAsync(ExampleWithRoyalty, source =>
        {
            source.Answer(source.Trial(LeverKind.Vouchers).WithGranted(true));
            source.Try(LeverKind.Vouchers);
        }));

        var vouchers = Card(html, "Vouchers");
        vouchers.ShouldSatisfyAllConditions(
            () => vouchers.ShouldContain("Your optimised plan"),
            () => vouchers.ShouldContain("€65,933.99"),
            () => vouchers.ShouldContain("With this idea"),
            () => vouchers.ShouldContain("€65,996.99"),
            () => vouchers.ShouldContain("Grant €250.00 of eco vouchers a year"),
            () => vouchers.ShouldContain("Grant €100.00 of sport and culture vouchers a year"),
            () => Regex.Count(vouchers, "<li class=\"piste__move\"").ShouldBe(4),
            () => vouchers.ShouldContain("Keep adds this idea alone to your plan; Optimise then retunes the rest."),
            () => Button(vouchers, "Keep").ShouldNotBeEmpty(),
            () => Button(vouchers, "Drop").ShouldNotBeEmpty(),
            () => Button(vouchers, "Try").ShouldBeEmpty());
    }

    // An idea that gains nothing offers no Keep.
    [Fact]
    public async Task PistesTab_triedAddingNothing_offersDropButNoKeep()
    {
        var trials = PisteSearch.Levers.Where(lever => lever != LeverKind.Copyright)
            .Select(lever => PisteTrial.Of(ExampleWithRoyalty, lever).WithGranted(true)).ToList();
        var view = PistesView.Of(ExampleWithRoyalty, trials, trial => (new Piste(trial.Lever, Plan(65_000m), Plan(65_000m), []), null), _ => true);

        var vouchers = Card(await DrawAsync(view), "Vouchers");

        (vouchers.Contains("Optimised with this idea, your plan gains nothing."), Button(vouchers, "Keep"), Button(vouchers, "Drop").Length > 0)
            .ShouldBe((true, "", true));
    }

    // A trial the engine refuses says the engine's refusal, and offers no Keep.
    [Fact]
    public async Task PistesTab_triedAndRefused_saysTheRefusalAndOffersNoKeep()
    {
        var html = await DrawAsync(await MeasuredAsync(RiverInputs.Example, source =>
        {
            source.Answer(source.Trial(LeverKind.Rent).WithCadastralIncome(-1m).WithShareLet(0.15m));
            source.Try(LeverKind.Rent);
        }));

        var rent = Card(html, "Home office rent");
        (rent.Contains("refused-message"), Button(rent, "Keep")).ShouldBe((true, ""));
    }

    [Fact]
    public async Task PistesTab_planStartingAtThePay_saysIdeasStartFromTheCompanysYear()
    {
        var html = await DrawAsync(await MeasuredAsync(RiverInputs.Example.SkipCompany()));

        (Headings(html, 2).Count, html.Contains("Ideas start from the company's year")).ShouldBe((0, true));
    }

    [Fact]
    public async Task PistesTab_everyIdeaInThePlan_saysSo()
    {
        var plan = ExampleWithRoyalty.AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Rent);

        var html = await DrawAsync(await MeasuredAsync(plan));

        (Headings(html, 2).Count, html.Contains("Every lever Netto can price as an idea is in your plan already.")).ShouldBe((0, true));
    }

    // The pension is one muted line: named, not in Netto yet, no button, no promise.
    [Fact]
    public async Task PistesTab_pension_isOneMutedLineWithNoButton()
    {
        var html = await DrawAsync(await MeasuredAsync(RiverInputs.Example));

        var line = Regex.Match(html, "<p class=\"pistes__not-yet\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value;
        (line.Contains("Pension: PLCI, EIP, pension savings"), line.Contains("Not in Netto yet."), line.Contains("<button")).ShouldBe((true, true, false));
    }

    [Fact]
    public async Task PistesTab_inFrench_saysItInFrench()
    {
        using var french = InLanguage.French();

        var html = await DrawAsync(await MeasuredAsync(ExampleWithRoyalty));

        (Headings(html, 1).Single(), html.Contains($"Si oui, {Formats.Addition(63m)} de plus dans votre poche"), html.Contains("Pas encore dans Netto."))
            .ShouldBe(("Pistes", true, true));
    }

    // The ideas of that plan as the page measures them, every one searched that can be, after what the
    // viewer did (act).
    private static async Task<PistesView> MeasuredAsync(RiverInputs plan, Action<PistesSource>? act = null)
    {
        var source = new PistesSource(() => plan, date => plan.FindMoves(date));
        act?.Invoke(source);
        while (await source.SearchNextOncePaintedAsync(On11November, () => ValueTask.CompletedTask))
        {
        }

        return source.View(On11November);
    }

    private static async Task<string> DrawAsync(PistesView view) =>
        WebUtility.HtmlDecode(await RenderAsync<PistesTab>(new Folds(new BrowserStorage()), new() { ["View"] = view }));

    private static MovePlan Plan(decimal inPocket) => new(60_000m, [], inPocket);

    private static List<string> Headings(string html, int level) =>
        [.. Regex.Matches(html, $"<h{level}[^>]*>(.*?)</h{level}>", RegexOptions.Singleline).Select(match => Regex.Replace(match.Groups[1].Value, "<[^>]*>", "").Trim())];

    // The card of the idea under that title.
    private static string Card(string html, string title) =>
        Regex.Matches(html, "<section class=\"piste\".*?</section>", RegexOptions.Singleline).Select(match => match.Value)
            .Single(card => Regex.IsMatch(card, $"<h2[^>]*>\\s*{Regex.Escape(title)}"));

    // The opening tag of the card's button that says that word, empty when it has none.
    private static string Button(string card, string word) =>
        Regex.Matches(card, "<button[^>]*>.*?</button>", RegexOptions.Singleline).Select(match => match.Value)
            .FirstOrDefault(button => Regex.Replace(button, "<[^>]*>", "").Trim() == word) is { } found
            ? Regex.Match(found, "<button[^>]*>").Value
            : "";
}
