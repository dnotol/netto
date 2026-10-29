namespace Netto.Web.Client.Tabs;

// The resource keys MapKey.razor builds from MapKey.resx, held to it by MapKeyTextTests, so a line without its
// words fails a test instead of showing its key on screen.
internal static class MapKeyText
{
    // The key's name, for a screen reader, which hears it before its lines; and what its bubble says more about.
    public const string Key = "Key";

    // In the key's bubble, a line's name, then what it is.
    public const string GlossLine = "GlossLine";

    // A line by its name, then what it is.
    public static string Name(MapKeyLine line) => Enum.IsDefined(line) ? $"Name_{line}" : throw Unknown(line);

    public static string Gloss(MapKeyLine line) => Enum.IsDefined(line) ? $"Gloss_{line}" : throw Unknown(line);

    private static ArgumentOutOfRangeException Unknown(MapKeyLine line) => new(nameof(line), line, "A line the key has no words for.");
}
