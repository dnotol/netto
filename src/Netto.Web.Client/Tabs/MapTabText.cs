using Netto.Web.Client.River;

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

    // A module on the map, named as its card is.
    public static string Module(RiverModule module) => $"Module_{module}";
}
