using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// The resource keys MovesCard.razor builds from the engine's moves and the card's states. The
// component and MovesCardTextTests both go through these, so a move or a state without a text in
// MovesCard.resx fails a test instead of showing on screen.
internal static class MovesCardText
{
    // What the move changes, in words: « Pay yourself {0} instead of {1} ».
    public static string Move(Move move) => move switch
    {
        PayMove => "Move_Pay",
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the card has no words for."),
    };

    // Why there are no moves, or that there are.
    public static string State(MovesState state) => $"State_{state}";
}
