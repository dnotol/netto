using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The conditions one module sets on another, as the engine names them for a year it ran (YearConditions), as
// links of the map: each from the module that sets it to the one it acts on, dashed (MapLinkKind.Condition),
// with no amount and no word on its line, the Effects list saying its rule and whether it holds (State, which the
// link carries), with the badge it leaves on the module it acts on where it is lost, and only while both its
// modules are on the map. The sentences with their figures are the cards': Corporate tax, Copyright and
// Dividend say them in their own texts, so the link carries no sentence: its Carries is the State words. Whether one holds is the engine's (YearCondition.Holds), carried as it is:
// nothing here tests a rule. A kind of condition the engine adds has its case here, or drawing it fails, and
// MapConditionsTests fails until it has.
internal static class MapConditions
{
    // The conditions of the year the engine ran, the benefits' share of the pay drawn from the module that carries
    // the benefit (BenefitsFrom).
    public static IReadOnlyList<MapLink> Of(YearResult year, IReadOnlyList<RiverModule> onMap) =>
        From(YearConditions.Of(year), onMap, BenefitsFrom(year, onMap));

    public static IReadOnlyList<MapLink> From(IReadOnlyList<YearCondition> conditions, IReadOnlyList<RiverModule> onMap, RiverModule benefits = RiverModule.Car) =>
        [.. conditions.Select(condition => Link(condition, benefits)).Where(link => onMap.Contains(link.From.Module!.Value) && onMap.Contains(link.To.Module!.Value))];

    // The office allowance acts on the rent, the pay and the benefits on corporate tax's rate, and a royalty and the
    // reserve on the pay the node « To share » gives. Each says whether it holds in a word or two (State), for the
    // Effects list, and marks the module it acts on with a badge (Badge) only where it costs the plan its rate or
    // its reserve: the office rule, a rule that holds and a royalty that lowers the pay keeping the rate, or no pay
    // at all, leave none.
    // The benefits' share of the pay is set by the flat-rate benefit the map draws it from (benefits): the car's
    // while the car carries one, else the IT and phone's (BenefitsFrom).
    public static MapLink Link(YearCondition condition, RiverModule benefits = RiverModule.Car) => condition switch
    {
        NoOfficeAllowanceBesideRent => Of(condition, RiverModule.Allowances, RiverModule.Rent, MapTabText.OfficeState, null),
        NoAllowanceBesideEquipment => Of(condition, RiverModule.Allowances, RiverModule.Equipment, MapTabText.OfficeState, null),
        PayKeepsReducedRate pay => Of(condition, RiverModule.Split, RiverModule.CorporateTax, MapTabText.RateState(pay.Holds), pay.Holds ? null : RateLost),
        BenefitsWithinShareOfPay within => Of(condition, benefits, RiverModule.CorporateTax, MapTabText.RateState(within.Holds), within.Holds ? null : RateLost),
        RoyaltyLowersPayKeepingReducedRate royalty => Of(condition, RiverModule.Copyright, RiverModule.Split, MapTabText.RoyaltyState(royalty), royalty is { Holds: false, With: null } ? RateLost : null),
        PayOutCoversReserve reserve => Of(condition, RiverModule.Dividend, RiverModule.Split, MapTabText.ReserveState(reserve.Holds), reserve.Holds ? null : Badge(MapTabText.ReserveShortBadge)),
        _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, "A condition the map has no link for."),
    };

    // The module whose benefit the share of the pay is drawn from: the car while it is on the map and carries a
    // benefit taxed as pay, else the IT and phone while it is on the map, else the car. A selection of the run's
    // figures, no rule.
    public static RiverModule BenefitsFrom(YearResult year, IReadOnlyList<RiverModule> onMap) =>
        onMap.Contains(RiverModule.Equipment) && year.Equipment.Benefit > 0m && !(onMap.Contains(RiverModule.Car) && year.Car.Benefit > 0m)
            ? RiverModule.Equipment
            : RiverModule.Car;

    private static readonly MapBadge RateLost = Badge(MapTabText.RateLostBadge);

    private static MapBadge Badge(string key) => new(MapLinkKind.Condition, new MapLabel(MapText.Map, key, []), MapBadgeTone.Lost);

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
