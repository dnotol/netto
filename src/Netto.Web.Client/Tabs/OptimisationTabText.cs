using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys OptimisationTab.razor builds from the engine's moves and its states. The component
// and OptimisationTabTextTests both go through these, so a move or a state without a text in
// OptimisationTab.resx fails a test instead of showing on screen.
internal static class OptimisationTabText
{
    // What the move changes, in words: « Pay yourself {0} instead of {1} ».
    public static string Move(Move move) => move switch
    {
        PayMove => "Move_Pay",
        CompanyAdvancePaymentMove => "Move_CompanyPayment",
        DirectorAdvancePaymentMove => "Move_DirectorPayment",
        EcoVouchersMove => "Move_EcoVouchers",
        SportCultureVouchersMove => "Move_SportCultureVouchers",
        RentMove => "Move_Rent",
        CopyrightFollowsPayMove => "Move_CopyrightFollowsPay",
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the card has no words for."),
    };

    // Why there are no moves, or that there are.
    public static string State(MovesState state) => $"State_{state}";

    // One sentence of why a move helps (MoveReasons): « It cancels the surcharge of {0} on your income tax. »
    public static string Why(MoveReasonKind kind) => $"Why_{kind}";

    // Where a move links to, by the card's name or the tab's: « See it on The split ».
    public static string Link(MoveLink link) => link.Card is { } card ? $"Link_{card}" : $"Link_{link.Tab}";

    // The link's name for a screen reader, its text then the move it is about: « See it on The split: {move} ».
    public const string LinkLabel = "LinkLabel";
}
