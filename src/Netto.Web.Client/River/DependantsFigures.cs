using Netto.Core.PersonalTax;

namespace Netto.Web.Client.River;

// The Household card's lines for the dependants (FigureLine), which the card words from its own texts: each
// supplement to the tax-free amount with its count, its bubble saying the year's amounts; their total, added
// to the tax-free amount of the director or of the partner with the higher taxable income; the refundable
// credit for children, or what the pay takes away of a joint year's; and what the dependants save the
// river's tax, or cost it. Each amount reads as what its words say, never a negative « saves ». A line of
// nothing is left out (FigureLines). They select the engine's figures.
internal static class DependantsFigures
{
    public static IReadOnlyList<FigureLine> Of(DependantsView view)
    {
        var (declared, supplements, rules) = (view.Declared, view.Supplements, view.Rules);

        return FigureLines.Shown(
        [
            new FigureLine("Figure_SupplementChildren", supplements.Children)
            {
                Args = [Count(declared.Children)], Help = "Help_SupplementChildren",
                HelpArgs = [.. new[] { rules.OneChild, rules.TwoChildren, rules.ThreeChildren, rules.FourChildren, rules.EachChildBeyondFour }.Select(Formats.RuleAmount)],
            },
            new FigureLine("Figure_SupplementUnder3", supplements.ChildrenUnder3)
            {
                Args = [Count(declared.ChildrenUnder3)], Help = "Help_SupplementUnder3", HelpArgs = [Formats.RuleAmount(rules.ChildUnder3)],
            },
            new FigureLine("Figure_SupplementSingleParent", supplements.SingleParent) { Help = "Help_SupplementSingleParent", HelpArgs = [Formats.RuleAmount(rules.SingleParent)] },
            new FigureLine("Figure_SupplementInCare", supplements.DependantsAged66InCare)
            {
                Args = [Count(declared.DependantsAged66InCare)], Help = "Help_SupplementInCare", HelpArgs = [Formats.RuleAmount(rules.DependantAged66InCare)],
            },
            new FigureLine("Figure_SupplementOthers", supplements.OtherDependants)
            {
                Args = [Count(declared.OtherDependants)], Help = "Help_SupplementOthers", HelpArgs = [Formats.RuleAmount(rules.OtherDependant)],
            },
            view.To == Taxpayer.Partner
                ? new FigureLine("Figure_SupplementsToPartner", supplements.Total) { Help = "Help_SupplementsToPartner" }
                : new FigureLine("Figure_SupplementsToYou", supplements.Total) { Help = "Help_SupplementsToYou" },
            view.ChildTaxCredit < 0m
                ? new FigureLine("Figure_ChildTaxCreditTakenAway", -view.ChildTaxCredit) { Help = "Help_ChildTaxCreditTakenAway" }
                : new FigureLine("Figure_ChildTaxCredit", view.ChildTaxCredit) { Help = "Help_ChildTaxCredit", HelpArgs = [Formats.RuleAmount(rules.ChildCreditCapPerChild)] },
            view.Saves < 0m
                ? new FigureLine("Figure_DependantsCost", -view.Saves) { Help = "Help_DependantsSave" }
                : new FigureLine("Figure_DependantsSave", view.Saves) { Help = "Help_DependantsSave" },
        ]);
    }

    private static string Count(int count) => count.ToString(Formats.Culture);
}
