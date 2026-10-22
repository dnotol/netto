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
            return new AdvancePaymentResult(0m, [.. paid.Select(_ => 0m)], 0m, [.. paid.Select(_ => 0m)], true);

        var beforeCredits = Money.RoundToCent(corporateTax * rules.SurchargeRate);
        var credits = paid.Select((amount, i) => Money.RoundToCent(amount * rules.Deadlines[i].CreditRate(rules))).ToList();
        var surcharge = Math.Max(0m, beforeCredits - credits.Sum());
        var topUps = paid.Select((_, deadline) => TopUp(rules, beforeCredits, credits, paid, surcharge, deadline)).ToList();

        return new AdvancePaymentResult(beforeCredits, credits, surcharge, topUps, false);
    }

    // What more paid by that deadline, the first being 0, cancels the surcharge the payments leave:
    // nothing when they leave none or the company is exempt.
    public static decimal TopUpBy(AdvancePaymentRules rules, decimal corporateTax, AdvancePayments payments, CompanyStatus status, int deadline) =>
        Compute(rules, corporateTax, payments, status).TopUps[deadline];

    // What the payment by the deadline must reach for its credit to cover what the others leave,
    // rounded up to the cent so that its credit, rounded, still covers it; less what it already is.
    private static decimal TopUp(AdvancePaymentRules rules, decimal beforeCredits, IReadOnlyList<decimal> credits, IReadOnlyList<decimal> paid, decimal surcharge, int deadline)
    {
        if (surcharge == 0m)
            return 0m;

        var leftForIt = beforeCredits - credits.Where((_, i) => i != deadline).Sum();
        var needed = Math.Ceiling(leftForIt / rules.Deadlines[deadline].CreditRate(rules) * 100m) / 100m;
        return needed - paid[deadline];
    }
}
