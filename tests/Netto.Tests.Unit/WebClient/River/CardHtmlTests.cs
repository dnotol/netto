using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// A card's HTML read as a viewer reads it (CardHtml): a key a misspelt text leaves, the summary and its
// rate and its shape, the paragraphs, the lines and the order of their lists, the bubbles, one card cut
// out of the Plan page, and that page with every card open.
public class CardHtmlTests
{
    // A river with a card holding a section of its own, as the car's comparison is, another card after
    // it, and what the page holds around them.
    private const string Page =
        "<h1>From what your company earns</h1>" +
        "<div id=\"river\" class=\"river__flow\">" +
        "<section id=\"card-car\" class=\"module-card\"><h2>Company car</h2><section class=\"car__comparison\"><p>With the car</p></section><p>After it</p></section>" +
        "<section id=\"card-split\" class=\"module-card\"><h2>The split</h2><p>Its note</p></section>" +
        "</div>" +
        "<aside class=\"assumed\"><h2>Assumed</h2></aside>";

    // One line of a figure list and one of a drain list, as the components draw them.
    private const string FigureLine = "<ul class=\"figure-list\"><li class=\"figure\"><span class=\"figure__label\">Taxed</span><span class=\"figure__amount\">€1.00</span></li></ul>";

    private const string DrainLine = "<ul class=\"drain-list\"><li class=\"drain drain--passed-on drain--to-split\">" +
        "<span class=\"drain__label\">Passed on</span><span class=\"drain__amount\">€2.00</span></li></ul>";

    // The example with every lever in the plan, at the example's values.
    private static readonly RiverInputs EveryLever = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module));

    // As the renderer writes it, its characters encoded.
    [Fact]
    public void Prose_encodedText_isReadDecoded()
    {
        CardHtml.Prose("<p>&#xAB;&#xA0;Today&#xA0;&#xBB; isn&#x27;t &#x2212;&#x20AC;600.00</p>").ShouldBe(["« Today » isn't −€600.00"]);
    }

    // An element that holds nothing, void or closed by its own tag, leaves the text after it in its parent.
    [Fact]
    public void Prose_voidAndSelfClosedElements_holdNoTextAndSwallowNone()
    {
        CardHtml.Prose("<p>One<br>two<input value=\"typed\"><svg><path d=\"M0 0\" /></svg>three</p><p>four</p>").ShouldBe(["Onetwothree", "four"]);
    }

    // A misspelt key draws the key itself: words joined by an underscore, or run together each with its
    // capital.
    [Fact]
    public void LeakedKeys_keysDrawnForTheirTexts_areFound()
    {
        const string html = "<p>Figure_TaxedOn</p><span class=\"fold__summary\">SummaryNoCar</span><p>Drain_social_contributions to your pocket</p>";

        CardHtml.LeakedKeys(html).ShouldBe(["Figure_TaxedOn", "SummaryNoCar", "Drain_social_contributions"]);
    }

    // A key is a defect wherever it is drawn: in a bubble, in a folded body, or naming a control.
    [Fact]
    public void LeakedKeys_keysOutOfSight_areFound()
    {
        const string html = "<span class=\"info-bubble__text\">Help_FlatCharges</span><div hidden><p>Deduction_Rate</p></div>" +
            "<button aria-label=\"More about Figure_Co2\"></button>";

        CardHtml.LeakedKeys(html).ShouldBe(["Help_FlatCharges", "Deduction_Rate", "Figure_Co2"]);
    }

    // The site's own words: a share, a law's name, a unit, a brand, quotes, and the administration's
    // portal, whose name is run together as a key's is.
    [Fact]
    public void LeakedKeys_plainText_findsNone()
    {
        const string html = "<p title=\"Article 36, CIR 92\">VVPRbis shares, 118 g/km of CO2, Tax-Calc and Netto: €1,333.33 at 9.75%, « Today », " +
            "as MyMinfin shows it.</p>";

        CardHtml.LeakedKeys(html).ShouldBeEmpty();
    }

    // A card's heading as its fold draws it: the summary beside the title, ending on the rate.
    [Fact]
    public async Task SummaryAndRate_card_areWhatItsFoldSaysBesideItsTitle()
    {
        var html = await RenderFoldAsync(("Summary", "€3,333.33 to the split · €2,000.00 taxed"), ("Rate", "60% to you"));

        (CardHtml.Summary(html), CardHtml.Rate(html)).ShouldBe(("€3,333.33 to the split · €2,000.00 taxed", "60% to you"));
    }

    [Fact]
    public async Task SummaryAndRate_foldWithoutThem_areNone()
    {
        var html = await RenderFoldAsync();

        (CardHtml.Summary(html), CardHtml.Rate(html)).ShouldBe(((string?)null, (string?)null));
    }

    // Each paragraph in sight, a term read as its words, whitespace as one space.
    [Fact]
    public void Prose_card_isTheTextOfEachParagraphInSight()
    {
        const string html = "<div class=\"module-card__body\"><p class=\"rent__note\">Up to the " +
            "<button type=\"button\" class=\"info-bubble__trigger info-bubble__term\">cadastral income</button>" +
            "<span class=\"info-bubble__text\">What the register holds.</span>, revalued.</p>" +
            "<div hidden><p>Folded</p></div><ul><li>A line</li></ul><p>  Two\r\n   lines </p></div>";

        CardHtml.Prose(html).ShouldBe(["Up to the cadastral income, revalued.", "Two lines"]);
    }

    // Each line as FigureList draws it: its label without its bubble, its amount and its share.
    [Fact]
    public async Task Lines_figureList_areItsLabelsAmountsAndShares()
    {
        var html = await RenderAsync<FigureList>(new Folds(new BrowserStorage()), new()
        {
            ["Figures"] = new Figure[] { new("Your pay", 40_000m) { Share = 0.4m }, new("Flat charges", -1_333.33m) { Help = "What the administration takes." } },
        });

        CardHtml.Lines(html).ShouldBe([("Your pay", "€40,000.00", "40.0%"), ("Flat charges", "−€1,333.33", null)]);
    }

    // Each line as DrainList draws it, without where it goes.
    [Fact]
    public void Lines_drainList_areItsLabelsAmountsAndShares()
    {
        const string html = "<ul class=\"drain-list\"><li class=\"drain drain--paid-out drain--to-taxes_on_pay drain--shared\">" +
            "<span class=\"drain__label\">Social contributions</span><span class=\"drain__amount\">&#x2212;&#x20AC;8,000.00</span>" +
            "<span class=\"drain__to\">To contributions and tax on pay</span> <span class=\"drain__share\">20.0%</span></li>" +
            "<li class=\"drain drain--passed-on drain--to-split\"><span class=\"drain__label\">Passed on</span>" +
            "<span class=\"drain__amount\">&#x20AC;72,000.00</span><span class=\"drain__to\">To the split</span></li></ul>";

        CardHtml.LineAmounts(html).ShouldBe(["−€8,000.00", "€72,000.00"]);
    }

    [Fact]
    public void Lines_hidden_areLeftOut()
    {
        CardHtml.Lines("<div hidden><ul class=\"figure-list\"><li class=\"figure\"><span class=\"figure__label\">Folded</span><span class=\"figure__amount\">€1.00</span></li></ul></div>")
            .ShouldBeEmpty();
    }

    // The lists the lines come from, in the page's order and run by run: a card draws its figure lines
    // before its drain lines, and the runs say so without a word of what the lines say.
    [Fact]
    public void LineRuns_figureLinesThenDrainLines_areTwoRuns()
    {
        const string html = FigureLine + FigureLine + DrainLine;

        CardHtml.LineRuns(html).ShouldBe([("figure", 2), ("drain", 1)]);
    }

    // A figure line drawn after the drains, as Director pay's top-up is, is a run of its own.
    [Fact]
    public void LineRuns_figureLineAfterTheDrains_isARunOfItsOwn()
    {
        const string html = FigureLine + DrainLine + DrainLine + FigureLine;

        CardHtml.LineRuns(html).ShouldBe([("figure", 1), ("drain", 2), ("figure", 1)]);
    }

    [Fact]
    public void LineRuns_hiddenLinesAndNoLines_areNone()
    {
        CardHtml.LineRuns($"<div hidden>{FigureLine}</div><p>No line</p>").ShouldBeEmpty();
    }

    // The amounts a text draws, as Formats writes them, in its order: euros with their sign, or a share.
    [Fact]
    public void Amounts_text_isItsAmountsInOrder()
    {
        CardHtml.Amounts("€3,333.33 to the split · −€600.00 taxed · +€5.00 back · 62% to you · rate 9.75% · best pay €19,757.00")
            .ShouldBe(["€3,333.33", "−€600.00", "+€5.00", "62%", "9.75%", "€19,757.00"]);
    }

    [Fact]
    public void Amounts_textWithoutFigures_isEmpty()
    {
        CardHtml.Amounts("No royalty yet").ShouldBeEmpty();
    }

    // A summary is figures with their labels joined by « · »: a label is a few words, a figure's name, and
    // holds none of the punctuation a sentence would.
    [Theory]
    [InlineData("€2,062.00 to your pocket · €97,938.00 left")]
    [InlineData("€44,978.48 paid out · 37% to your pocket")]
    [InlineData("€50,000.00 to your pay · €30,086.95 to the company · best pay €19,757.00")]
    [InlineData("No royalty yet")]
    public void SummaryFaults_figuresWithTheirLabels_areNone(string summary)
    {
        CardHtml.SummaryFaults(summary).ShouldBeEmpty();
    }

    [Fact]
    public void SummaryFaults_sentence_isTheLabelHoldingItsPunctuation()
    {
        CardHtml.SummaryFaults("€5.00 is what you keep, after tax. · €3.00 left").ShouldBe(["is what you keep, after tax."]);
    }

    // A figure with no label says nothing of what it is.
    [Fact]
    public void SummaryFaults_figureWithNoLabel_isAFault()
    {
        CardHtml.SummaryFaults("€2,062.00 · €97,938.00 left").ShouldBe([""]);
    }

    // Every (i) of a page with its label and the text it is described by, as a screen reader is given it,
    // in the page's order; none without a text for a trigger whose text the page does not hold.
    [Fact]
    public void Bubbles_page_isEachTriggerWithTheTextItIsDescribedBy()
    {
        const string html = "<p>Flat charges<span class=\"info-bubble\"><button type=\"button\" class=\"info-bubble__trigger\" aria-label=\"More about Flat charges\" " +
            "aria-describedby=\"info-bubble-1\"></button><span id=\"info-bubble-1\" class=\"info-bubble__text\" aria-hidden=\"true\">" +
            "What the administration <em>takes</em> as costs.</span></span></p>" +
            "<p>Rent<button type=\"button\" aria-label=\"More about Rent\" aria-describedby=\"info-bubble-2\"></button></p>" +
            "<p><button type=\"button\" aria-label=\"Open the menu\"></button></p>";

        CardHtml.Bubbles(html).ShouldBe([("Flat charges", "What the administration takes as costs."), ("Rent", "")]);
    }

    // A card holding a section of its own is cut whole, and no further than its own end.
    [Fact]
    public void Card_page_isTheCardsSectionWithAllItHolds()
    {
        CardHtml.Prose(CardHtml.Card(Page, "car")).ShouldBe(["With the car", "After it"]);
    }

    [Fact]
    public void Card_nextCardOnThePage_isCutAtItsOwnEnd()
    {
        CardHtml.Prose(CardHtml.Card(Page, "split")).ShouldBe(["Its note"]);
    }

    // A card the page does not draw fails the test that asks for it.
    [Fact]
    public void Card_notOnThePage_fails()
    {
        Should.Throw<ShouldAssertException>(() => CardHtml.Card(Page, "rent"));
    }

    // The example's five cards, or ten with every lever in the plan, all open.
    public static TheoryData<bool, int> Rivers => new() { { false, 5 }, { true, 10 } };

    [Theory]
    [MemberData(nameof(Rivers))]
    public async Task RenderCardsOpenAsync_river_opensEveryCard(bool everyLever, int count)
    {
        var html = await CardHtml.RenderCardsOpenAsync(everyLever ? EveryLever : RiverInputs.Example);

        var cards = MapModules.Order.Select(MapModules.Name).Where(name => html.Contains($"id=\"card-{name}\"", StringComparison.Ordinal)).ToList();
        (cards.Count, cards.Count(name => IsOpen(CardHtml.Card(html, name)))).ShouldBe((count, count));
    }

    // Every text the page draws with every lever in the plan and every card open, in sight or not, is in
    // its component's texts.
    [Fact]
    public async Task RenderCardsOpenAsync_everyLever_leavesNoKeyOnThePage()
    {
        var html = await CardHtml.RenderCardsOpenAsync(EveryLever);

        CardHtml.LeakedKeys(html).ShouldBeEmpty();
    }

    // Every (i) the page draws with every lever in the plan and every card open, named « More about » its
    // label, is described by a text that says something.
    [Fact]
    public async Task RenderCardsOpenAsync_everyLever_everyBubbleSaysSomething()
    {
        var bubbles = CardHtml.Bubbles(await CardHtml.RenderCardsOpenAsync(EveryLever));

        bubbles.ShouldSatisfyAllConditions(
            () => bubbles.ShouldNotBeEmpty(),
            () => bubbles.Where(bubble => bubble.Text.Length == 0).Select(bubble => bubble.Label).ShouldBeEmpty());
    }

    private static Task<string> RenderFoldAsync(params (string Name, object? Value)[] more)
    {
        var parameters = new Dictionary<string, object?> { ["Name"] = "rent", ["Title"] = "Home office rent", ["ChildContent"] = Text("The card's body") };
        foreach (var (name, value) in more)
            parameters[name] = value;

        return RenderAsync<Fold>(new Folds(new BrowserStorage()), parameters);
    }
}
