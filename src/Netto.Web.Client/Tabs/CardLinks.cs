namespace Netto.Web.Client.Tabs;

// The Plan tab's address with a card to open on arrival, plan?card=<its name>: a module's card by its fold's
// name, a bucket's by its key (MapChoice.Name, MapChoice.Named): the page chooses it on the map, its card
// beside it (Plan). A real link, so Back and a new browser tab work.
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
