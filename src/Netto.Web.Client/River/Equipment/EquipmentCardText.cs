using Netto.Core.Equipment;

namespace Netto.Web.Client.River.Equipment;

// The resource keys the IT and phone card builds from values, gone through by the card and its tests alike.
internal static class EquipmentCardText
{
    // The set against the same things bought privately: what it gains, what it costs more, the same, or the
    // comparison waits for what those things would cost, while it is not given or is refused.
    public static string Private(EquipmentComparison comparison) => comparison.AgainstPrivate switch
    {
        null => "Private_Waiting",
        > 0m => "Private_Gain",
        < 0m => "Private_Loss",
        _ => "Private_Tie",
    };

    // The summary says what the set adds to the pocket or takes from it, the plan with it less the plan
    // without (LeverGain); a card with no piece yet says so alone.
    public static string Summary(bool hasSet, decimal gain) => hasSet ? LeverGainText.Summary(gain) : "SummaryNoSet";

    // A kind of piece's line, its count and one piece's value, for the whole year or for the days provided.
    public static string Piece(EquipmentKind kind, bool allYear) => allYear ? $"Figure_Piece_{kind}" : $"Figure_Piece_{kind}_Days";
}
