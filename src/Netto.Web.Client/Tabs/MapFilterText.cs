namespace Netto.Web.Client.Tabs;

// The resource keys MapFilter.razor builds, and the Map tab builds from MapFilter.resx for the line that
// counts a chosen module's hidden links, held to the .resx by MapFilterTextTests, so a kind or a count
// without its words fails a test instead of showing its key on screen.
internal static class MapFilterText
{
    // The group's visible name: the legend of the lines, whose boxes say which are drawn.
    public const string Legend = "Legend";

    // The line under the group when the viewer hid every kind.
    public const string AllHidden = "AllHidden";

    // A kind by its name, then what it is.
    public static string Name(MapLinkKind kind) => Enum.IsDefined(kind) ? $"Name_{kind}" : throw Unknown(kind);

    public static string Gloss(MapLinkKind kind) => Enum.IsDefined(kind) ? $"Gloss_{kind}" : throw Unknown(kind);

    // After a kind's name, how many links the plan has of it.
    public static string Count(int count) => count switch
    {
        < 0 => throw new ArgumentOutOfRangeException(nameof(count), count, "A count of links is not below none."),
        0 => "Count_None",
        1 => "Count_One",
        _ => "Count_Many",
    };

    // How many of the plan's links are drawn, none while the plan has none: the noun follows the total
    // ("of 1 link"). Every kind hidden says so instead.
    public static string? Summary(MapFilterView view) => view switch
    {
        { AllHidden: true } => AllHidden,
        { Total: 0 } => null,
        { Total: 1 } => "Summary_One",
        _ => "Summary_Many",
    };

    // In the chosen module's list, how many of its links the kinds turned off hide.
    public static string Hidden(int count) => count switch
    {
        < 1 => throw new ArgumentOutOfRangeException(nameof(count), count, "A line of hidden links is for at least one."),
        1 => "Hidden_One",
        _ => "Hidden_Many",
    };

    private static ArgumentOutOfRangeException Unknown(MapLinkKind kind) =>
        new(nameof(kind), kind, "A kind of link the filter has no words for.");
}
