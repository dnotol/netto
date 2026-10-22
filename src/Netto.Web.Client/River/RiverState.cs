namespace Netto.Web.Client.River;

// The river's inputs while the tab is open, so going to the front page and back keeps them. The
// river opens on the plan it was last opened from (SavedPlans), or on the example while none is.
internal sealed class RiverState(SavedPlans plans)
{
    public RiverInputs Inputs { get; set; } = plans.OpenedPlan?.Inputs ?? RiverInputs.Example;
}
