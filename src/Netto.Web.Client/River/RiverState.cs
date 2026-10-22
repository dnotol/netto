namespace Netto.Web.Client.River;

// The river's inputs while the tab is open, so going to the front page and back keeps them, and the
// setup the user marked as theirs, which the bar compares the river with (BaselineGain). Nothing
// leaves the browser; saving scenarios in browser storage comes with its own story.
internal sealed class RiverState
{
    public RiverInputs Inputs { get; set; } = RiverInputs.Example;

    public SetupSnapshot? Setup { get; set; }
}

// The river as it was when the user marked it as their current setup, run once then.
internal sealed record SetupSnapshot(RiverInputs Inputs, RiverRun Run)
{
    public static SetupSnapshot Of(RiverInputs inputs) => new(inputs, inputs.Run());
}
