using Netto.Core.Rules;

namespace Netto.Web.Client.River;

// The resource keys the river's components other than its cards build from values and the engine's
// keys, one class per component: the blocks several cards show, the bar's menu and the notice of a
// module taken out. Each card's own keys sit beside the card, in a file of their own (RentCardText.cs,
// CompanyCar/CarCardText.cs). The components and RiverTextTests both go through these, so a key
// without a text in its .resx fails a test instead of showing on screen.
internal static class OriginChipText
{
    public static string Origin(InputOrigin origin) => $"Origin_{origin}";

    public static string Hint(InputOrigin origin) => $"Hint_{origin}";
}

internal static class DrainListText
{
    // A text the step behind a drain follows, as its bubble cites it.
    public const string Source = "Source";

    public static string Drain(string key) => $"Drain_{key}";

    // Where a drain goes; what comes back says where from.
    public static string Destination(Drain drain) => drain.Kind == DrainKind.Back ? $"From_{drain.To}" : $"To_{drain.To}";
}

internal static class RefusedMessageText
{
    public static string Error(Refusal refusal) => $"Error_{refusal.Key}";
}

internal static class ConditionListText
{
    public static string Condition(string key) => $"Condition_{key}";

    // « 1 condition Netto cannot check » or « 5 conditions Netto cannot check ».
    public static string Heading(int count) => count == 1 ? "Heading_One" : "Heading_Many";
}

// The share a card's summary ends on, worded once in ModuleCard.resx for every card.
internal static class ModuleCardText
{
    public const string ToYou = "ToYou";

    public const string PassesOn = "PassesOn";

    // What a lever adds to the pocket over what it pays out (LeverGain).
    public const string OfThePayout = "OfThePayout";

    // The line a card says while it waits for the company's year.
    public const string Waiting = "Waiting";

    // Its detail view's line, and the title over its rules with the income year.
    public const string DetailsLabel = "DetailsLabel";

    public const string SourcesTitle = "SourcesTitle";
}

internal static class CompositionMenuText
{
    public static string Title(OptionalModule module) => $"Title_{module}";

    // What the module is, said while it is out of the plan.
    public static string Description(OptionalModule module) => $"Description_{module}";

    // What it brings to the pocket, said while it is in the plan, of the amount's size; or what it costs, the
    // household's tax rising rather than falling.
    public static string Brings(OptionalModule module, decimal toPocket) => toPocket < 0m ? $"Costs_{module}" : $"Brings_{module}";

    // The button's name, with how many optional modules the plan holds.
    public static string ButtonLabel(int count) => count switch
    {
        0 => "ButtonLabel_None",
        1 => "ButtonLabel_One",
        _ => "ButtonLabel_Many",
    };
}

internal static class TakenOutNoticeText
{
    public static string TakenOut(OptionalModule module) => $"TakenOut_{module}";
}

internal static class InfoBubbleText
{
    // The name of a bubble's (i): « More about » what it stands beside.
    public const string MoreAbout = "MoreAbout";
}

// A sentence of a card with a term of art in it, marked in its .resx text between square brackets:
// « It keeps the [reduced corporate rate], … ». TermSentence draws the term as the trigger of an info
// bubble that says what it means, and the rest as text.
internal static class TermText
{
    // The text before the term, the term and the text after it; the whole sentence, with no term,
    // when none is marked.
    public static (string Before, string? Term, string After) Split(string sentence)
    {
        var open = sentence.IndexOf('[');
        var close = open < 0 ? -1 : sentence.IndexOf(']', open + 1);
        return close < 0 ? (sentence, null, "") : (sentence[..open], sentence[(open + 1)..close], sentence[(close + 1)..]);
    }
}
