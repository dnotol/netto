namespace Netto.Core.Rules;

// Where a rule or an amount comes from: an official text (tax administration, Moniteur belge,
// INASTI), the place in it, and where to read it. Every figure Netto shows traces back to one.
public sealed record Source(string Title, string Reference, Uri Url);
