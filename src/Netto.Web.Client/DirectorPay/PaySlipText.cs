namespace Netto.Web.Client.DirectorPay;

// The resource keys PaySlip.razor builds from GrossToNetView's keys. The component and
// PaySlipTextTests both go through these, so a key the engine returns without a text in
// PaySlip.resx fails a test instead of showing on screen.
internal static class PaySlipText
{
    public static string Track(string key) => $"Track_{key}";

    public static string TrackHint(string key) => $"TrackHint_{key}";

    public static string Row(string key) => $"Row_{key}";

    // The balance's line for what was withheld says where the amount comes from.
    public static string Withheld(bool given) => given ? "Withheld_Given" : "Withheld_Derived";

    public static string Balance(BalanceKind kind) => $"Balance_{kind}";
}
