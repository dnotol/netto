namespace Netto.Web.Client.River;

// The river's inputs while the tab is open, so going to the front page and back keeps them. Nothing
// leaves the browser; saving scenarios in browser storage comes with its own story.
internal sealed class RiverState
{
    public RiverInputs Inputs { get; set; } = RiverInputs.Example;
}
