using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys PisteCard.razor builds, held to PisteCard.resx by PisteCardTextTests.
internal static class PisteCardText
{
    // The vouchers' one question: that the company grants them on their card's conditions.
    public const string VouchersGranted = "VouchersGranted";

    // The trial's three figures: the optimised plan without the idea, with it, and the difference.
    public const string TrialWithout = "TrialWithout";

    public const string TrialWith = "TrialWith";

    public const string TrialGain = "TrialGain";

    // That Keep adds the idea alone, Optimise retuning the rest.
    public const string KeepNote = "KeepNote";

    // Where the idea stands, beside its gain or in its place.
    public static string State(PisteState state) => $"State_{state}";

    // The line that opens the idea's trial, where the focus goes on « Try »: only a state a trial shows has
    // one, an idea asking or answered being not tried yet.
    public static string Trial(PisteState state) => state is PisteState.Asking or PisteState.Ready
        ? throw new ArgumentOutOfRangeException(nameof(state), state, "An idea asking or answered shows no trial.")
        : $"Trial_{state}";

    // The tuned gain, « if yes » while the idea awaits the viewer's yes.
    public static string Gain(bool awaitsYes) => awaitsYes ? "Gain_IfYes" : "Gain";
}
