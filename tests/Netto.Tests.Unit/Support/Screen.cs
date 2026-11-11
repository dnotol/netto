using Netto.Web.Client.River;

namespace Netto.Tests.Support;

// The plan the page shows, as the viewer edits it: what a source of the plan page reads as the plan on screen
// (MovesSource, PistesSource).
internal sealed class Screen(RiverInputs plan)
{
    public RiverInputs Plan { get; set; } = plan;
}
