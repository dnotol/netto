using System.Globalization;
using System.Net;
using System.Resources;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The river's outline as a viewer meets it on first load, drawn with the whole page without a browser
// (RiverRendering): a nav before the river listing every card on screen in reading order, each with its
// title and one figure of its summary, the first card marked, and a toggle that opens or folds them
// all. What a click does is OutlineRowsTests' and the browser's.
public class RiverOutlineTests
{
    private static readonly RiverInputs Everything = RiverInputs.Example
        .AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Allowances).AddToPlan(OptionalModule.Rent)
        .AddToPlan(OptionalModule.Car).AddToPlan(OptionalModule.Copyright);

    // Every lever with a figure to show: an electric car the company pays for, and a royalty following
    // the pay once the director's creative share is given.
    private static readonly RiverInputs EveryFigure = Everything
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
        .WithCreativeShare(0.5m);

    // The deadline still open on this date is December's.
    private static readonly DateTimeOffset On23October = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    // A landmark of its own, named by its visible title, before the cards in the page's order, so the
    // keyboard meets it once, after the bar and before the river.
    [Fact]
    public async Task Outline_isANavNamedByItsTitleBeforeTheRiver()
    {
        var html = await RenderPlanAsync(RiverInputs.Example);

        var nav = Regex.Match(html, "<nav [^>]*aria-labelledby=\"([^\"]+)\"");
        (nav.Success, Regex.IsMatch(html, $"<p [^>]*id=\"{nav.Groups[1].Value}\"[^>]*>River outline</p>"), nav.Index < html.IndexOf("id=\"river\"", StringComparison.Ordinal))
            .ShouldBe((true, true, true));
    }

    [Fact]
    public async Task Outline_example_listsItsCardsInReadingOrder()
    {
        var html = await RenderPlanAsync(RiverInputs.Example);

        Titles(html).ShouldBe(["Your moves", "Company year", "The split", "Director pay", "Corporate tax", "Dividend", "When to pay"]);
    }

    // The two streams read as the page reads them: the director's, the royalty first, then the company's.
    [Fact]
    public async Task Outline_everyLeverInThePlan_listsEveryCardInReadingOrder()
    {
        var html = await RenderPlanAsync(Everything);

        Titles(html).ShouldBe(
        [
            "Your moves", "Company year", "Vouchers", "Expense allowances", "Home office rent", "Company car", "The split", "Copyright royalty",
            "Director pay", "Corporate tax", "Dividend", "When to pay",
        ]);
    }

    // A card not on screen has no row: the split waits for the company's figures.
    [Fact]
    public async Task Outline_companySkipped_listsNoSplit()
    {
        var html = await RenderPlanAsync(RiverInputs.Example.SkipCompany());

        Titles(html).ShouldBe(["Your moves", "Company year", "Director pay", "Corporate tax", "Dividend", "When to pay"]);
    }

    // At the top of the page no card has reached the bar: the first one is marked as the one in view.
    [Fact]
    public async Task Outline_atTheTop_marksTheFirstCard()
    {
        var html = await RenderPlanAsync(RiverInputs.Example);

        Rows(html).Where(row => row.Contains("aria-current=\"location\"")).Select(Title).ShouldBe(["Your moves"]);
    }

    [Fact]
    public async Task Outline_cardsFolded_offersToOpenThemAll()
    {
        var html = await RenderPlanAsync(RiverInputs.Example);

        AllToggle(html).ShouldBe(("Open all", "Open all cards"));
    }

    [Fact]
    public async Task Outline_everyCardOpen_offersToFoldThemAll()
    {
        var folds = new Folds(new BrowserStorage());
        foreach (var name in OutlineRows.Order)
            folds.Set(name, true);

        var html = await RenderPlanAsync(Everything, folds);

        AllToggle(html).ShouldBe(("Fold all", "Fold all cards"));
    }

    // Each card's glance is one figure of its own summary, in the summary's words: the outline shows
    // nothing its card does not show.
    [Fact]
    public async Task Outline_everyGlance_isAFigureOfItsCardsSummary()
    {
        var html = await RenderPlanAsync(EveryFigure, today: EveryFigure);

        var glances = Rows(html).Where(row => Glance(row) is not null).Select(row => (Title: Title(row), Figure: Figure(Glance(row)!))).ToList();

        glances.ShouldSatisfyAllConditions(
            () => glances.Count.ShouldBe(12),
            () => glances.Where(glance => !Summary(html, glance.Title).Contains(glance.Figure)).Select(glance => $"{glance.Title}: {glance.Figure}").ShouldBeEmpty());
    }

    // What each card is about: what reaches the pocket for a lever, the pay and the dividend, where the
    // river starts, what the split gives the pay, what the company keeps after its tax, what the moves
    // add, and when the next payment falls.
    [Fact]
    public async Task Outline_everyCard_glancesAtWhatItIsAbout()
    {
        var html = await RenderPlanAsync(EveryFigure, today: EveryFigure);

        Rows(html).Select(row => (Title(row), Glance(row) is { } glance ? Kind(glance) : (GlanceKind?)null)).ShouldBe(
        [
            ("Your moves", GlanceKind.MoreThanToday), ("Company year", GlanceKind.In), ("Vouchers", GlanceKind.ToYou),
            ("Expense allowances", GlanceKind.ToYou), ("Home office rent", GlanceKind.ToYou), ("Company car", GlanceKind.MuddyPocket),
            ("The split", GlanceKind.ToYourPay), ("Copyright royalty", GlanceKind.ToYou), ("Director pay", GlanceKind.ToYou),
            ("Corporate tax", GlanceKind.LeftAfterTax), ("Dividend", GlanceKind.ToYou), ("When to pay", GlanceKind.NextBy),
        ]);
    }

    // A card whose summary has no figure has no glance: no plan starred Today yet, no car without its
    // papers, no royalty without the director's creative share.
    [Fact]
    public async Task Outline_cardWithoutAFigure_hasNoGlance()
    {
        var html = await RenderPlanAsync(Everything);

        Rows(html).Where(row => Glance(row) is null).Select(Title).ShouldBe(["Your moves", "Company car", "Copyright royalty"]);
    }

    // The icon of the bucket the figure lands in, which a screen reader skips: the pocket's for what
    // reaches it, the muddy pocket's for a car's costs, none for money still on its way.
    [Fact]
    public async Task Outline_glanceLandingInABucket_carriesItsIcon()
    {
        var html = await RenderPlanAsync(EveryFigure, today: EveryFigure);
        var pocket = BucketIcons.For("pocket");

        Rows(html).Select(row => (Title(row), Icon(row))).ShouldBe(
        [
            ("Your moves", null), ("Company year", null), ("Vouchers", pocket), ("Expense allowances", pocket), ("Home office rent", pocket),
            ("Company car", BucketIcons.For(BucketsView.Muddy)), ("The split", null), ("Copyright royalty", pocket), ("Director pay", pocket),
            ("Corporate tax", null), ("Dividend", pocket), ("When to pay", null),
        ]);
    }

    private static List<string> Rows(string html) =>
        [.. Regex.Matches(html, "<button[^>]*class=\"river-outline__row\"[^>]*>.*?</button>", RegexOptions.Singleline).Select(match => match.Value)];

    private static string Title(string row) => Regex.Match(row, "<span[^>]*class=\"river-outline__card\"[^>]*>([^<]*)</span>").Groups[1].Value;

    private static List<string> Titles(string html) => [.. Rows(html).Select(Title)];

    // The glance's words, without its icon.
    private static string? Glance(string row)
    {
        var glance = Regex.Match(row, "<span[^>]*class=\"river-outline__glance\"[^>]*>(.*?)</span>\\s*</button>", RegexOptions.Singleline);
        return glance.Success
            ? Regex.Replace(glance.Groups[1].Value, "<span[^>]*class=\"river-outline__icon\".*?</span>", "", RegexOptions.Singleline).Trim()
            : null;
    }

    private static string? Icon(string row)
    {
        var icon = Regex.Match(row, "<span[^>]*class=\"river-outline__icon\"[^>]*aria-hidden=\"true\"[^>]*>([^<]*)</span>");
        return icon.Success ? icon.Groups[1].Value : null;
    }

    // The kind whose words the glance is in, as RiverOutline.resx words each.
    private static GlanceKind Kind(string glance) => Enum.GetValues<GlanceKind>().First(kind => Words(kind).IsMatch(glance));

    private static string Figure(string glance) => Words(Kind(glance)).Match(glance).Groups["figure"].Value;

    private static Regex Words(GlanceKind kind)
    {
        var format = OutlineTexts.GetString(RiverOutlineText.Glance(kind), CultureInfo.InvariantCulture) ?? "";
        return new Regex("^" + Regex.Escape(format).Replace("\\{0}", "(?<figure>.+)") + "$");
    }

    private static readonly ResourceManager OutlineTexts = new("Netto.Web.Client.River.RiverOutline", typeof(RiverOutline).Assembly);

    // The summary on the button of the card titled so.
    private static string Summary(string html, string title) =>
        Regex.Match(html, $"<span[^>]*class=\"fold__title\"[^>]*>{Regex.Escape(title)}</span>\\s*<span[^>]*class=\"fold__summary\"[^>]*>(.*?)</span>",
            RegexOptions.Singleline).Groups[1].Value;

    private static (string Label, string Name) AllToggle(string html)
    {
        var toggle = Regex.Match(html, "<button[^>]*class=\"river-outline__all\"[^>]*>([^<]*)</button>");
        return (toggle.Groups[1].Value, Regex.Match(toggle.Value, "aria-label=\"([^\"]*)\"").Groups[1].Value);
    }

    private static async Task<string> RenderPlanAsync(RiverInputs inputs, Folds? folds = null, RiverInputs? today = null)
    {
        var storage = new BrowserStorage();
        var plans = new SavedPlans(storage);
        if (today is not null)
            plans.Star(plans.Save(today, number => $"Plan {number}").Id);

        return WebUtility.HtmlDecode(await RenderAsync(typeof(Plan), folds ?? new Folds(new BrowserStorage()), [], services =>
        {
            services.AddSingleton<IBrowserStorage>(storage);
            services.AddSingleton(plans);
            services.AddSingleton(new RiverState(plans) { Inputs = inputs });
            services.AddSingleton<TimeProvider>(new Clock(On23October));
        }));
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
