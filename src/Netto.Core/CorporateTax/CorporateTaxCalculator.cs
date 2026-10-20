namespace Netto.Core.CorporateTax;

public static class CorporateTaxCalculator
{
    // A loss pays no tax. The reduced rate covers only the first slice; the rest pays the standard
    // rate. Rounded to the cent once: the reduced slice at its ceiling is a whole amount. The director
    // pay is the pay as art. 215 reads it, with what the company bore for the director in it: the
    // taxable pay (YearSimulation), not only the gross on the payslip.
    public static CorporateTaxResult Compute(CorporateTaxRules rules, decimal taxableIncome, decimal directorPay, CompanyStatus status)
    {
        var condition = Condition(rules, taxableIncome, directorPay, status);
        var taxed = Math.Max(0m, taxableIncome);

        var reducedSlice = condition.AppliesReducedRate() ? Math.Min(taxed, rules.ReducedRateCeiling.Value) : 0m;
        var tax = rules.ReducedRate.Value * reducedSlice + rules.StandardRate.Value * (taxed - reducedSlice);

        return new CorporateTaxResult(taxableIncome, condition, rules.MinimumDirectorPay.Value, Money.RoundToCent(tax));
    }

    // The smallest director pay that meets art. 215's pay condition out of a given profit before pay:
    // the minimum, or half the profit, rounded up to the cent, where the taxable income left no longer
    // exceeds the pay. The pay is the one art. 215 reads, the taxable pay; YearSimulation turns it into
    // the pay the split moves. Null when the rate does not depend on the pay: not a small company, or
    // still in its first four tax periods.
    public static decimal? SmallestPayKeepingReducedRate(CorporateTaxRules rules, decimal profitBeforePay, CompanyStatus status)
    {
        if (!status.SmallCompany || status.FirstFourTaxPeriods)
            return null;

        var halfTheProfit = Math.Ceiling(profitBeforePay * 50m) / 100m;
        return Math.Clamp(halfTheProfit, 0m, rules.MinimumDirectorPay.Value);
    }

    private static ReducedRateCondition Condition(CorporateTaxRules rules, decimal taxableIncome, decimal directorPay, CompanyStatus status)
    {
        if (!status.SmallCompany)
            return ReducedRateCondition.NotSmallCompany;
        if (status.FirstFourTaxPeriods)
            return ReducedRateCondition.FirstFourTaxPeriods;
        if (directorPay >= rules.MinimumDirectorPay.Value)
            return ReducedRateCondition.MinimumPayMet;

        return taxableIncome <= directorPay ? ReducedRateCondition.TaxableIncomeNotAbovePay : ReducedRateCondition.MinimumPayNotMet;
    }
}
