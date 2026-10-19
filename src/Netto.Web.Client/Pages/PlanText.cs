namespace Netto.Web.Client.Pages;

// The resource keys Plan.razor builds from the engine's keys. The page and PlanTextTests both go
// through these, so a key the engine returns without a text in Plan.resx fails a test instead of
// showing on screen.
internal static class PlanText
{
    public static string Track(string key) => $"Track_{key}";

    public static string TrackHint(string key) => $"TrackHint_{key}";

    public static string Row(string key) => $"Row_{key}";

    public static string Error(string errorKey) => $"Error_{errorKey}";
}
