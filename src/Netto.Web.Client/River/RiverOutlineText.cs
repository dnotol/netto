namespace Netto.Web.Client.River;

// The resource keys RiverOutline.razor builds, held to RiverOutline.resx by RiverOutlineTextTests.
internal static class RiverOutlineText
{
    // The outline's title, which names its landmark.
    public const string Label = "Label";

    // The toggle in the outline's head: what it will do, « Open all » while a card is folded, « Fold
    // all » once all are open.
    public static string All(bool allOpen) => allOpen ? "FoldAll" : "OpenAll";

    // The same, as a screen reader hears it: which cards.
    public static string AllName(bool allOpen) => allOpen ? "FoldAllName" : "OpenAllName";

    // A card's glance, one figure of its summary in the summary's words.
    public static string Glance(GlanceKind kind) => $"Glance_{kind}";
}
