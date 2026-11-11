using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// Where a move of the Optimisation tab links to: the card of My plan it acts on, while that card is on the
// map, or the plan by date. The pay is on the Director pay card while the river holds the company's year, a
// lever's card while its module is in the plan, so a lever's move not applied yet links nowhere until
// « Apply » adds its module; a payment's place is My plan by date.
internal static class MoveLinks
{
    public static MoveLink? To(Move move, RiverInputs inputs) => move switch
    {
        PayMove => inputs.Company is null ? null : new MoveLink(PlanTab.Plan, MapModules.Name(RiverModule.DirectorPay)),
        CompanyAdvancePaymentMove or DirectorAdvancePaymentMove => new MoveLink(PlanTab.Plan, null, PlanLens.ByDate),
        EcoVouchersMove or SportCultureVouchersMove => Lever(OptionalModule.Vouchers, inputs),
        RentMove => Lever(OptionalModule.Rent, inputs),
        CopyrightFollowsPayMove => Lever(OptionalModule.Copyright, inputs),
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the tab has no place to link to."),
    };

    private static MoveLink? Lever(OptionalModule module, RiverInputs inputs) =>
        inputs.InPlan(module) ? new MoveLink(PlanTab.Plan, ModuleFolds.Name(module)) : null;
}

// A tab, and on My plan the card to open on arrival (its fold's name), none for the tab alone, or the lens
// to read it through.
internal sealed record MoveLink(PlanTab Tab, string? Card, PlanLens Lens = PlanLens.ByBucket)
{
    public string Href => Card is { } card ? CardLinks.Href(card) : PlanTabRoutes.Href(Tab, Lens);
}
