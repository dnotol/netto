namespace Netto.Web.Client.River;

// The resource keys the river's shared components build from values and the engine's keys. The
// components and RiverTextTests both go through these, so a key without a text in its .resx fails a
// test instead of showing on screen.
internal static class OriginChipText
{
    public static string Origin(InputOrigin origin) => $"Origin_{origin}";

    public static string Hint(InputOrigin origin) => $"Hint_{origin}";
}

internal static class DrainListText
{
    public static string Drain(string key) => $"Drain_{key}";

    // Where a drain goes; what comes back says where from.
    public static string Destination(Drain drain) => drain.Kind == DrainKind.Back ? $"From_{drain.To}" : $"To_{drain.To}";
}

internal static class RefusedMessageText
{
    public static string Error(string errorKey) => $"Error_{errorKey}";
}
