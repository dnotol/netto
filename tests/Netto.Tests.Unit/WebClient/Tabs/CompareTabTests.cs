using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Compare plans tab as a viewer meets it on arriving, drawn without a browser (RiverRendering): the
// head once, then what the saved plans allow. Picking a plan and the tab following the saved plans are
// events; what they choose is CompareViewTests', what the table says CompareRowsTests'.
public class CompareTabTests
{
    private static readonly RiverInputs BestPay = RiverInputs.Example.WithDirectorPay(20_799m);

    [Fact]
    public async Task Compare_noPlan_saysHowToGetOneAndDrawsNoPickerNorTable()
    {
        var html = await RenderTabAsync(Saved());

        (html.Contains("There is no plan to compare yet"), html.Contains("<select"), html.Contains("<table")).ShouldBe((true, false, false));
    }

    // The head stays when there is nothing to compare, so the page always opens on its title.
    [Fact]
    public async Task Compare_everyState_drawsTheHeadOnce()
    {
        var heads = new List<int>();
        foreach (var plans in new[] { Saved(), Saved(RiverInputs.Example), Saved(RiverInputs.Example, BestPay) })
            heads.Add(Regex.Matches(await RenderTabAsync(plans), "<h1[^>]*>Compare plans</h1>").Count);

        heads.ShouldBe([1, 1, 1]);
    }

    [Fact]
    public async Task Compare_storageCouldNotBeRead_saysSo()
    {
        var storage = new BrowserStorage();
        storage.Set(PlanStorage.Key, "{ broken");

        var html = await RenderTabAsync(new SavedPlans(storage));

        html.Contains("could not be read by this version of Netto").ShouldBeTrue();
    }

    [Fact]
    public async Task Compare_onePlan_namesItAndAsksForASecond()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example));

        (html.Contains("You have one saved plan, « Plan 1 »"), html.Contains("<select")).ShouldBe((true, false));
    }

    // Today first, the plan the river was opened from second, each plan the other picker holds out of reach.
    [Fact]
    public async Task Compare_twoPlans_picksTodayAgainstTheOpenedPlan()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        Selects(html).ShouldBe(
        [
            "compare-plan | Plan | Plan 1 (Today) selected; Plan 2 disabled",
            "compare-against | Against | Plan 1 (Today) disabled; Plan 2 selected",
        ]);
    }

    [Fact]
    public async Task Compare_table_hasColumnRowAndGroupHeaders()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        var table = Between(html, "<table", "</table>");
        (Regex.Matches(table, "<th scope=\"col\"").Count, Regex.Matches(table, "<th scope=\"rowgroup\"").Count, Regex.Matches(table, "<th scope=\"row\"").Count)
            .ShouldBe((4, 3, 10));
    }

    [Fact]
    public async Task Compare_table_isNamedByItsHeadingAndScrollsWithTheKeyboard()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        (Regex.IsMatch(html, "<h2[^>]*id=\"compare-table-title\"[^>]*>Figure by figure</h2>"), Regex.IsMatch(html, "<table[^>]*aria-labelledby=\"compare-table-title\""),
            Regex.IsMatch(html, "<div[^>]*role=\"region\"[^>]*tabindex=\"0\"|<div[^>]*tabindex=\"0\"[^>]*role=\"region\"")).ShouldBe((true, true, true));
    }

    // The columns say whose they are, and the difference whose less whose.
    [Fact]
    public async Task Compare_columns_nameThePlansAndTheDifferenceSaysWhichLessWhich()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        var head = Between(html, "<thead", "</thead>");
        (head.Contains("Plan 1"), head.Contains("Plan 2"), head.Contains(", Plan 2 compared with Plan 1"), head.Contains("Today")).ShouldBe((true, true, true, true));
    }

    // The sign is shown and also said in words: a screen reader reads a plus or a minus as a dash or nothing.
    [Fact]
    public async Task Compare_pocketDifference_isShownSignedAndSaidInWords()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        var row = RowOf(html, "Your pocket");
        (Regex.IsMatch(row, "<span aria-hidden=\"true\"[^>]*>\\+€2,551.13</span>"), row.Contains("€2,551.13 more"), row.Contains("€61,975.74"), row.Contains("€64,526.87"))
            .ShouldBe((true, true, true, true));
    }

    [Fact]
    public async Task Compare_lessDifference_isShownWithTheMinusSignAndSaidInWords()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        var row = RowOf(html, "Social contributions");
        (Regex.IsMatch(row, "<span aria-hidden=\"true\"[^>]*>−€4,731.68</span>"), row.Contains("€4,731.68 less")).ShouldBe((true, true));
    }

    [Fact]
    public async Task Compare_reach_isShownInPoints()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        var row = RowOf(html, "Reaches you");
        (row.Contains("62.0%"), row.Contains("64.5%"), row.Contains("+2.5 points"), row.Contains("2.5 points more")).ShouldBe((true, true, true, true));
    }

    [Fact]
    public async Task Compare_reducedRate_isKeptThenGivenUpAndChanged()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        var row = RowOf(html, "Reduced corporate rate");
        (Regex.IsMatch(row, ">Kept<"), row.Contains(">Given up<"), row.Contains(">Changed<")).ShouldBe((true, true, true));
    }

    [Fact]
    public async Task Compare_figureThatDoesNotDiffer_saysSame()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        RowOf(html, "Kept in the company").Contains(">Same</td>").ShouldBeTrue();
    }

    // The rows that differ are marked, in the row's own class, besides the signs.
    [Fact]
    public async Task Compare_rowsThatDiffer_areMarked()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        (RowOf(html, "Your pocket").Contains("compare__row--differs"), RowOf(html, "Kept in the company").Contains("compare__row--differs")).ShouldBe((true, false));
    }

    [Fact]
    public async Task Compare_leverInOnlyOnePlan_saysWhereItIsNotInThePlan()
    {
        var car = RiverInputs.Example.AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

        var html = await RenderTabAsync(Saved(RiverInputs.Example, car));

        var row = RowOf(html, "Company car costs");
        (row.Contains("Not in this plan"), row.Contains("€8,000.00"), row.Contains("€8,000.00 more")).ShouldBe((true, true, true));
    }

    // The rows and groups the plans do not have are not drawn.
    [Fact]
    public async Task Compare_noLever_drawsNoGroupForThem()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        (html.Contains("Before the split"), html.Contains("Company car costs"), html.Contains("The split")).ShouldBe((false, false, true));
    }

    // The bar is the river's only announcement: the comparison changes when the viewer picks, silently.
    [Fact]
    public async Task Compare_comparison_isNoLiveRegion()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        (html.Contains("aria-live"), html.Contains("role=\"status\""), html.Contains("role=\"alert\"")).ShouldBe((false, false, false));
    }

    [Fact]
    public async Task Compare_pocketPanel_saysTheGainAndHidesItsBarsFromAScreenReader()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        var panel = Between(html, "<section class=\"compare__panel\"", "</section>");
        (panel.Contains("« Plan 2 » leaves €2,551.13 more in your pocket than « Plan 1 »."), Regex.IsMatch(panel, "<ul[^>]*aria-hidden=\"true\"")).ShouldBe((true, true));
    }

    // The larger pocket is the gold one, the other drawn in proportion.
    [Fact]
    public async Task Compare_pocketBars_areInProportionAndTheLargerIsBetter()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        var fills = Regex.Matches(html, "<span class=\"compare__fill( compare__fill--better)?\" style=\"width: ([0-9.]+)%").Select(match => (match.Groups[1].Value, match.Groups[2].Value)).ToList();

        fills.ShouldBe([("", "96.05"), (" compare__fill--better", "100")]);
    }

    [Fact]
    public async Task Compare_samePockets_areNeitherBetter()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, RiverInputs.Example));

        (html.Contains("compare__fill--better"), html.Contains("leaves the same in your pocket as")).ShouldBe((false, true));
    }

    [Fact]
    public async Task Compare_planTheEngineRefuses_saysWhichAndDrawsNoTable()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, RiverInputs.Example.WithReserve(-1m)));

        (html.Contains("An amount of « Plan 2 » is refused"), html.Contains("<table"), html.Contains("<select")).ShouldBe((true, false, true));
    }

    [Fact]
    public async Task Compare_onePlanStartsAtThePay_saysTheyCannotBeComparedAndDrawsNoTable()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, RiverInputs.Example.SkipCompany()));

        (html.Contains("« Plan 1 » and « Plan 2 » cannot be compared"), html.Contains("<table")).ShouldBe((true, false));
    }

    [Fact]
    public async Task Compare_noToday_saysThePlansAreComparedWithEachOtherOnly()
    {
        var plans = Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m));
        plans.Delete(plans.Plans[0].Id);

        var html = await RenderTabAsync(plans);

        (html.Contains("No plan is starred as Today"), html.Contains("<table")).ShouldBe((true, true));
    }

    [Fact]
    public async Task Compare_withToday_doesNotSayNoPlanIsStarred()
    {
        var html = await RenderTabAsync(Saved(RiverInputs.Example, BestPay));

        html.Contains("No plan is starred").ShouldBeFalse();
    }

    [Fact]
    public async Task Compare_planThatCannotBeRead_isCountedAndNotListed()
    {
        var plans = Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m));
        var book = JsonNode.Parse(plans.Export())!;
        book["plans"]![2]!["inputs"]!["carLease"] = 850;
        var storage = new BrowserStorage();
        storage.Set(PlanStorage.Key, book.ToJsonString());

        var html = await RenderTabAsync(new SavedPlans(storage));

        (html.Contains("1 saved plan cannot be read by this version of Netto and is left out."), html.Contains("<option value=\"" + plans.Plans[2].Id)).ShouldBe((true, false));
    }

    private static SavedPlans Saved(params RiverInputs[] inputs)
    {
        var plans = new SavedPlans(new BrowserStorage());
        foreach (var plan in inputs)
            plans.Save(plan, number => $"Plan {number}");
        return plans;
    }

    private static async Task<string> RenderTabAsync(SavedPlans plans) =>
        WebUtility.HtmlDecode(await RenderAsync(typeof(CompareTab), new Folds(new BrowserStorage()),
            new() { ["Inputs"] = RiverInputs.Example, ["Run"] = RiverInputs.Example.Run() },
            services => services.AddSingleton(plans)));

    // Each select as its id, its label and its options with their state.
    private static List<string> Selects(string html) =>
    [
        .. Regex.Matches(html, "<select[^>]*id=\"([^\"]+)\"[^>]*>(.*?)</select>", RegexOptions.Singleline).Select(select =>
            $"{select.Groups[1].Value} | {Regex.Match(html, $"<label[^>]*for=\"{select.Groups[1].Value}\"[^>]*>([^<]*)</label>").Groups[1].Value} | " +
            string.Join("; ", Regex.Matches(select.Groups[2].Value, "<option([^>]*)>([^<]*)</option>").Select(option =>
                option.Groups[2].Value + (option.Groups[1].Value.Contains("selected") ? " selected" : "") + (option.Groups[1].Value.Contains("disabled") ? " disabled" : "")))),
    ];

    private static string Between(string html, string from, string to)
    {
        var start = html.IndexOf(from, StringComparison.Ordinal);
        return start < 0 ? "" : html[start..(html.IndexOf(to, start, StringComparison.Ordinal) + to.Length)];
    }

    // The whole row, from its tag to its end, whose heading says the label.
    private static string RowOf(string html, string label) =>
        Regex.Match(html, $"<tr[^>]*>\\s*<th scope=\"row\"[^>]*>(?:(?!</tr>).)*?{Regex.Escape(label)}.*?</tr>", RegexOptions.Singleline).Value;
}
