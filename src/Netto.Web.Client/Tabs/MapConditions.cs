using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The conditions one module sets on another, as the engine names them for a year it ran (YearConditions), as
// links of the map: each from the module that sets it to the one it acts on, dashed (MapLinkKind.Condition),
// with no amount, its line saying in a word or two whether it holds (State, which the link carries), with
// the badge it leaves on the module it acts on, and only while both its modules are on the map. The
// sentences with their figures are the cards': Corporate tax, Split and Dividend say them in their own
// texts, so the link carries no sentence: its Carries is the State words. Whether one holds is the engine's (YearCondition.Holds), carried as it is:
// nothing here tests a rule. A kind of condition the engine adds has its case here, or drawing it fails, and
// MapConditionsTests fails until it has.
internal static class MapConditions
{
    public static IReadOnlyList<MapLink> From(IReadOnlyList<YearCondition> conditions, IReadOnlyList<RiverModule> onMap) =>
        [.. conditions.Select(Link).Where(link => onMap.Contains(link.From.Module!.Value) && onMap.Contains(link.To.Module!.Value))];

    // The office allowance acts on the rent, the pay and the benefits on corporate tax's rate, and a
    // royalty and the reserve on the split's pay. Each says whether it holds in a word or two (State), and,
    // but the office rule, which always holds, marks the module it acts on with a badge (Badge) saying which
    // rule and whether it holds, lost where it does not hold and costs the plan its rate or its reserve, with
    // no effect where the royalty lowers no pay, nothing being lost by it.
    public static MapLink Link(YearCondition condition) => condition switch
    {
        NoOfficeAllowanceBesideRent => Of(condition, RiverModule.Allowances, RiverModule.Rent, MapTabText.OfficeState, null),
        PayKeepsReducedRate pay => Of(condition, RiverModule.Split, RiverModule.CorporateTax, MapTabText.RateState(pay.Holds),
            Badge(MapTabText.PayBadge(pay.Holds), pay.Holds ? MapBadgeTone.Holds : MapBadgeTone.Lost)),
        BenefitsWithinShareOfPay benefits => Of(condition, RiverModule.Car, RiverModule.CorporateTax, MapTabText.RateState(benefits.Holds),
            Badge(MapTabText.BenefitsBadge(benefits.Holds), benefits.Holds ? MapBadgeTone.Holds : MapBadgeTone.Lost)),
        RoyaltyLowersPayKeepingReducedRate royalty => Of(condition, RiverModule.Copyright, RiverModule.Split, MapTabText.RoyaltyState(royalty),
            Badge(MapTabText.RoyaltyBadge(royalty), royalty switch
            {
                { Holds: true } => MapBadgeTone.Holds,
                { With: null } => MapBadgeTone.Lost,
                _ => MapBadgeTone.NoEffect,
            })),
        PayOutCoversReserve reserve => Of(condition, RiverModule.Dividend, RiverModule.Split, MapTabText.ReserveState(reserve.Holds),
            Badge(MapTabText.ReserveBadge(reserve.Holds), reserve.Holds ? MapBadgeTone.Holds : MapBadgeTone.Lost)),
        _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, "A condition the map has no link for."),
    };

    private static MapBadge Badge(string key, MapBadgeTone tone) => new(MapLinkKind.Condition, new MapLabel(MapText.Map, key, []), tone);

    private static MapLink Of(YearCondition condition, RiverModule from, RiverModule to, string state, MapBadge? badge)
    {
        var words = new MapLabel(MapText.Map, state, []);
        return new(MapLinkKind.Condition, MapEnd.Of(from), MapEnd.Of(to), from, words, null)
        {
            Holds = condition.Holds,
            State = words,
            Badge = badge,
        };
    }
}
