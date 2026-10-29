using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;

namespace Netto.Web.Client.Tabs;

// The resource keys MapTab.razor and the map's links build, held to MapTab.resx by MapTabTextTests, so a
// module or a link without its words fails a test instead of showing its key on screen. The map words the
// conditions on the corporate rate apart from the Corporate tax card: the card states the rate's outcome
// (CorporateTaxCardText.Condition), the map says each test apart in a word or two, on the link from the
// module that sets it.
internal static class MapTabText
{
    // What Company year hands on: what the company earns before it pays its director anything, as its
    // summary says it.
    public const string ProfitBeforeLevers = "Carries_ProfitBeforeLevers";

    // A module on the map, named as its card is.
    public static string Module(RiverModule module) => $"Module_{module}";

    // Whether a condition one module sets on another holds, in a word or two, which its line says on the map
    // (MapLink.State, MapConditions) and its link carries: a test of the corporate rate met or lost, the
    // office kept, what a royalty does to the pay keeping the rate (lowers it, has no effect on it, or leaves
    // no pay keeping it), the reserve covered or not. The sentences with their figures are the cards': Corporate
    // tax, Split and Dividend say them in their own texts.
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
        MapText.CompanyYearCard => typeof(CompanyYearCard),
        MapText.SplitCard => typeof(SplitCard),
        MapText.CarCard => typeof(CarCard),
        MapText.VoucherCard => typeof(VoucherCard),
        MapText.AllowanceCard => typeof(AllowanceCard),
        MapText.RentCard => typeof(RentCard),
        MapText.HouseholdCard => typeof(HouseholdCard),
        _ => throw new ArgumentOutOfRangeException(nameof(text), text, "Texts the map does not word a link from."),
    };
}
