using System.Net;
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

    private static List<string> Rows(string html) =>
        [.. Regex.Matches(html, "<button[^>]*class=\"river-outline__row\"[^>]*>.*?</button>", RegexOptions.Singleline).Select(match => match.Value)];

    private static string Title(string row) => Regex.Match(row, "<span[^>]*class=\"river-outline__card\"[^>]*>([^<]*)</span>").Groups[1].Value;

    private static List<string> Titles(string html) => [.. Rows(html).Select(Title)];

    private static (string Label, string Name) AllToggle(string html)
    {
        var toggle = Regex.Match(html, "<button[^>]*class=\"river-outline__all\"[^>]*>([^<]*)</button>");
        return (toggle.Groups[1].Value, Regex.Match(toggle.Value, "aria-label=\"([^\"]*)\"").Groups[1].Value);
    }

    private static async Task<string> RenderPlanAsync(RiverInputs inputs, Folds? folds = null)
    {
        var storage = new BrowserStorage();
        var plans = new SavedPlans(storage);

        return WebUtility.HtmlDecode(await RenderAsync(typeof(Plan), folds ?? new Folds(new BrowserStorage()), [], services =>
        {
            services.AddSingleton<IBrowserStorage>(storage);
            services.AddSingleton(plans);
            services.AddSingleton(new RiverState(plans) { Inputs = inputs });
            services.AddSingleton(TimeProvider.System);
        }));
    }
}
