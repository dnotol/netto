namespace Netto.Web.Client.Steps;

// The resource keys StepDetails.razor builds from the engine's keys. The component and
// StepTextTests both go through these, so a key the engine returns without a text in
// StepDetails.resx fails a test instead of showing on screen.
internal static class StepText
{
    public static string Title(string ruleKey) => $"Title_{ruleKey}";

    public static string Rule(string ruleKey) => $"Rule_{ruleKey}";

    public static string Amount(string key) => $"Amount_{key}";
}
