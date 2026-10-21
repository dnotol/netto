namespace Netto.Core.Rules;

// Where a rule or an amount comes from: an official text (tax administration, Moniteur belge,
// INASTI), the place in it, and where to read it. Every figure Netto shows traces back to one.
// Document is the text as it was read, filed in official-docs/ under the path INDEX.md's File column
// gives it: its row there says which income years may cite it, so an edition the URL alone cannot
// tell apart is still pinned.
public sealed record Source(string Title, string Reference, Uri Url, string Document)
{
    // How a list cites it in a few words, « CIR 92, art. 38/1 », the title staying the text's full
    // name: the title itself until one is given.
    public string Label { get; init; } = Title;
}
