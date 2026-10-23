using Netto.Core.Copyright;
using Netto.Core.Rules;

namespace Netto.Web.Client.River.Copyright;

// The copyright royalty's figures, line by line, as its card shows them before where its money goes
// (FigureList), keys of the card's texts with their amounts: how its limits split the royalty and what the
// company withholds on each part, then the plan with the royalty against the same plan without it. What a
// label cannot say, a line's bubble does (Help); the rules the card no longer says in sentences are in its
// details, on the steps they are the rules of. The view model selects and orders the engine's figures, and
// computes none.
internal static class CopyrightFigures
{
    // The royalty as the engine took it: the part within its limits, with the limits in its bubble, the
    // part beyond them taxed as pay, the flat-rate costs that come off the part within before it is taxed,
    // and what the company withholds on each part at its rate, which the assessment credits. Each rate is
    // the rule set's own, as it states it. A line of nothing is left out. None without a royalty.
    public static IReadOnlyList<FigureLine> Royalty(CopyrightResult royalty, CopyrightRules rules) => royalty.Royalty == 0m
        ? []
        :
        [
            new("Figure_Royalty", royalty.Royalty),
            .. Some(new FigureLine("Figure_WithinLimits", royalty.WithinLimits)
            {
                Help = "Help_WithinLimits",
                HelpArgs =
                [
                    Formats.RuleAmount(rules.RelativeLimit), Formats.Euros(royalty.RelativeLimit), Formats.Euros(royalty.Cap),
                    rules.IncomeYear.ToString(Formats.Culture),
                ],
            }),
            .. Some(new FigureLine("Figure_TaxedAsPay", royalty.TaxedAsPay)),
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
            .. Some(Withheld(royalty.WithheldWithinLimits, rules.WithholdingRate)),
            .. Some(Withheld(royalty.WithheldBeyondLimits, rules.BeyondLimitsWithholdingRate)),
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

    // What the royalty leaves in the pocket more, less or the same, its amount the difference itself.
    public static IReadOnlyList<FigureLine> Difference(CopyrightComparison comparison) =>
        [new(CopyrightCardText.Gain(comparison), Math.Abs(comparison.Gain))];

    private static FigureLine Withheld(decimal amount, RuleAmount rate) =>
        new("Figure_Withheld", amount) { Args = [Formats.RuleAmount(rate)], Help = "Help_Withheld" };

    private static IEnumerable<FigureLine> Some(FigureLine line) => line.Amount == 0m ? [] : [line];
}
