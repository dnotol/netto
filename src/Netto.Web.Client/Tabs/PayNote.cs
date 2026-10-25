using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the Optimisation tab says under the pay's move when the river, at that pay, has a better one. The
// moves are found once from the plan they are measured from, so a fact the river holds that the plan does
// not (or a value a move sets, edited since) leaves the move's pay and the split's best pay for the river
// apart, and « In the river », which is by value, would say the river is at the best pay while the split
// card says another. The note names the split's: the search the page already runs for the river
// (RiverInputs.SearchSplit), handed to the tab, never one of its own. The search does not depend on the
// pay, so one search serves the river at the move's pay too (RiverInputs.SameBesidesPay).
internal static class PayNote
{
    // The river's own best pay, for the line of the move: none unless the move is the pay, the page found
    // a best pay for the river, and the river at the move's pay is not at it, that is, the split card
    // would offer its button there (BestPayView.IsYours), whether or not the river is at that pay yet. A
    // pay within a euro of the best is the best; a river refused at the pay, one that keeps no reserve at
    // any pay or one that skips the company's year has no best pay to name.
    public static decimal? RiversBestPay(Move move, RiverInputs river, SplitSearchResult? search)
    {
        if (move is not PayMove pay || search is null)
            return null;

        var atPay = river.WithDirectorPay(pay.Pay);
        return SplitSearchView.From(atPay, atPay.Run(), search)?.Best is { IsYours: false } best ? best.Level.Pay : null;
    }
}
