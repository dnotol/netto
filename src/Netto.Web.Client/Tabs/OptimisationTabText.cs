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
    // The three figures the tab opens on: what the plan the moves are from leaves in the pocket, what it
    // leaves with every move, and the difference.
    public static string FromInPocket(MovesBaseline from) => $"Figure_Today{Plan(from)}";

    public const string PlanInPocket = "Figure_Plan";

    public static string MoreThanFrom(MovesBaseline from) => $"Figure_Gain{Plan(from)}";

    // What the pocket holds once a move is taken, with the moves above it: « {0} in your pocket after it ».
    public const string After = "After";

    // That the river holds what the plan the moves are still from does not (MovesView.RiverChanged): from
    // the plan in the river, any change since it was opened or saved, one text; from Today, the cards whose
    // facts differ, a text for each number of names it holds (« in {0} », « in {0} and {1} », « in {0}, {1}
    // and {2} », « in {0}, {1} and {2} more »), since no word joining them is typed here. At least one
    // difference, or there is no note to word.
    public static string RiverChanged(MovesBaseline from, int differences) => from.IsToday ? $"RiverChanged_{Names(differences)}" : "RiverChanged_Plan";

    private static string Names(int differences) => differences switch
    {
        < 1 => throw new ArgumentOutOfRangeException(nameof(differences), differences, "A note on a river that holds what Today does not names a card."),
        1 => "One",
        2 => "Two",
        3 => "Three",
        _ => "More",
    };

    // The differences the note names: all of them up to three, else the first two, the text counting the rest.
    public static IReadOnlyList<RiverDifference> Shown(IReadOnlyList<RiverDifference> differences) =>
        differences.Count <= MostNamed ? differences : [.. differences.Take(MostNamed - 1)];

    private const int MostNamed = 3;

    // What fills the note: the names it shows, each worded (Module, Difference), and with « and {2} more »
    // how many differences it leaves unnamed.
    public static object[] RiverChangedArguments(IReadOnlyList<string> names, int differences) =>
        differences <= MostNamed ? [.. names] : [.. names, differences - names.Count];

    // A card's name in the note, the title it draws: « Company year », « Copyright royalty ».
    public static string Module(RiverModule module) => $"Module_{module}";

    // A card's name with whether the river or Today holds the lever the other does not: « {0} »,
    // « {0} (not in Today) », « {0} (not in your river) ».
    public static string Difference(RiverDifferenceKind kind) => $"Difference_{kind}";

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

    // Why there are no moves, or that there are, from the plan they are measured from; the line opening the
    // moves names it, « From « {0} », as you opened or saved it: », and so does the line it replaces while
    // the page looks for them: « Looking for the moves from « {0} », as you opened or saved it… ».
    public static string State(MovesState state, MovesBaseline from) =>
        $"State_{state}{(state is MovesState.Moves or MovesState.Searching ? Named(from) : Plan(from))}";

    // One sentence of why a move helps (MoveReasons): « It cancels the surcharge of {0} on your income tax. »
    public static string Why(MoveReasonKind kind) => $"Why_{kind}";

    // Where a move links to, by the card's name or the tab's: « See it on The split ».
    public static string Link(MoveLink link) => link.Card is { } card ? $"Link_{card}" : $"Link_{link.Tab}";

    // The link's name for a screen reader, its text then the move it is about: « See it on The split: {move} ».
    public const string LinkLabel = "LinkLabel";

    // Today's texts are the plain keys; the plan in the river's add « _Plan ».
    private static string Plan(MovesBaseline from) => from.IsToday ? "" : "_Plan";

    // And « _PlanNamed » where a text names the plan in the river and it has a name.
    private static string Named(MovesBaseline from) => from is { IsToday: false, Name: not null } ? "_PlanNamed" : Plan(from);
}
