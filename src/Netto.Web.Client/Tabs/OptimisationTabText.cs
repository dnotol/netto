using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys OptimisationTab.razor builds from the engine's moves, its states and the plan the moves
// are measured from (MovesBaseline). The component and OptimisationTabTextTests both go through these, so a
// move or a state without a text in OptimisationTab.resx fails a test instead of showing on screen. The
// texts about the plan starred Today have keys of their own; the plan in the river's add « _Plan », and
// « _PlanNamed » where they name it, their {0} being its name.
internal static class OptimisationTabText
{
    // What the tab is measured against, above its title: « Against « {0} », the plan in your river ».
    public static string Eyebrow(MovesBaseline? from) => $"Eyebrow{Named(from)}";

    // What the tab holds, under its title.
    public static string Lede(MovesBaseline? from) => $"Lede{Plan(from)}";

    // The three figures the tab opens on: what the plan the moves are from leaves in the pocket, what it
    // leaves with every move, and the difference.
    public static string FromInPocket(MovesBaseline? from) => $"Figure_Today{Plan(from)}";

    public const string PlanInPocket = "Figure_Plan";

    public static string MoreThanFrom(MovesBaseline? from) => $"Figure_Gain{Plan(from)}";

    // What the pocket holds once a move is taken, with the moves above it: « {0} in your pocket after it ».
    public const string After = "After";

    // What the moves add together: « Together: {0} more than this plan. »
    public static string Total(MovesBaseline? from) => $"Total{Plan(from)}";

    // That the river changed since the plan in it was opened or saved, which the moves are still from.
    public const string RiverChanged = "RiverChanged";

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

    // Why there are no moves, or that there are, from the plan they are measured from; the line opening the
    // moves names it: « From « {0} », as you opened or saved it: ».
    public static string State(MovesState state, MovesBaseline? from) => $"State_{state}{(state == MovesState.Moves ? Named(from) : Plan(from))}";

    // One sentence of why a move helps (MoveReasons): « It cancels the surcharge of {0} on your income tax. »
    public static string Why(MoveReasonKind kind) => $"Why_{kind}";

    // Where a move links to, by the card's name or the tab's: « See it on The split ».
    public static string Link(MoveLink link) => link.Card is { } card ? $"Link_{card}" : $"Link_{link.Tab}";

    // The link's name for a screen reader, its text then the move it is about: « See it on The split: {move} ».
    public const string LinkLabel = "LinkLabel";

    // Today's texts are the plain keys; the plan in the river's add « _Plan ».
    private static string Plan(MovesBaseline? from) => from is { IsToday: false } ? "_Plan" : "";

    // And « _PlanNamed » where a text names the plan in the river and it has a name.
    private static string Named(MovesBaseline? from) => from is { IsToday: false, Name: not null } ? "_PlanNamed" : Plan(from);
}
