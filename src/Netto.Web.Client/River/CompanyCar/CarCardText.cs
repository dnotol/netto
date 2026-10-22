namespace Netto.Web.Client.River.CompanyCar;

// The resource keys the car card builds from values, gone through by the card and its tests alike.
internal static class CarCardText
{
    // What the car takes in cash against what it puts in the muddy pocket: the muddy pocket holds more
    // than it costs, or less, or it costs no cash at all.
    public static string Gap(CarComparison comparison) => comparison switch
    {
        { CashCost: <= 0m } => "Gap_NoCash",
        { Gap: >= 0m } => "Gap_More",
        _ => "Gap_Less",
    };
}
