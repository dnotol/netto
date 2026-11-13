using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.CompanyCar;
using Netto.Core.CorporateTax;
using Netto.Core.SocialContributions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The pay curve of the Optimisation tab as a viewer reads it, rendered on its own the way its tab hands it the
// plan on screen and the page's search (RiverRendering: the first draw, no click). It draws the curve of the
// pocket at every pay under « Tune your pay », its key, the best pay's figures and the button that sets the pay
// there, and nothing while there is no search to draw.
public class PayCurveTests
{
    private static readonly ResxText Texts = ResxText.For<PayCurve>();

    private static readonly RiverInputs Fixed = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(20_000m);

    // The example pays 50,000 and its best pay is 20,799 (SplitSearchViewTests): the button sets the pay there.
    [Fact]
    public async Task Curve_paySetAwayFromTheBest_drawsTheCurveItsKeyAndTheButtonToTheBestPay()
    {
        var html = await CurveAsync(RiverInputs.Example);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain(Texts.Format("Title")),
            () => html.ShouldContain("class=\"pay-curve__plot\""),
            () => html.ShouldContain($"</span>{Texts.Format("KeyYourPay")}</li>"),
            () => html.ShouldContain($"</span>{Texts.Format("KeyBest")}</li>"),
            () => CardHtml.Lines(html).Select(line => line.Label).ShouldContain(Texts.Format("Figure_Best")),
            () => html.ShouldContain("tune__move-to-best"),
            () => WebUtility.HtmlDecode(html).ShouldContain(Texts.Format("MoveToBestLabel", "€20,799.00")));
    }

    // At the best pay there is nothing to move to: the figures say it is the best, with no button.
    [Fact]
    public async Task Curve_paySetAtTheBest_drawsNoButtonAndSaysItIsTheBest()
    {
        var best = RiverInputs.Example.SearchSplit().ShouldNotBeNull().Best.ShouldNotBeNull().Pay;

        var html = await CurveAsync(RiverInputs.Example.WithDirectorPay(best));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldNotContain("tune__move-to-best"),
            () => CardHtml.Lines(html).Select(line => line.Label).ShouldContain(Texts.Format("Figure_BestIsYours")));
    }

    // There is no slider here: the plan's pay is typed on the Director pay card.
    [Fact]
    public async Task Curve_drawn_hasNoSliderNorField()
    {
        var html = await CurveAsync(RiverInputs.Example);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldNotContain("mud-slider"),
            () => html.ShouldNotContain("<input"));
    }

    // With the company's year skipped, or no pay to search, the page's search is none: no curve, but one line
    // saying what it waits for, which can take the focus from code alone, so the Pay sub-tab is never blank.
    [Fact]
    public async Task Curve_companySkipped_saysWhatTheCurveWaitsFor()
    {
        var html = WebUtility.HtmlDecode(await CurveAsync(RiverInputs.Example.SkipCompany()));

        html.Trim().ShouldMatch("^<p class=\"tune__unavailable\" tabindex=\"-1\"[^>]*>Netto draws this curve once your plan holds its company's year, with a profit to share and no amount refused\\.</p>$");
    }

    [Fact]
    public async Task Curve_noSearch_saysWhatTheCurveWaitsForAlone()
    {
        var html = await CurveAsync(RiverInputs.Example, search: false);

        (html.Contains("tune__unavailable"), html.Contains("<section")).ShouldBe((true, false));
    }

    // The curve's heading starts the Pay sub-tab: it can take the focus from code alone.
    [Fact]
    public async Task Curve_heading_takesTheFocusOnlyByScript()
    {
        var html = await CurveAsync(RiverInputs.Example);

        (Regex.IsMatch(html, "<h2 id=\"tune-title\" class=\"tune__title\" tabindex=\"-1\""), html.Contains("tune__unavailable")).ShouldBe((true, false));
    }

    // No pay keeps the reserve: the section says how much the company can keep, and draws no button.
    [Fact]
    public async Task Curve_noPayKeepsTheReserve_saysSoWithTheMostTheCompanyKeeps()
    {
        var html = await CurveAsync(RiverInputs.Example.WithReserve(200_000m));

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Prose(html).ShouldContain(prose => prose.StartsWith("No pay keeps €200,000.00", StringComparison.Ordinal)),
            () => html.ShouldNotContain("tune__move-to-best"));
    }

    // A small company in its second tax period needs no minimum pay, but a company car's benefit of 1,714.29
    // must stay within 20% of the pay with it: the curve weighs the rate, and the bubble of the reduced
    // corporate rate says why, with the share its rule holds.
    [Fact]
    public async Task Curve_carInSecondPeriod_termBubbleNamesTheBenefitsTest()
    {
        var river = RiverInputs.Example.WithFinancialYear(FinancialYear.Second).AddToPlan(OptionalModule.Car)
            .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m).WithCarEngine(CarEngine.Electric);

        var html = WebUtility.HtmlDecode(await CurveAsync(river));

        var meaning = Texts.Format(ReducedRateText.Term, "20%");

        meaning.ShouldSatisfyAllConditions(
            () => meaning.ShouldContain("20%"),
            () => html.ShouldContain(meaning));
    }

    // The curve's key says the pay the dashed line starts at wherever the search found one, with a royalty in the
    // plan too: no card of Optimise says it, so the key is where it is read.
    [Fact]
    public async Task Curve_aRoyaltyLoweringThePay_keysTheThresholdWithItsPay()
    {
        var threshold = Fixed.SearchSplit().ShouldNotBeNull().SmallestPayKeepingReducedRate.ShouldNotBeNull();

        var html = WebUtility.HtmlDecode(await CurveAsync(Fixed));

        html.ShouldContain(Texts.Format("KeyThreshold", Formats.Euros(threshold)));
    }

    [Fact]
    public async Task Curve_noRoyaltyInThePlan_keysTheThresholdWithItsPay()
    {
        var threshold = RiverInputs.Example.SearchSplit().ShouldNotBeNull().SmallestPayKeepingReducedRate.ShouldNotBeNull();

        var html = WebUtility.HtmlDecode(await CurveAsync(RiverInputs.Example));

        html.ShouldContain(Texts.Format("KeyThreshold", Formats.Euros(threshold)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Curve_anyPlan_leaksNoKey(bool royalty)
    {
        var html = await CurveAsync(royalty ? Fixed : RiverInputs.Example);

        CardHtml.LeakedKeys(html).ShouldBeEmpty();
    }

    // The best pay the search found and what it leaves, on the filled plan, the amount and the share of each line,
    // in order: the best pay, its dividend, the royalty beside it, what reaches the pocket there and what it gains
    // over the pay set, then the pocket at the pay set, out of what leaves the company.
    [Fact]
    public async Task Curve_filledPlan_drawsTheBestPayAsLines()
    {
        CardHtml.Lines(await CurveAsync(StreamCardsFiguresTests.Filled)).Select(line => (line.Amount, line.Share)).ShouldBe(
        [
            ("€19,757.00", null),
            ("€46,937.35", null),
            ("€2,822.42", null),
            ("€60,866.97", "66.2%"),
            ("€2,619.44", null),
            ("€58,247.53", "63.3%"),
        ]);
    }

    // How the best pay stands against the reduced rate is a sentence, with what keeping the rate would
    // leave less, no line saying it.
    [Fact]
    public async Task Curve_filledPlan_weighsTheReducedRateInASentence()
    {
        var html = await CurveAsync(StreamCardsFiguresTests.Filled);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Prose(html).ShouldContain(paragraph => paragraph.Contains("€1,016.20", StringComparison.Ordinal)),
            () => CardHtml.LineAmounts(html).ShouldNotContain("€1,016.20"));
    }

    // The river's best pay, whatever the facts the river holds now, is offered by a button of its own while the
    // pay set is not it: not at the best, where no pay keeps the reserve, or while the pay is refused and there
    // is no search to read it from, the section then drawing nothing.
    public static TheoryData<string, RiverInputs, int> BestPayButtons => new()
    {
        { "the pay set away from the best", RiverInputs.Example, 1 },
        { "the pay set at the best", RiverInputs.Example.WithDirectorPay(BestPay(RiverInputs.Example)), 0 },
        { "no pay keeping the reserve", RiverInputs.Example.WithReserve(80_000m), 0 },
        { "the pay refused", RiverInputs.Example.WithDirectorPay(-1m), 0 },
    };

    [Theory]
    [MemberData(nameof(BestPayButtons))]
    public async Task Curve_open_offersTheBestPayOnlyWhileThePaySetIsNotIt(string _, RiverInputs plan, int buttons)
    {
        var html = WebUtility.HtmlDecode(await CurveAsync(plan));

        Regex.Count(html, $"<button\\b[^>]*>(?:(?!</button>).)*?{Regex.Escape(Texts.Get("MoveToBest").ShouldNotBeNull())}", RegexOptions.Singleline).ShouldBe(buttons);
    }

    // The button says which pay it sets for a screen reader, its visible words a part of its name.
    [Fact]
    public async Task Curve_open_namesTheBestPayInTheButtonsLabel()
    {
        var html = WebUtility.HtmlDecode(await CurveAsync(RiverInputs.Example));
        var label = Texts.Format("MoveToBestLabel", Formats.Euros(BestPay(RiverInputs.Example)));

        html.ShouldContain($"aria-label=\"{label}\"");
        label.ShouldStartWith(Texts.Get("MoveToBest").ShouldNotBeNull());
    }

    // Until the pointer is on the curve, its line keeps its room, empty.
    [Fact]
    public async Task Curve_firstDraw_keepsThePointersLineEmpty()
    {
        Regex.IsMatch(await CurveAsync(RiverInputs.Example), @"<p class=""tune__pointer""[^>]*>\s*</p>").ShouldBeTrue();
    }

    // The curve is the plan on screen's own, not the plan with the moves applied: its heading says so, and
    // its note says why its best pay can differ from a move's pay, in English and French.
    [Fact]
    public async Task Curve_heading_saysItIsForThePlanOnScreen()
    {
        var english = WebUtility.HtmlDecode(await CurveAsync(RiverInputs.Example));
        using var french = InLanguage.French();
        var frenchHtml = WebUtility.HtmlDecode(await CurveAsync(RiverInputs.Example));

        (Regex.Match(english, "<h2[^>]*>([^<]*)</h2>").Groups[1].Value, english.Contains("not with the moves applied"),
                Regex.Match(frenchHtml, "<h2[^>]*>([^<]*)</h2>").Groups[1].Value, frenchHtml.Contains("pas avec les ajustements appliqués"),
                english.Contains("above"), frenchHtml.Contains("ci-dessus"))
            .ShouldBe(("Tune your pay for the plan on screen", true, "Ajuster votre rémunération pour le plan affiché", true, false, false));
    }

    // The best pay the search finds for the river as it stands.
    private static decimal BestPay(RiverInputs river) => river.SearchSplit().ShouldNotBeNull().Best.ShouldNotBeNull().Pay;

    // The component on the plan on screen, with the page's search for it, or none.
    internal static async Task<string> CurveAsync(RiverInputs river, bool search = true) =>
        await RiverRendering.RenderAsync<PayCurve>(new Folds(new BrowserStorage()),
            new Dictionary<string, object?> { ["Inputs"] = river, ["Search"] = search ? river.SearchSplit() : null });
}
