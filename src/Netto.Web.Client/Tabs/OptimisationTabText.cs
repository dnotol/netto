using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys OptimisationTab.razor builds from the engine's moves and its states. The component and
// OptimisationTabTextTests both go through these, so a move or a state without a text in
// OptimisationTab.resx fails a test instead of showing on screen.
internal static class OptimisationTabText
{
    // The three figures the tab opens on: what the plan on screen leaves in the pocket, what it leaves
    // with every move, and the difference.
    public const string FromInPocket = "Figure_From";

    public const string PlanInPocket = "Figure_Plan";

    public const string MoreThanFrom = "Figure_Gain";

    // What the pocket holds once a move is taken, with the moves above it: « {0} in your pocket after it ».
    public const string After = "After";

    // The one action applying every move.
    public const string ApplyAll = "ApplyAll";

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

    // What fills the {0} and {1} of the move's sentence (Move), formatted as the page reads them: the pay
    // and the pay it is from, a payment's amount and day, a voucher's or the rent's amount, the creative
    // share the royalty follows the pay at.
    public static object[] MoveArguments(Move move, decimal fromPay, decimal creativeShare) => move switch
    {
        PayMove pay => [Formats.Euros(pay.Pay), Formats.Euros(fromPay)],
        CompanyAdvancePaymentMove payment => [Formats.Euros(payment.Amount), Formats.Day(payment.Date)],
        DirectorAdvancePaymentMove payment => [Formats.Euros(payment.Amount), Formats.Day(payment.Date)],
        EcoVouchersMove eco => [Formats.Euros(eco.Amount)],
        SportCultureVouchersMove sport => [Formats.Euros(sport.Amount)],
        RentMove rent => [Formats.Euros(rent.Rent)],
        CopyrightFollowsPayMove => [Formats.Percentage(creativeShare)],
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the card has no words for."),
    };

    // Why there are no moves, that there are, or that they are being looked for, from the plan on screen.
    public static string State(MovesState state) => $"State_{state}";

    // One sentence of why a move helps (MoveReasons): « It cancels the surcharge of {0} on your income tax. »
    public static string Why(MoveReasonKind kind) => $"Why_{kind}";

    // Where a move links to, by the card's name or the tab's: « See it on The split ».
    public static string Link(MoveLink link) => link.Card is { } card ? $"Link_{card}" : $"Link_{link.Lens}";

    // The link's name for a screen reader, its text then the move it is about: « See it on The split: {move} ».
    public const string LinkLabel = "LinkLabel";
}
