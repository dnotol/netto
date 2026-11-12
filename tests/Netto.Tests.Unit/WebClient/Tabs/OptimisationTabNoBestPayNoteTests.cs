using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// No move carries a line naming the pay curve's best pay: the moves are stacked, each found on top of the ones
// above it, and the pay curve under them is headed as the plan on screen's own (PayCurveTests), so a move's
// pay and the curve's best pay are two figures for two plans and a note setting one against the other only
// made a move read as wrong. Found in the browser on 2026-11-12: the first move proposed 39,947.74 for
// +1,208.71 while the curve's best pay, for the plan as it stands, was 40,122.74.
public class OptimisationTabNoBestPayNoteTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    private static readonly RiverInputs Today = RiverInputs.Example.WithRevenue(168_000m).WithRunningCosts(20_000m);

    // The moves found for Today, the river since given a royalty whose best pay is another (51,000): the case
    // in which the tab used to name that best pay under the pay's move.
    [Fact]
    public async Task Optimisation_riverWhoseBestPayIsNotTheMoves_namesNoBestPayUnderAMove()
    {
        var river = Today.WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright);

        var html = WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = false, ["Found"] = Today.FindMoves(On22October), ["Inputs"] = river, ["Search"] = river.SearchSplit(),
        }));

        (Regex.Count(html, "<li class=\"moves__move\""), Regex.Count(html, "moves__note"), html.Contains("the best pay is"))
            .ShouldBe((MovesView.Of(Today, On22October).Moves.Count, 0, false));
    }
}
