using Netto.Core.Copyright;

namespace Netto.Web.Client.River.Copyright;

// How the copyright royalty is decided, on its own card (operator, 2026-10-28, reversing the one-place
// decision of 2026-10-23): whether it follows the pay, what an advance ruling accepts at each pay, or is a
// fixed amount, with where each comes from; the amount given for a fixed one, kept while the royalty
// follows the pay; and, for one that follows the pay, why it is none (WhyNone), as the engine says it
// (CopyrightFollowing). Set at what a ruling accepts, its figure is among the card's lines and no
// sentence says it again. The split pays the royalty beside the pay and says whichever way it is set.
internal sealed record CopyrightPolicyView(Input<bool> FollowsPay, Input<decimal> Fixed, CopyrightFollowing? Following)
{
    public static CopyrightPolicyView From(RiverInputs inputs, RiverRun run) =>
        new(inputs.Copyright.FollowsPay, inputs.Copyright.Royalty, run.Year?.Copyright.Following);

    // The text of why a royalty that follows the pay is none, none when it is set at what a ruling
    // accepts, is fixed, or there is no year to say why.
    public string? WhyNone => FollowsPay.Value && Following is { } following ? CopyrightCardText.Following(following) : null;
}
