namespace Netto.Web.Client.Tabs;

// The Plan tab's address with a card to open on arrival, plan?card=<its fold's name>: the page opens that
// card as the viewer's fold and brings it under the bar of buckets, its heading focused, as choosing its
// row in the river's outline does (Plan). A real link, so Back and a new browser tab work.
internal static class CardLinks
{
    // The query parameter that names the card.
    public const string Query = "card";

    public static string Href(string card) => $"{PlanTabRoutes.Href(PlanTab.Plan)}?{Query}={Uri.EscapeDataString(card)}";
}

// Which card a link arrives at, asked on every set of the page's parameters: the card the Plan tab's
// address names, once per arrival. The same address given again, as Blazor may while the page stays
// there, arrives nowhere more, so the card stays where the viewer left it; the same link followed again
// after the address named another tab, or no card, is a new arrival.
internal sealed class CardArrival
{
    // The card the address named when last asked, none on another tab or without a card.
    private string? _named;

    public string? Take(PlanTab tab, string? card)
    {
        var named = tab == PlanTab.Plan ? card : null;
        var arriving = named is not null && named != _named ? named : null;
        _named = named;

        return arriving;
    }
}
