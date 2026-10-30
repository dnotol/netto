namespace Netto.Web.Client.Tabs;

// The resource keys of a gate's lines and of the pay's pill (MapGates), held to MapGateWords.resx by
// MapGateTextTests, so a line without its words fails a test instead of showing its key on screen; and the
// cut of a sentence where its amount goes, so the amount can be drawn apart, what reaches the viewer in gold.
internal static class MapGateText
{
    // The size a gate's line is drawn at (MapGateWords.razor.css), which its width is held to at the
    // narrowest map (MapGateTextTests).
    internal const decimal FontRem = 0.68m;

    // What crosses: the rent, the royalty, the dividend paid out.
    public const string Across = "Gate_Across";

    // What reaches the viewer, in gold.
    public const string ToYou = "Gate_ToYou";

    // After what the vouchers or the allowances bring the viewer: no tax is due on it.
    public const string Untaxed = "Gate_Untaxed";

    // The car's benefit in kind, taxed with the viewer's pay.
    public const string Benefit = "Gate_Benefit";

    // What the viewer pays for the car, their contribution.
    public const string YouPay = "Gate_YouPay";

    // In the partner's box, the marital quotient: its words on a line, then its amount on the next, the box
    // being too narrow for the two on one.
    public const string Quotient = "Gate_Quotient";

    public const string QuotientAmount = "Gate_QuotientAmount";

    private const string Amount = "{0}";

    // The pay's pill: the pay alone, or with the contributions the company pays on the director's behalf.
    public static string Pay(bool withContributions) => withContributions ? "Pill_PayAndContributions" : "Pill_Pay";

    // How many lines the pay's pill says, by its words: the pay alone on one; with the contributions, whose words
    // no pill holds on one line beside the amount in either language, its words over its amount, on two
    // (MapGateTextTests, FrenchTabsTests).
    public static int PillLines(string key) => key == Pay(withContributions: true) ? 2 : 1;

    // A sentence cut where its one amount goes: the words before it, after it, and whether it has one; a
    // sentence with no amount is its words alone.
    public static (string Before, string After, bool HoldsAmount) Parts(string sentence)
    {
        var at = sentence.IndexOf(Amount, StringComparison.Ordinal);
        if (at < 0)
            return (sentence, "", false);
        if (sentence.IndexOf(Amount, at + Amount.Length, StringComparison.Ordinal) >= 0)
            throw new ArgumentException("A gate's line says one amount.", nameof(sentence));

        return (sentence[..at], sentence[(at + Amount.Length)..], true);
    }
}
