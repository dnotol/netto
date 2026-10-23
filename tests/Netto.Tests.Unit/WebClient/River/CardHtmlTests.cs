using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// A card's HTML read as a viewer reads it (CardHtml): the words in sight, a key a misspelt text leaves,
// the summary and its rate, the paragraphs, the lines, one card or the river cut out of the Plan page,
// and that page with every card open.
public class CardHtmlTests
{
    // A river with a card holding a section of its own, as the car's comparison is, and what the page
    // holds around the river.
    private const string Page =
        "<h1>From what your company earns</h1>" +
        "<div id=\"river\" class=\"river__flow\">" +
        "<section id=\"card-car\" class=\"module-card\"><h2>Company car</h2><section class=\"car__comparison\"><p>With the car</p></section><p>After it</p></section>" +
        "<section id=\"card-split\" class=\"module-card\"><h2>The split</h2></section>" +
        "</div>" +
        "<aside class=\"assumed\"><h2>Assumed</h2></aside>";

    // The example with every lever in the plan, at the example's values.
    private static readonly RiverInputs EveryLever = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module));

    [Fact]
    public void VisibleWords_text_isItsWordsInTheirOrder()
    {
        CardHtml.VisibleWords("<section><h2>Home office rent</h2><p>Joins your taxable income</p></section>")
            .ShouldBe(["Home", "office", "rent", "Joins", "your", "taxable", "income"]);
    }

    // What a bubble says is in the page for a screen reader, out of sight.
    [Fact]
    public void VisibleWords_bubblesText_isLeftOut()
    {
        const string html = "<p>Flat charges<span class=\"info-bubble\"><button type=\"button\" class=\"info-bubble__trigger info-bubble__icon\" " +
            "aria-label=\"More about Flat charges\"></button><span id=\"info-bubble-1\" class=\"info-bubble__text\" aria-hidden=\"true\">" +
            "What the administration <em>takes</em> as costs.</span></span></p>";

        CardHtml.VisibleWords(html).ShouldBe(["Flat", "charges"]);
    }

    // A folded body or a closed disclosure is hidden, with all it holds.
    [Fact]
    public void VisibleWords_hiddenElement_isLeftOutWithAllItHolds()
    {
        CardHtml.VisibleWords("<div class=\"fold__body\" hidden><p>Your pay <span>slip</span></p></div><p>Shown</p>").ShouldBe(["Shown"]);
    }

    // A word holds a letter or a digit: a separator alone is none.
    [Fact]
    public void VisibleWords_separatorsAlone_areNoWords()
    {
        CardHtml.VisibleWords("<p>€3,333.33 · 62% to you — « Today »</p>").ShouldBe(["€3,333.33", "62%", "to", "you", "Today"]);
    }

    // Each text is read on its own, as the page draws a title and its summary apart.
    [Fact]
    public void VisibleWords_textsOfTwoElements_areWordsApart()
    {
        CardHtml.VisibleWords("<button><span>Rent</span><span>€3,333.33</span></button>").ShouldBe(["Rent", "€3,333.33"]);
    }

    // As the renderer writes it, its characters encoded.
    [Fact]
    public void VisibleWords_encodedText_isReadDecoded()
    {
        CardHtml.VisibleWords("<p>&#xAB;&#xA0;Today&#xA0;&#xBB; isn&#x27;t &#x2212;&#x20AC;600.00</p>").ShouldBe(["Today", "isn't", "−€600.00"]);
    }

    // An element that holds nothing, void or closed by its own tag, leaves the text after it in its parent.
    [Fact]
    public void VisibleWords_voidAndSelfClosedElements_holdNoText()
    {
        CardHtml.VisibleWords("<p>One<br>two<input value=\"typed\"><svg><path d=\"M0 0\" /></svg>three</p><p>four</p>")
            .ShouldBe(["One", "two", "three", "four"]);
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

    // A card holding a section of its own is cut whole.
    [Fact]
    public void Card_page_isTheCardsSectionWithAllItHolds()
    {
        CardHtml.VisibleWords(CardHtml.Card(Page, "car")).ShouldBe(["Company", "car", "With", "the", "car", "After", "it"]);
    }

    [Fact]
    public void River_page_isTheCardsWithoutWhatThePageHoldsAroundThem()
    {
        CardHtml.VisibleWords(CardHtml.River(Page)).ShouldBe(["Company", "car", "With", "the", "car", "After", "it", "The", "split"]);
    }

    // A card the page does not draw fails the test that asks for it.
    [Fact]
    public void Card_notOnThePage_fails()
    {
        Should.Throw<ShouldAssertException>(() => CardHtml.Card(Page, "rent"));
    }

    // The example's five cards, or ten with every lever in the plan, all open, as « Open all » leaves them;
    // the notes under the river stay folded.
    public static TheoryData<bool, int> Rivers => new() { { false, 5 }, { true, 10 } };

    [Theory]
    [MemberData(nameof(Rivers))]
    public async Task RenderPlanOpenAsync_river_opensEveryCardAndNotTheNotes(bool everyLever, int count)
    {
        var html = await CardHtml.RenderPlanOpenAsync(everyLever ? EveryLever : RiverInputs.Example);

        var cards = OutlineRows.Order.Where(name => html.Contains($"id=\"card-{name}\"", StringComparison.Ordinal)).ToList();
        var notes = Regex.Matches(html, "<aside class=\"(?:assumed|limits)\".*?</aside>", RegexOptions.Singleline);
        (cards.Count, cards.Count(name => IsOpen(CardHtml.Card(html, name))), notes.Count, notes.Count(note => IsOpen(note.Value))).ShouldBe((count, count, 2, 0));
    }

    // Every text the page draws with every lever in the plan and every card open, in sight or not, is in
    // its component's texts.
    [Fact]
    public async Task RenderPlanOpenAsync_everyLever_leavesNoKeyOnThePage()
    {
        var html = await CardHtml.RenderPlanOpenAsync(EveryLever);

        CardHtml.LeakedKeys(html).ShouldBeEmpty();
    }

    private static Task<string> RenderFoldAsync(params (string Name, object? Value)[] more)
    {
        var parameters = new Dictionary<string, object?> { ["Name"] = "rent", ["Title"] = "Home office rent", ["ChildContent"] = Text("The card's body") };
        foreach (var (name, value) in more)
            parameters[name] = value;

        return RenderAsync<Fold>(new Folds(new BrowserStorage()), parameters);
    }
}
