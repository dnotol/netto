using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;

namespace Netto.Web.Client.Tabs;

// The resource keys MapTab.razor and the map's links build, held to MapTab.resx by MapTabTextTests, so a
// module or a link without its words fails a test instead of showing its key on screen. The map words the
// conditions on the corporate rate apart from the Corporate tax card: the card states the rate's outcome
// (CorporateTaxCardText.Condition), the map says each test apart in a word or two, in the Effects list and,
// where it is lost, in a badge, never on the line of the module that sets it.
internal static class MapTabText
{
    // What Company year hands on: what the company earns before it pays its director anything, as its
    // summary says it.
    public const string ProfitBeforeLevers = "Carries_ProfitBeforeLevers";

    // What a stretch of the pocket line carries: what has reached the director so far (MapPocketLine).
    public const string PocketSoFar = "Carries_PocketSoFar";


    // A module on the map, named as its card is.
    public static string Module(RiverModule module) => $"Module_{module}";

    // A column of the map, by whose money it holds: the director's and their household's, on its way across, or
    // the company's.
    public static string Column(MapOwner owner) => $"Column_{owner}";

    // Whether a condition one module sets on another holds, in a word or two, which the Effects list says after its
    // rule (MapLink.State, MapConditions), its link carrying it: a test of the corporate rate met or lost, the
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

    // The badge a condition leaves in the box of the module it acts on (MapBadges), only for what is lost, in words
    // short enough to read whole in the box on the narrowest map, its icon beside them: the reduced rate lost, to
    // the pay, the benefits or a royalty that leaves no pay keeping it, or the amount kept the pay-out cannot cover.
    // A rule that holds, or a royalty that changes nothing, leaves none: the Effects list says them.
    public const string RateLostBadge = "Badge_RateLost";

    public const string ReserveShortBadge = "Badge_TooMuchKept";

    // What a module's tax base gains, its amount first, the one part of its words seen (MapBadgeWords), worded by
    // the module it reaches: what Corporate tax adds back, what Director pay taxes, the income imputed to the partner.
    public static string TaxBaseBadge(RiverModule module) => $"Badge_TaxBase_{module}";

    // The component whose texts word a link (MapLabel.Text): the map's own, a drain list's, or the card
    // whose figure line it is, so the map and the card say the line in the same words.
    public static Type Texts(MapText text) => text switch
    {
        MapText.Map => typeof(MapTab),
        MapText.DrainList => typeof(DrainList),
        MapText.CompanyYearCard => typeof(CompanyYearCard),
        MapText.SharedCard => typeof(SharedCard),
        MapText.CarCard => typeof(CarCard),
        MapText.VoucherCard => typeof(VoucherCard),
        MapText.AllowanceCard => typeof(AllowanceCard),
        MapText.RentCard => typeof(RentCard),
        MapText.HouseholdCard => typeof(HouseholdCard),
        _ => throw new ArgumentOutOfRangeException(nameof(text), text, "Texts the map does not word a link from."),
    };
}
