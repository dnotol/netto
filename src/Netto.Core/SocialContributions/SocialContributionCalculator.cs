namespace Netto.Core.SocialContributions;

public static class SocialContributionCalculator
{
    // INASTI bills the year in four equal quarters, each rounded to the cent, so the yearly amount
    // is four rounded quarters: 4 × 890.42 at the minimum, where rounding once a year would give
    // 3,561.69.
    public static decimal Annual(SocialContributionRules rules, decimal incomeBase) =>
        Billed(Unrounded(rules, incomeBase));

    internal static decimal Billed(decimal unroundedAnnual) => 4 * Money.RoundToCent(unroundedAnnual / 4);

    // The exact yearly amount before billing, for the gross-to-net fixed point, which has to be
    // solved without rounding (see GrossToNet).
    internal static decimal Unrounded(SocialContributionRules rules, decimal incomeBase)
    {
        var firstBandCeiling = rules.FirstBandCeiling.Value;
        var assessed = Math.Max(incomeBase, rules.MinimumIncome.Value);

        var firstBand = Math.Min(assessed, firstBandCeiling);
        var secondBand = Math.Clamp(assessed, firstBandCeiling, rules.SecondBandCeiling.Value) - firstBandCeiling;

        return rules.FirstBandRate.Value * firstBand + rules.SecondBandRate.Value * secondBand;
    }
}
