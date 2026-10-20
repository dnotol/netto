using Netto.Core.Simulation;

namespace Netto.Core.CorporateTax;

public static class AdvancePaymentCalculator
{
    // The surcharge before credits and each credit are rounded to the cent, then subtracted: the SPF
    // multiplies the base by the rate, then deducts each payment's credit. The base is the corporate
    // tax itself, a one-person company having no withholding or credit to deduct from it.
    public static AdvancePaymentResult Compute(AdvancePaymentRules rules, decimal corporateTax, AdvancePayments payments, CompanyStatus status)
    {
        var paid = payments.ByDeadline;
        if (status.SmallCompany && status.FirstThreeFinancialYears)
            return new AdvancePaymentResult(0m, [.. paid.Select(_ => 0m)], 0m, 0m, true);

        var beforeCredits = Money.RoundToCent(corporateTax * rules.SurchargeRate);
        var credits = paid.Select((amount, i) => Money.RoundToCent(amount * rules.Deadlines[i].CreditRate(rules))).ToList();
        var surcharge = Math.Max(0m, beforeCredits - credits.Sum());

        return new AdvancePaymentResult(beforeCredits, credits, surcharge, TopUp(rules, beforeCredits, credits, paid, surcharge), false);
    }

    // What the last payment must reach for its credit to cover what the others leave, rounded up to
    // the cent so that its credit, rounded, still covers it; less what it already is.
    private static decimal TopUp(AdvancePaymentRules rules, decimal beforeCredits, List<decimal> credits, IReadOnlyList<decimal> paid, decimal surcharge)
    {
        if (surcharge == 0m)
            return 0m;

        var last = credits.Count - 1;
        var leftForTheLast = beforeCredits - credits.Take(last).Sum();
        var lastNeeded = Math.Ceiling(leftForTheLast / rules.Deadlines[last].CreditRate(rules) * 100m) / 100m;
        return lastNeeded - paid[last];
    }
}
