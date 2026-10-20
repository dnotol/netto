using Netto.Core.Simulation;

namespace Netto.Core.PersonalTax;

// The lines round where Tax-Calc's « Détail du calcul » rounds them (runs T9 to T11 of 2026-10-20):
// the tax at 106%, the surcharge on the base, each payment's credit on its needed part, the 90%, and
// each payment's bonus.
public static class IncomeTaxAdvancePaymentCalculator
{
    public static IncomeTaxAdvancePaymentResult Compute(IncomeTaxAdvancePaymentRules rules, decimal totalTax, decimal taxCredits, TaxPrepayments prepayments)
    {
        var figures = Figures(rules, totalTax, taxCredits, prepayments);
        var topUp = figures.Surcharge == 0m ? 0m : TopUp(rules, totalTax, taxCredits, prepayments, figures.SurchargeBeforeCredits);

        return figures with { LastDeadlineTopUp = topUp };
    }

    // The surcharge and the bonus without the suggested payment, which the split's search does not
    // read.
    internal static IncomeTaxAdvancePaymentResult Figures(IncomeTaxAdvancePaymentRules rules, decimal totalTax, decimal taxCredits, TaxPrepayments prepayments)
    {
        var paid = prepayments.Paid.ByDeadline;
        var taxAt106 = Money.RoundToCent(totalTax * rules.BaseFactor.Value);
        var prepaidAtSource = prepayments.Withheld + taxCredits;
        var taxBase = Math.Max(0m, taxAt106 - prepaidAtSource);
        var beforeCredits = prepayments.Starter ? 0m : Money.RoundToCent(taxBase * rules.SurchargeRate);

        var needed = new decimal[paid.Count];
        var credits = new decimal[paid.Count];
        var left = beforeCredits;
        for (var i = 0; i < paid.Count && left > 0m; i++)
        {
            var rate = rules.Deadlines[i].CreditRate(rules);
            var fullCredit = Money.RoundToCent(paid[i] * rate);
            needed[i] = fullCredit <= left ? paid[i] : SmallestCovering(left, rate);
            credits[i] = Money.RoundToCent(needed[i] * rate);
            left -= credits[i];
        }

        var beforeKeptRate = Math.Max(0m, left);
        var kept = Money.RoundToCent(beforeKeptRate * rules.KeptRate.Value);
        var belowMinimum = kept > 0m && (kept < rules.MinimumSurcharge.Value || kept < rules.MinimumShareRate.Value * taxBase);

        var bonuses = new decimal[paid.Count];
        var bonusBaseLeft = Math.Max(0m, taxAt106 - prepaidAtSource - needed.Sum());
        for (var i = 0; i < paid.Count; i++)
        {
            var eligible = Math.Min(paid[i] - needed[i], bonusBaseLeft);
            bonuses[i] = Money.RoundToCent(eligible * rules.Deadlines[i].BonusRate(rules));
            bonusBaseLeft -= eligible;
        }

        return new IncomeTaxAdvancePaymentResult(
            taxAt106, taxBase, beforeCredits, needed, credits, beforeKeptRate, belowMinimum ? 0m : kept, belowMinimum,
            bonuses, bonuses.Sum(), 0m, prepayments.Starter);
    }

    // The smallest amount in cents whose credit, rounded half away from zero, still covers what is
    // left: Tax-Calc's « VA nécessaires pour éviter la majoration ».
    private static decimal SmallestCovering(decimal left, decimal rate) => Math.Ceiling((left - 0.005m) / rate * 100m) / 100m;

    // The smallest payment more by the last deadline after which no surcharge is due, to the cent:
    // the surcharge only falls as that payment rises, and paying what covers the whole surcharge
    // before credits there leaves none.
    private static decimal TopUp(IncomeTaxAdvancePaymentRules rules, decimal totalTax, decimal taxCredits, TaxPrepayments prepayments, decimal beforeCredits)
    {
        var last = prepayments.Paid.ByDeadline.Count - 1;
        var low = 0m;
        var high = SmallestCovering(beforeCredits, rules.Deadlines[last].CreditRate(rules));
        while (high - low > 0.01m)
        {
            var middle = Math.Floor((low + high) * 50m) / 100m;
            if (Figures(rules, totalTax, taxCredits, WithMoreByTheLastDeadline(prepayments, middle)).Surcharge == 0m)
                high = middle;
            else
                low = middle;
        }

        return high;
    }

    private static TaxPrepayments WithMoreByTheLastDeadline(TaxPrepayments prepayments, decimal more) =>
        prepayments with { Paid = prepayments.Paid with { Fourth = prepayments.Paid.Fourth + more } };
}
