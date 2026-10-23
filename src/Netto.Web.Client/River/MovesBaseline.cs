namespace Netto.Web.Client.River;

// The plan the moves are measured from: the plan starred « Today », the user's current setup, while a
// readable one is, or else the plan in the river as it was opened or saved, under its name while it is
// a saved plan as it was saved. Public because the Optimisation tab, a component, takes it as a parameter.
public sealed record MovesBaseline(RiverInputs Plan, bool IsToday, string? Name = null)
{
    public static MovesBaseline Today(RiverInputs plan) => new(plan, IsToday: true);
}
