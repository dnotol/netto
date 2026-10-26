using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;

namespace Netto.Web.Client.Tabs;

// The resource keys MapTab.razor and the map's links build, held to MapTab.resx by MapTabTextTests, so a
// module or a link without its words fails a test instead of showing its key on screen. The map words the
// conditions on the corporate rate apart from the Corporate tax card: the card states the rate's outcome
// (CorporateTaxCardText.Condition), the map says each test apart, with its own figures, on the link from
// the module that sets it.
internal static class MapTabText
{
    // What Company year hands on: what the company earns before it pays its director anything, as its
    // summary says it.
    public const string ProfitBeforeLevers = "Carries_ProfitBeforeLevers";

    // A link in words, in the list of the chosen module's: what it carries, its amount, where from and to;
    // a condition, which names its figures in its words, without an amount.
    public const string Link = "Link";

    public const string LinkWithoutAmount = "LinkWithoutAmount";

    // Where a cost of the company leaves the river, as a link's end.
    public const string OutOfRiver = "End_out";

    // The list's heading: the chosen module's links, or what the list is while none is chosen.
    public const string LinksOf = "LinksOf";

    public const string LinksOfNone = "LinksOfNone";

    // What the list says of the chosen module while the engine refuses an amount: there is no figure.
    public const string NoFigures = "NoFigures";

    // What the list says of the chosen module when the kinds the viewer turned off (MapLinkFilter) hide some
    // of its links: how many, so a short list is never read as a module with little to say. Said only for
    // at least one.
    public static string Hidden(int count) => count switch
    {
        < 1 => throw new ArgumentOutOfRangeException(nameof(count), count, "A line of hidden links is for at least one."),
        1 => "Hidden_One",
        _ => "Hidden_Many",
    };

    // A module on the map, named as its card is.
    public static string Module(RiverModule module) => $"Module_{module}";

    // The words of a condition one module sets on another (MapConditions), by its kind and whether it
    // holds: a test of the corporate rate that holds says its condition is met, never that the rate is
    // kept, which the other test may still lose, and one that fails that the rate is lost; the reserve
    // covered or not; what a royalty does to the pay that keeps the rate. The pay's by what art. 215 found
    // of it.
    public static string PayCondition(ReducedRateCondition test) => $"Condition_Pay_{test}";

    public static string BenefitsCondition(bool holds) => holds ? "Condition_Benefits_Met" : "Condition_Benefits_Lost";

    // Said of the one of the two the plan pays; a year never runs with both.
    public static string OfficeCondition(bool rentPaid) => rentPaid ? "Condition_Office_Rent" : "Condition_Office_Allowance";

    // Where it holds, the pay it lowers it to, beside the one without it, or none without it; where it does
    // not, the pay keeping the rate either way, or none.
    public static string RoyaltyCondition(RoyaltyLowersPayKeepingReducedRate royalty) => royalty switch
    {
        { Holds: true, Without: null } => "Condition_Royalty_LowersFromNone",
        { Holds: true } => "Condition_Royalty_Lowers",
        { With: null } => "Condition_Royalty_NoPayKeeps",
        _ => "Condition_Royalty_DoesNotLower",
    };

    public static string ReserveCondition(bool holds) => holds ? "Condition_Reserve_Covered" : "Condition_Reserve_NotCovered";

    // Whether a condition holds in a word or two, which its line says on the map (MapLink.State), its
    // sentence above being said where the chosen module's links are listed: a test of the corporate rate
    // met or lost, the office kept, what a royalty does to the pay keeping the rate (lowers it, has no
    // effect on it, or leaves no pay keeping it), the reserve covered or not.
    public static string RateState(bool holds) => holds ? "State_Met" : "State_Lost";

    public const string OfficeState = "State_Kept";

    public static string RoyaltyState(RoyaltyLowersPayKeepingReducedRate royalty) => royalty switch
    {
        { Holds: true } => "State_Lowers",
        { With: null } => "State_NoPayKeeps",
        _ => "State_NoEffect",
    };

    public static string ReserveState(bool holds) => holds ? "State_Covered" : "State_NotCovered";

    // The badge a condition leaves in the box of the module it acts on (MapBadges): which rule and whether
    // it holds, in a few words with its line's state, so two rules on one module are told apart: the pay
    // met or the rate lost by it, the benefits likewise, what a royalty does to the pay keeping the rate,
    // the reserve covered or not. The office rule, which always holds, leaves none.
    public static string PayBadge(bool holds) => holds ? "Badge_Pay_Met" : "Badge_Pay_Lost";

    public static string BenefitsBadge(bool holds) => holds ? "Badge_Benefits_Met" : "Badge_Benefits_Lost";

    public static string RoyaltyBadge(RoyaltyLowersPayKeepingReducedRate royalty) => royalty switch
    {
        { Holds: true } => "Badge_Royalty_Lowers",
        { With: null } => "Badge_Royalty_NoPayKeeps",
        _ => "Badge_Royalty_NoEffect",
    };

    public static string ReserveBadge(bool holds) => holds ? "Badge_Reserve_Covered" : "Badge_Reserve_NotCovered";

    // What a module's tax base gains, its amount first.
    public const string TaxBaseBadge = "Badge_TaxBase";

    // The component whose texts word a link (MapLabel.Text): the map's own, a drain list's, or the card
    // whose figure line it is, so the map and the card say the line in the same words.
    public static Type Texts(MapText text) => text switch
    {
        MapText.Map => typeof(MapTab),
        MapText.DrainList => typeof(DrainList),
        MapText.SplitCard => typeof(SplitCard),
        MapText.CarCard => typeof(CarCard),
        MapText.VoucherCard => typeof(VoucherCard),
        _ => throw new ArgumentOutOfRangeException(nameof(text), text, "Texts the map does not word a link from."),
    };
}
