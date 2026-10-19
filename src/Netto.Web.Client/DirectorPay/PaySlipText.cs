namespace Netto.Web.Client.DirectorPay;

// The resource keys PaySlip.razor builds from GrossToNetView's keys. The component and
// PaySlipTextTests both go through these, so a key the engine returns without a text in
// PaySlip.resx fails a test instead of showing on screen.
internal static class PaySlipText
{
    public static string Track(string key) => $"Track_{key}";

    public static string TrackHint(string key) => $"TrackHint_{key}";

    public static string Row(string key) => $"Row_{key}";
}
