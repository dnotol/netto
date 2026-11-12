using Netto.Core.Copyright;
using Netto.Core.Rules;

namespace Netto.Web.Client.River.Copyright;

// The copyright royalty's figures, line by line, as its card shows them before where its money goes
// (FigureList), keys of the card's texts with their amounts: how its limits split the royalty and what the
// company withholds on the part beyond them, then the plan with the royalty against the same plan without
// it. Each amount is said once: a part that is the whole royalty is no line of its own. What a
// label cannot say, a line's bubble does (Help); the rules the card no longer says in sentences are in its
// details, on the steps they are the rules of. The view model selects and orders the engine's figures, and
// computes none.
internal static class CopyrightFigures
{
    // The royalty the company pays, whole, which the map's gate says crosses (Tabs/MapGates).
    public const string Paid = "Figure_Royalty";

    // The royalty as the engine took it: while its limits split it, the part within them, with the limits in
    // its bubble, the sentence under the lines saying which limit set them; the flat-rate costs that come off the part within before it is taxed;
    // and what the company withholds on the part beyond at its rate, which the tax on pay credits. What it
    // withholds on the part within is the tax apart itself, the card's drain line, never a second figure.
    // Each rate is the rule set's own, as it states it. A line of nothing is left out. None without a
    // royalty.
    public static IReadOnlyList<FigureLine> Royalty(CopyrightResult royalty, CopyrightRules rules) => royalty.Royalty == 0m
        ? []
        :
        [
            new(Paid, royalty.Royalty),
            .. Parts(royalty, rules),
            .. Some(new FigureLine("Figure_FlatRateCosts", royalty.FlatRateCosts == 0m ? 0m : -royalty.FlatRateCosts)
            {
                Help = "Help_FlatRateCosts",
                HelpArgs =
                [
                    Formats.RuleAmount(rules.FlatRateCosts.FirstBandRate), Formats.RuleAmount(rules.FlatRateCosts.FirstBandCeiling),
                    Formats.RuleAmount(rules.FlatRateCosts.SecondBandRate), Formats.RuleAmount(rules.FlatRateCosts.SecondBandCeiling),
                    Formats.RuleAmount(rules.TaxedApartRate),
                ],
            }),
            .. Some(new FigureLine("Figure_WithheldBeyond", royalty.WithheldBeyondLimits) { Args = [Formats.RuleAmount(rules.BeyondLimitsWithholdingRate)] }),
        ];

    // The plan with its royalty, at the pay set: what reaches the pocket and what goes to the taxes on pay
    // and on profit.
    public static IReadOnlyList<FigureLine> With(CopyrightComparison comparison) =>
    [
        new("Figure_Pocket", comparison.PocketWith),
        new("Figure_TaxesOnPay", comparison.TaxesOnPayWith),
        new("Figure_TaxesOnProfit", comparison.TaxesOnProfitWith),
    ];

    // The same plan without it, where the company keeps the royalty as profit, taxed and paid out with the
    // dividend.
    public static IReadOnlyList<FigureLine> Without(CopyrightComparison comparison) =>
    [
        new("Figure_Pocket", comparison.PocketWithout),
        new("Figure_TaxesOnPay", comparison.TaxesOnPayWithout),
        new("Figure_TaxesOnProfit", comparison.TaxesOnProfitWithout) { Help = "Help_KeptAsProfit" },
    ];

    // The part within the limits, a line only while the limits split the royalty; the part beyond is the
    // card's drain line on to Director pay.
    private static IEnumerable<FigureLine> Parts(CopyrightResult royalty, CopyrightRules rules) => royalty.WithinLimits == 0m || royalty.TaxedAsPay == 0m
        ? []
        :
        [
            new FigureLine("Figure_WithinLimits", royalty.WithinLimits)
            {
                Help = "Help_WithinLimits",
                HelpArgs =
                [
                    Formats.RuleAmount(rules.RelativeLimit), Formats.Euros(royalty.RelativeLimit), Formats.Euros(royalty.Cap),
                    rules.IncomeYear.ToString(Formats.Culture),
                ],
            },
        ];

    private static IEnumerable<FigureLine> Some(FigureLine line) => line.Amount == 0m ? [] : [line];
}
