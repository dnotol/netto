using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;

namespace Netto.Web.Client.Tabs;

// The resource keys MapTab.razor builds, held to MapTab.resx by MapTabTextTests, so a module or a link
// without its words fails a test instead of showing its key on screen.
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

    // A module on the map, named as its card is.
    public static string Module(RiverModule module) => $"Module_{module}";

    // The words of a condition one module sets on another (MapConditions), by its kind and whether it
    // holds: « kept » or « lost » for the corporate rate and the reserve, what a royalty does to the pay
    // that keeps the rate. The pay's by what art. 215 found of it.
    public static string PayCondition(ReducedRateCondition test) => $"Condition_Pay_{test}";

    public static string BenefitsCondition(bool holds) => holds ? "Condition_Benefits_Kept" : "Condition_Benefits_Lost";

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

    public static string ReserveCondition(bool holds) => holds ? "Condition_Reserve_Kept" : "Condition_Reserve_Lost";

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
