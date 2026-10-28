namespace Netto.Web.Client.Tabs;

// The resource keys MapFilter.razor builds from MapFilter.resx, held to it by MapFilterTextTests, so a kind
// or a count without its words fails a test instead of showing its key on screen.
internal static class MapFilterText
{
    // The group's name, for a screen reader, the legend of the lines, whose boxes say which are drawn; and
    // what its bubble says more about.
    public const string Legend = "Legend";

    // In the legend's bubble, a kind's name, then what it is.
    public const string GlossLine = "GlossLine";

    // The line under the group when the viewer hid every kind: the one line it ever says, the boxes' own
    // states and counts saying the rest.
    public const string AllHidden = "AllHidden";

    // A kind by its name, then what it is.
    public static string Name(MapLinkKind kind) => Enum.IsDefined(kind) ? $"Name_{kind}" : throw Unknown(kind);

    public static string Gloss(MapLinkKind kind) => Enum.IsDefined(kind) ? $"Gloss_{kind}" : throw Unknown(kind);

    // After a kind's name, how many links the plan has of it, in words for a screen reader: the eye reads
    // the number alone.
    public static string Count(int count) => count switch
    {
        < 0 => throw new ArgumentOutOfRangeException(nameof(count), count, "A count of links is not below none."),
        0 => "Count_None",
        1 => "Count_One",
        _ => "Count_Many",
    };

    private static ArgumentOutOfRangeException Unknown(MapLinkKind kind) =>
        new(nameof(kind), kind, "A kind of link the filter has no words for.");
}
