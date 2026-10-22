namespace Netto.Web.Client.River;

// The resource keys PlansPanel.razor builds. The component and PlansPanelTextTests both go through
// these, so a key without a text in PlansPanel.resx fails a test instead of showing on screen.
internal static class PlansPanelText
{
    // What the « Plans » button says for a screen reader: how many plans are saved.
    public static string ButtonLabel(int count) => count switch
    {
        0 => "ButtonLabel_None",
        1 => "ButtonLabel_One",
        _ => "ButtonLabel_Many",
    };

    // A plan's difference from « Today », or that the two cannot be compared (BaselineGain).
    public static string Gain(GainKind kind) => $"Gain_{kind}";

    // Beside it, what the plan's muddy pocket holds more or less than Today's, when it differs.
    public static string Muddy(decimal difference) => difference > 0m ? "Muddy_More" : "Muddy_Less";
}
