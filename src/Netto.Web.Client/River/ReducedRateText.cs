using Netto.Core.CorporateTax;

namespace Netto.Web.Client.River;

// The bubble of the term « reduced corporate rate », which the Optimisation tab's pay curve and the Copyright
// royalty card both explain where they weigh it: the key each card's texts hold it under, and what it states
// of the year's rules.
internal static class ReducedRateText
{
    // What the reduced corporate rate is, in the bubble the term opens where the rate is weighed.
    public const string Term = "Term_ReducedRate";

    // What the bubble states of the year's rules: the share of the pay the flat-rate benefits may reach
    // (art. 215 al. 3, 5°), none for a year without them, when no rate is weighed.
    public static object[] Arguments(int incomeYear) =>
        CorporateTaxRules.For(incomeYear) is { } rules ? [Formats.RuleAmount(rules.ExcessBenefitsShare)] : [""];
}
