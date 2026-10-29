using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Core.SocialContributions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab drawn without a browser, as a viewer meets it (RiverRendering): on a river's own run, with
// a module chosen or none, and any service a test adds (more), registered after the render's own so its
// own wins: the viewer's filter of the kinds of link, which shows every kind unless a test gives another,
// the map's first visit, with its rules' lines left out, being the tests of that first visit's own.
// Its HTML decoded to the text a viewer reads. And what the tests holding the map to the cards share: the
// rivers they draw by name, and the amounts a card draws in sight.
internal static class MapRendering
{
    // Every lever in the plan, none with an amount: each moves no money.
    public static readonly RiverInputs EveryLeverEmpty = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module));

    // The rivers the map's tests draw, by name.
    public static RiverInputs River(string name) => name switch
    {
        "every lever" => MapLinksTests.Full,
        "the company paying the contributions" => MapLinksTests.Full.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.035m),
        "the example" => RiverInputs.Example,
        "the company skipped" => MapLinksTests.Full.SkipCompany(),
        "every lever empty" => EveryLeverEmpty,
        "a rent under its floor" => MapLinksTests.Full.WithRent(1m),
        "a car without contribution" => MapLinksTests.Full.WithCarContribution(0m),
        "a contribution beyond the car's costs" => MapLinksTests.Full.WithCarContribution(12_000m),
        "the dividend all kept" => MapLinksTests.Full.WithReserve(1_000_000m),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    // The amounts a card draws in sight, in a page drawn with every card open (CardHtml), without their sign:
    // its lines' and its summary's.
    public static HashSet<string> Drawn(string html, RiverModule module)
    {
        var card = CardHtml.Card(html, MapModules.Name(module));
        return [.. CardHtml.LineAmounts(card).Concat(CardHtml.Amounts(CardHtml.Summary(card) ?? "")).Select(Unsigned)];
    }

    // An amount without the sign a drain list draws (−, +), which says which way the money goes, as the map's
    // arrow or a gate's words say instead.
    public static string Unsigned(string amount) => amount.TrimStart('−', '+', '-');

    // A browser's storage in which the viewer chose to see every kind of link (the key present and empty):
    // what a test that holds a line, its arrowhead or its words to the map starts from.
    public static BrowserStorage EveryKindShown()
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, "");
        return storage;
    }

    public static async Task<string> RenderTabAsync(RiverInputs river, RiverModule? chosen = null, Action<IServiceCollection>? more = null) =>
        WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(MapTab), new Folds(new BrowserStorage()), new()
        {
            [nameof(MapTab.Inputs)] = river,
            [nameof(MapTab.Run)] = river.Run(),
            [nameof(MapTab.Chosen)] = chosen,
        }, services =>
        {
            services.AddSingleton(new MapLinkFilter(EveryKindShown()));
            more?.Invoke(services);
        }));

    // The inner HTML of each kind's item of the legend in a rendered page, in the order drawn: its box and the
    // bubble beside it.
    public static List<string> LegendItems(string html) =>
        [.. Regex.Matches(html, "<li class=\"map-filter__kind\"[^>]*>(.*?)</li>", RegexOptions.Singleline).Select(match => match.Groups[1].Value)];

    // Each box of the legend: the kind it is, whether it is ticked, and its label as a screen reader reads
    // it, what the eye alone reads of it (the count's number) left out.
    public static List<(MapLinkKind Kind, bool Ticked, string Text)> LegendBoxes(string html) =>
    [
        .. LegendItems(html).Select(item => (
            Enum.Parse<MapLinkKind>(Regex.Match(item, "data-link-kind=\"(\\w+)\"").Groups[1].Value),
            Regex.IsMatch(item, "<input [^>]*type=\"checkbox\"[^>]*\\schecked"),
            Regex.Replace(Regex.Replace(Regex.Replace(Regex.Match(item, "<label.*?</label>", RegexOptions.Singleline).Value,
                "<span [^>]*aria-hidden=\"true\"[^>]*>[^<]*</span>", ""), "<[^>]*>", " "), "\\s+", " ").Trim())),
    ];
}
