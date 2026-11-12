using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Core.SocialContributions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab drawn without a browser, as a viewer meets it (RiverRendering): on a river's own run, with
// a module chosen or none, and any service a test adds (more). Its HTML decoded to the text a viewer reads. And what the tests holding the map to the cards share: the
// rivers they draw by name, and the amounts a card draws in sight.
internal static class MapRendering
{
    // Every lever in the plan, none with an amount: each moves no money.
    public static readonly RiverInputs EveryLeverEmpty = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module)).WithPartner(true);

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

    // The choice of a module or none, as a theory names it (MapChoice holds in no attribute).
    public static MapChoice? Chosen(RiverModule? module) => module is { } chosen ? MapChoice.Of(chosen) : null;

    public static async Task<string> RenderTabAsync(RiverInputs river, MapChoice? chosen = null, Action<IServiceCollection>? more = null) =>
        WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(MapTab), new Folds(new BrowserStorage()), new()
        {
            [nameof(MapTab.Inputs)] = river,
            [nameof(MapTab.Run)] = river.Run(),
            [nameof(MapTab.Chosen)] = chosen,
        }, services => more?.Invoke(services)));
}
