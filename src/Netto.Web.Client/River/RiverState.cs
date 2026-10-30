namespace Netto.Web.Client.River;

// The river's inputs while the tab is open, so going to the front page and back keeps them. The river opens on
// the river carried across a reload (Carry), once, else on the plan it was last opened from (SavedPlans), or on
// the example while none is.
internal sealed class RiverState
{
    // Under this key in the tab's own storage, the river carried across a reload.
    internal const string CarriedKey = "netto.river";

    private readonly IBrowserSession _session;

    public RiverState(SavedPlans plans, IBrowserSession session)
    {
        _session = session;
        Inputs = Carried(session.Take(CarriedKey)) ?? plans.OpenedPlan?.Inputs ?? RiverInputs.Example;
    }

    public RiverInputs Inputs { get; set; }

    // Before the page is loaded again, as a switch of language does: the river as it is, edits no plan holds
    // included, written as one plan of the saved plans' own document (PlanStorage), so it reads back exactly as a
    // saved plan does. The tab's storage keeps it until the page that loads next takes it; nothing leaves the
    // browser.
    public void Carry() =>
        _session.Set(CarriedKey, PlanStorage.Write(new PlanBook([new SavedPlan("carried", "", Inputs, PlanStorage.Store(Inputs))], null, null)));

    // What cannot be read as this version reads a plan is dropped, as an unreadable plan is never opened.
    private static RiverInputs? Carried(string? stored) =>
        stored is not null && PlanStorage.Read(stored) is ({ Plans: [{ Inputs: { } inputs }] }, false) ? inputs : null;
}
