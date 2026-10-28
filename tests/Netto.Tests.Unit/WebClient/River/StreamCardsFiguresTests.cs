using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The split and the cards of the two streams after it, Director pay and Corporate tax, as a viewer reads
// them open on a plan filled with every lever (CardHtml): each draws its figures as lines of label,
// amount and share, and keeps a sentence only for what a figure cannot say. What these tests hold is the
// amounts and shares drawn, their order, and the shape of what stands around them, never the words. The
// plan grants 220 days of
// meal vouchers with 250 of eco and 100 of sport/culture vouchers, pays 240 for internet, 240 for a
// computer and 600 of phone costs, lets 12% of a home of 1,200 of cadastral income for 1,380, holds
// YearSimulationCarTests' electric car (50,000 of catalogue value, 8,000 of yearly costs) and pays a
// royalty that follows the pay, at half of the director's time spent creating.
public class StreamCardsFiguresTests
{
    private static readonly RiverInputs Filled = RiverInputs.Example
        .AddToPlan(OptionalModule.Vouchers).WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
        .AddToPlan(OptionalModule.Allowances).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m)
        .AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
        .AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.5m);

    // The Plan page on the filled plan with every card open, drawn once for the tests that read it.
    private static readonly Lazy<Task<string>> Page = new(() => CardHtml.RenderCardsOpenAsync(Filled));

    // The texts of Director pay's .resx, for the rules its bubbles say, read where a test needs one.
    private static readonly ResxText Texts = ResxText.For<DirectorPayCard>();

    // The split's, for the words of its button.
    private static readonly ResxText SplitTexts = ResxText.For<SplitCard>();

    // Corporate tax's, for the sentence its disclosure states about the surcharge.
    private static readonly ResxText CorporateTexts = ResxText.For<CorporateTaxCard>();

    // The example with nothing withheld on the pay, which leaves a surcharge due on its tax.
    private static readonly RiverInputs NothingWithheld = RiverInputs.Example.WithTaxWithheld(0m);

    public static TheoryData<string> Cards => new() { "split", "director-pay", "corporate-tax" };

    // A card's summary is its figures, each with the label that names it, and no sentence.
    [Theory]
    [MemberData(nameof(Cards))]
    public async Task Summary_filledPlan_isItsFiguresWithTheirLabels(string card)
    {
        CardHtml.SummaryFaults(CardHtml.Summary(await CardAsync(card)).ShouldNotBeNull()).ShouldBeEmpty();
    }

    // A sentence that restates a figure its lines show is gone: no paragraph in sight holds an amount
    // one of the card's figure or drain lines draws, on the filled plan, on the example, with a
    // surcharge due on the pay's tax, and with the whole base of that tax paid by 10 April, which earns
    // a bonus.
    public static TheoryData<string, RiverInputs, string> CardsOfEachPlan => new()
    {
        { "the filled plan", Filled, "split" },
        { "the example", RiverInputs.Example, "split" },
        { "the filled plan", Filled, "director-pay" },
        { "the example", RiverInputs.Example, "director-pay" },
        { "nothing withheld", NothingWithheld, "director-pay" },
        { "a bonus", NothingWithheld.WithTaxAdvancePayment(0, 12_014m), "director-pay" },
        { "the filled plan", Filled, "corporate-tax" },
        { "the example", RiverInputs.Example, "corporate-tax" },
    };

    [Theory]
    [MemberData(nameof(CardsOfEachPlan))]
    public async Task Card_open_proseRestatesNoAmountOfItsLines(string _, RiverInputs plan, string card)
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(plan), card);
        var lines = CardHtml.LineAmounts(html).Select(Unsigned).ToHashSet();

        CardHtml.Prose(html).Where(paragraph => CardHtml.Amounts(paragraph).Any(amount => lines.Contains(Unsigned(amount)))).ShouldBeEmpty();
    }

    // Every text a card draws, in sight or not, is in its .resx.
    [Theory]
    [MemberData(nameof(Cards))]
    public async Task Card_open_leavesNoKey(string card)
    {
        CardHtml.LeakedKeys(await CardAsync(card)).ShouldBeEmpty();
    }

    // What the split shares, each side out of the 87,229.80 the levers leave it, then the smallest pay
    // keeping the reduced rate with the royalty and without it (YearConditions' two pays, 40,710.67 and
    // 43,618.57 here), then the best pay the search found and what it leaves, with what reaches the pocket there and at the pay set out of what
    // leaves the company: the amount and the share of each line, in order.
    [Fact]
    public async Task Split_filledPlan_drawsItsSidesThenTheBestPayAsLines()
    {
        CardHtml.Lines(await CardAsync("split")).Select(line => (line.Amount, line.Share)).ShouldBe(
        [
            ("€50,000.00", "57.3%"),
            ("€7,142.85", "8.2%"),
            ("€30,086.95", "34.5%"),
            ("€40,710.67", null),
            ("€43,618.57", null),
            ("€19,757.00", null),
            ("€46,937.35", null),
            ("€2,822.42", null),
            ("€60,866.97", "66.2%"),
            ("€2,619.44", null),
            ("€58,247.53", "63.3%"),
        ]);
    }

    // How far the pay goes is a bound, in sight under its field and never in a bubble; why, the field's
    // bubble says.
    [Fact]
    public async Task Split_filledPlan_keepsThePaysBoundInSightAndWhyInItsBubble()
    {
        var html = await CardAsync("split");
        var bubble = CardHtml.BubbleAfter(html, "Gross director pay for the year");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Prose(html).ShouldContain(paragraph => paragraph.Contains("€76,326.08", StringComparison.Ordinal)),
            () => bubble.ShouldNotBeNullOrWhiteSpace(),
            () => bubble.ShouldNotContain("€76,326.08"));
    }

    // How the best pay stands against the reduced rate is a sentence, with what keeping the rate would
    // leave less, no line saying it.
    [Fact]
    public async Task Split_filledPlan_weighsTheReducedRateInASentence()
    {
        var html = await CardAsync("split");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Prose(html).ShouldContain(paragraph => paragraph.Contains("€1,016.20", StringComparison.Ordinal)),
            () => CardHtml.LineAmounts(html).Select(Unsigned).ShouldNotContain("€1,016.20"));
    }

    // A refused pay leaves no figure to draw: the refusal stands in sight where the lines were.
    [Fact]
    public async Task Split_payRefused_showsTheRefusalInSightAndNoLines()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example.WithDirectorPay(-1m)), "split");

        (CardHtml.Lines(html).Count, Regex.IsMatch(html, "class=\"[^\"]*refused-message")).ShouldBe((0, true));
    }

    // The river's best pay, whatever the facts the river holds now, is offered by a button of its own
    // while the pay set is not it: not at the best, where no pay keeps the reserve, or while the pay is
    // refused and there is no search to read it from. The Optimisation tab's pay line is another
    // baseline's, and is not this button.
    public static TheoryData<string, RiverInputs, int> BestPayButtons => new()
    {
        { "the pay set away from the best", RiverInputs.Example, 1 },
        { "the pay set at the best", RiverInputs.Example.WithDirectorPay(BestPay(RiverInputs.Example)), 0 },
        { "no pay keeping the reserve", RiverInputs.Example.WithReserve(80_000m), 0 },
        { "the pay refused", RiverInputs.Example.WithDirectorPay(-1m), 0 },
    };

    [Theory]
    [MemberData(nameof(BestPayButtons))]
    public async Task Split_open_offersTheBestPayOnlyWhileThePaySetIsNotIt(string _, RiverInputs plan, int buttons)
    {
        var html = WebUtility.HtmlDecode(CardHtml.Card(await CardHtml.RenderCardsOpenAsync(plan), "split"));

        ButtonsSaying(html, SplitTexts.Get("MoveToBest").ShouldNotBeNull()).ShouldBe(buttons);
    }

    // The button says which pay it moves to for a screen reader, its visible words a part of its name.
    [Fact]
    public async Task Split_open_namesTheBestPayInTheButtonsLabel()
    {
        var plan = RiverInputs.Example;
        var html = WebUtility.HtmlDecode(CardHtml.Card(await CardHtml.RenderCardsOpenAsync(plan), "split"));
        var label = SplitTexts.Format("MoveToBestLabel", Formats.Euros(BestPay(plan)));

        html.ShouldContain($"aria-label=\"{label}\"");
        label.ShouldStartWith(SplitTexts.Get("MoveToBest").ShouldNotBeNull());
    }

    // Until the pointer is on the curve, its line keeps its room in the card, empty.
    [Fact]
    public async Task Split_firstDraw_keepsThePointersLineEmpty()
    {
        Regex.IsMatch(await CardAsync("split"), @"<p class=""split__pointer""[^>]*>\s*</p>").ShouldBeTrue();
    }

    // The gross the split gives, from which the lines go on: what the car's benefit adds to what the pay
    // is taxed on, then where the pay goes.
    [Fact]
    public async Task DirectorPay_filledPlan_drawsWhatThePayIsTaxedOnThenWhereItGoes()
    {
        var html = await CardAsync("director-pay");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Prose(html)[0].ShouldContain("€50,000.00"),
            () => CardHtml.Lines(html).Select(line => (line.Amount, line.Share)).ShouldBe(
            [
                ("€1,714.29", null),
                ("€51,714.29", null),
                ("−€8,577.72", "17.2%"),
                ("−€11,958.11", "23.9%"),
                ("−€837.07", "1.7%"),
                ("€28,627.10", "57.3%"),
            ]),
            () => CardHtml.LineRuns(html).ShouldBe([("figure", 2), ("drain", 4)]));
    }

    // The fund's fee at 3.05% is a drain line of its own right after the contributions, with its share of
    // the pay: 251.68 of the example's 50,000 (GrossToNetFundFeeTests).
    [Fact]
    public async Task DirectorPay_fundFeeGiven_drawsTheFeeLineAfterTheContributions()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example.WithFundFeeRate(0.0305m)), "director-pay");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).Take(2).Select(line => (line.Amount, line.Share)).ShouldBe([("−€8,251.60", "16.5%"), ("−€251.68", "0.5%")]),
            () => CardHtml.Lines(html)[1].Label.ShouldBe(ResxText.For<DrainList>().Get("Drain_fund_fee").ShouldNotBeNull()),
            () => CardHtml.LeakedKeys(html).ShouldBeEmpty());
    }

    // The fee's field sits in « Your situation » and names no origin until a rate is given, then the
    // user's, as the royalty's creative share.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task DirectorPay_situationOpen_drawsTheFeesFieldWithItsOriginOnlyOnceGiven(bool feeGiven, bool yours)
    {
        var river = feeGiven ? RiverInputs.Example.WithFundFeeRate(0.0305m) : RiverInputs.Example;

        var html = await RenderAsync(typeof(DirectorPayCard), OpenCard("director-pay"), new() { ["Inputs"] = river, ["Run"] = river.Run(), ["Refused"] = null },
            services => services.AddSingleton<IComponentActivator>(new OpenDisclosures()));

        var field = FieldHead(html, "river-fund-fee");
        (field.Contains("origin-chip", StringComparison.Ordinal), field.Contains("origin-chip--yours", StringComparison.Ordinal)).ShouldBe((yours, yours));
    }

    // Once a fee is given, the card's details say how Netto reads it where no text names it as deductible,
    // above the pay slip and the sources; without one they do not.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DirectorPay_detailsOpen_sayNettosReadingOfTheFeeOnlyOnceOneIsGiven(bool feeGiven)
    {
        var river = feeGiven ? RiverInputs.Example.WithFundFeeRate(0.0305m) : RiverInputs.Example;
        var folds = OpenCard("director-pay");
        folds.Set("director-pay-details", true);

        var html = WebUtility.HtmlDecode(await RenderAsync(typeof(DirectorPayCard), folds, new() { ["Inputs"] = river, ["Run"] = river.Run(), ["Refused"] = null }));

        CardHtml.Prose(html).Contains(Texts.Get("FundFeeReading").ShouldNotBeNull()).ShouldBe(feeGiven);
    }

    // With no surcharge due, the card says why in a note, in sight, and draws no line for it.
    [Fact]
    public async Task DirectorPay_surchargeUnderTheMinimum_drawsItsNoteAndNoTopUp()
    {
        var html = await CardAsync("director-pay");

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("class=\"director-pay__condition\""),
            () => CardHtml.LineRuns(html).Select(run => run.List).ShouldBe(["figure", "drain"]));
    }

    // Due, the surcharge is a drain line, and what more by the first deadline still open leaves none due a
    // line of its own, a figure line drawn after the drains. The example's pay has no benefit in kind, so no
    // figure line stands before them. The page's clock is 23 October, when 21 December, the last, is the
    // one deadline left.
    [Fact]
    public async Task DirectorPay_nothingWithheld_drawsWhatMoreByTheFirstOpenDeadlineLeavesNoneDue()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(NothingWithheld), "director-pay");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.LineRuns(html).Select(run => run.List).ShouldBe(["drain", "figure"]),
            () => CardHtml.Lines(html).Select(line => (line.Amount, line.Share)).Last().ShouldBe(("€14,317.50", null)),
            () => html.ShouldNotContain("class=\"director-pay__condition\""));
    }

    // The line follows the browser's date: it names the first deadline not yet past on it, before the
    // first, on a deadline and between two, with the amount the Optimisation tab's payment move proposes
    // for that date. 2026's deadlines are 10 April, 10 July, 12 October and 21 December.
    public static TheoryData<DateOnly, string> OpenDeadlineDays => new()
    {
        { new(2026, 3, 1), "10 April 2026" },
        { new(2026, 4, 10), "10 April 2026" },
        { new(2026, 7, 15), "12 October 2026" },
        { new(2026, 10, 13), "21 December 2026" },
    };

    [Theory]
    [MemberData(nameof(OpenDeadlineDays))]
    public async Task DirectorPay_nothingWithheldOnAnotherDay_drawsTheFirstDeadlineStillOpenAndThePaymentMovesFigure(DateOnly today, string day)
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(NothingWithheld, At(today)), "director-pay");

        var move = new DirectorAdvancePaymentFinder(today).Find(NothingWithheld.MoveScenario()).ShouldBeOfType<DirectorAdvancePaymentMove>();
        html.ShouldSatisfyAllConditions(
            () => CardHtml.LineRuns(html).Select(run => run.List).ShouldBe(["drain", "figure"]),
            () => CardHtml.Lines(html).Select(line => (line.Label, line.Amount)).Last().ShouldBe((Texts.Format("Surcharge_Due", day), Formats.Euros(move.Amount))),
            () => html.ShouldNotContain("class=\"director-pay__condition\""));
    }

    // Once every deadline is past no payment can still leave none due: the line gives way to a note that
    // says so, the surcharge staying among the drains.
    [Fact]
    public async Task DirectorPay_everyDeadlinePast_drawsItsNoteInPlaceOfTheLine()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(NothingWithheld, At(new DateOnly(2026, 12, 22))), "director-pay");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.LineRuns(html).Select(run => run.List).ShouldBe(["drain"]),
            () => CardHtml.Prose(html).ShouldContain(Texts.Get("Surcharge_TooLate").ShouldNotBeNull()),
            () => CardHtml.Lines(html).Select(line => line.Label).ShouldNotContain(label => label.StartsWith("More by", StringComparison.Ordinal)));
    }

    // In its Advance payments disclosure, Corporate tax says what more by the first deadline still open
    // cancels its surcharge, as the company's payment move proposes for that date.
    [Theory]
    [MemberData(nameof(OpenDeadlineDays))]
    public async Task CorporateTax_onAnotherDay_saysWhatMoreByTheFirstDeadlineStillOpenCancelsTheSurcharge(DateOnly today, string day)
    {
        var html = await CorporateTaxAsync(At(today));

        var move = new CompanyAdvancePaymentFinder(today).Find(RiverInputs.Example.MoveScenario()).ShouldBeOfType<CompanyAdvancePaymentMove>();
        CardHtml.Prose(html).ShouldContain(CorporateTexts.Format("Surcharge_Due", Formats.Euros(move.Amount), day));
    }

    [Fact]
    public async Task CorporateTax_everyDeadlinePast_saysNoPaymentCanStillCancelTheSurcharge()
    {
        var html = await CorporateTaxAsync(At(new DateOnly(2026, 12, 22)));

        CardHtml.Prose(html).ShouldContain(CorporateTexts.Get("Surcharge_TooLate").ShouldNotBeNull());
    }

    // How the withholding is derived from the pay slip, and what the key formula is, are rules: the
    // withheld field's bubble says them, and no note under the field says them again. A refused advance
    // payment opens the field's disclosure here.
    [Fact]
    public async Task DirectorPay_withheldAsThePaySlip_explainsTheWithholdingInTheFieldsBubble()
    {
        var hint = Texts.Get("WithheldHint").ShouldNotBeNull();
        var html = WebUtility.HtmlDecode(CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example.WithTaxAdvancePayment(0, -1m)), "director-pay"));

        html.ShouldSatisfyAllConditions(
            () => FieldHead(html, "river-tax-withheld").ShouldContain(hint),
            () => CardHtml.Prose(html).ShouldNotContain(paragraph => paragraph.Contains(hint, StringComparison.Ordinal)));
    }

    // An amount typed replaces the pay slip's, as its origin says: only the way back to the pay slip's
    // amount stays under the field, and the rule is still the bubble's alone.
    [Fact]
    public async Task DirectorPay_amountWithheldTyped_keepsOnlyTheWayBackToThePaySlipsAmount()
    {
        var html = WebUtility.HtmlDecode(CardHtml.Card(await CardHtml.RenderCardsOpenAsync(RiverInputs.Example.WithTaxWithheld(-1m)), "director-pay"));

        (html.Contains(Texts.Get("WithheldAsPaySlip").ShouldNotBeNull(), StringComparison.Ordinal),
                CardHtml.Prose(html).Any(paragraph => paragraph.Contains(Texts.Get("WithheldHint").ShouldNotBeNull(), StringComparison.Ordinal)))
            .ShouldBe((true, false));
    }

    // What the taxable income adds back to the profit the split leaves the company, then where that
    // profit goes, each drain out of it: the amount and the share of each line, in order.
    [Fact]
    public async Task CorporateTax_filledPlan_drawsWhatItAddsBackThenWhereTheProfitGoes()
    {
        var html = await CardAsync("corporate-tax");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).Select(line => (line.Amount, line.Share)).ShouldBe(
            [
                ("€31,808.58", null),
                ("€1,430.20", null),
                ("€291.43", null),
                ("−€6,361.72", "21.1%"),
                ("−€429.42", "1.4%"),
                ("€23,295.81", "77.4%"),
            ]),
            () => CardHtml.LineRuns(html).ShouldBe([("figure", 3), ("drain", 3)]));
    }

    // Which condition decided the rate stays in sight, with the minimum pay and the share of it the
    // benefits may reach, figures no line shows.
    [Fact]
    public async Task CorporateTax_filledPlan_keepsTheConditionOfTheRateInSight()
    {
        var condition = CardHtml.Prose(await CardAsync("corporate-tax")).SingleOrDefault(paragraph => paragraph.Contains("€51,000.00", StringComparison.Ordinal));

        CardHtml.Amounts(condition.ShouldNotBeNull()).ShouldBe(["€51,000.00", "20%"]);
    }

    // What a small company is is the checkbox's bubble: its text is the bubble's, the bubble beside the
    // checkbox and never inside its label, where a click on the label would toggle it, and no paragraph
    // under the checkbox says it again. The card is drawn with its disclosures open (OpenDisclosures).
    [Fact]
    public async Task CorporateTax_smallCompany_explainsItInABubbleOutsideTheCheckboxsLabel()
    {
        var texts = InterfaceTexts.Of(typeof(CorporateTaxCard)).ToDictionary(text => text.Key, text => text.Value);
        var html = WebUtility.HtmlDecode(await RenderAsync(typeof(CorporateTaxCard), OpenCard("corporate-tax"),
            new() { ["Inputs"] = RiverInputs.Example, ["Run"] = RiverInputs.Example.Run(), ["Refused"] = null },
            services => services.AddSingleton<IComponentActivator>(new OpenDisclosures())));
        var label = Regex.Match(html, @"<label[^>]*>(?:(?!</label>).)*</label>", RegexOptions.Singleline);

        html.ShouldSatisfyAllConditions(
            () => Regex.IsMatch(html, $"class=\"info-bubble__text\"[^>]*>{Regex.Escape(texts["SmallCompanyHint"])}</span>").ShouldBeTrue(),
            () => (label.Value.Contains(texts["SmallCompanyLabel"], StringComparison.Ordinal), label.Value.Contains("info-bubble", StringComparison.Ordinal)).ShouldBe((true, false)),
            () => CardHtml.Prose(html).ShouldNotContain(paragraph => paragraph.Contains(texts["SmallCompanyHint"], StringComparison.Ordinal)));
    }

    private static async Task<string> CardAsync(string card) => CardHtml.Card(await Page.Value, card);

    // Noon on that day on the browser's clock.
    private static DateTimeOffset At(DateOnly day) => new(day.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

    // The Corporate tax card on the example, open with its disclosures, on the browser's clock at that
    // instant, its HTML decoded to the text a viewer reads.
    private static async Task<string> CorporateTaxAsync(DateTimeOffset on) =>
        WebUtility.HtmlDecode(await RenderAsync(typeof(CorporateTaxCard), OpenCard("corporate-tax"),
            new() { ["Inputs"] = RiverInputs.Example, ["Run"] = RiverInputs.Example.Run(), ["Refused"] = null },
            services =>
            {
                services.AddSingleton<IComponentActivator>(new OpenDisclosures());
                services.AddSingleton<TimeProvider>(new Clock(on));
            }));

    // The best pay the split's search finds for the river as it stands.
    private static decimal BestPay(RiverInputs river) => river.SearchSplit().ShouldNotBeNull().Best.ShouldNotBeNull().Pay;

    // The buttons of a card that say that, whole.
    private static int ButtonsSaying(string html, string text) =>
        Regex.Count(html, $"<button\\b[^>]*>(?:(?!</button>).)*?{Regex.Escape(text)}", RegexOptions.Singleline);

    // A card opened by the viewer, as a fold of its own.
    private static Folds OpenCard(string name)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(name, true);
        return folds;
    }

    private static string Unsigned(string amount) => amount.TrimStart('−', '+');
}
