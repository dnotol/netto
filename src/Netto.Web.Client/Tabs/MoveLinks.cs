using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// Where a move of the Optimisation tab links to: the card of the Plan tab it acts on, while that card is
// on the tab, or another tab. The split is on the Plan tab while the river holds the company's year, a
// lever's card while its module is in the plan, so a lever's move not applied yet links nowhere until
// « Apply » adds its module; a payment's place is When to pay.
internal static class MoveLinks
{
    // The split's card, by the name its fold is remembered under (SplitCard, OutlineRows.Order).
    private const string Split = "split";

    public static MoveLink? To(Move move, RiverInputs inputs) => move switch
    {
        PayMove => inputs.Company is null ? null : new MoveLink(PlanTab.Plan, Split),
        CompanyAdvancePaymentMove or DirectorAdvancePaymentMove => new MoveLink(PlanTab.WhenToPay, null),
        EcoVouchersMove or SportCultureVouchersMove => Lever(OptionalModule.Vouchers, inputs),
        RentMove => Lever(OptionalModule.Rent, inputs),
        CopyrightFollowsPayMove => Lever(OptionalModule.Copyright, inputs),
        _ => null,
    };

    private static MoveLink? Lever(OptionalModule module, RiverInputs inputs) =>
        inputs.InPlan(module) ? new MoveLink(PlanTab.Plan, ModuleFolds.Name(module)) : null;
}

// A tab, and on the Plan tab the card to open on arrival (its fold's name), none for the tab alone.
internal sealed record MoveLink(PlanTab Tab, string? Card)
{
    public string Href => Card is { } card ? CardLinks.Href(card) : PlanTabRoutes.Href(Tab);
}

// The Plan tab's address with a card to open on arrival, plan?card=<its fold's name>: the page opens that
// card as the viewer's fold and brings it under the bar of buckets, as the river's outline does (Plan). A
// real link, so Back and a new browser tab work.
internal static class CardLinks
{
    // The query parameter that names the card.
    public const string Query = "card";

    public static string Href(string card) => $"{PlanTabRoutes.Href(PlanTab.Plan)}?{Query}={Uri.EscapeDataString(card)}";
}
