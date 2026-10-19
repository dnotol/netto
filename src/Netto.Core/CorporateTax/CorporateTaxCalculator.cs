namespace Netto.Core.CorporateTax;

public static class CorporateTaxCalculator
{
    // A loss pays no tax. The reduced rate covers only the first slice; the rest pays the standard
    // rate. Rounded to the cent once: the reduced slice at its ceiling is a whole amount.
    public static CorporateTaxResult Compute(CorporateTaxRules rules, decimal taxableIncome, decimal directorPay, CompanyStatus status)
    {
        var condition = Condition(rules, taxableIncome, directorPay, status);
        var taxed = Math.Max(0m, taxableIncome);

        var reducedSlice = condition.AppliesReducedRate() ? Math.Min(taxed, rules.ReducedRateCeiling.Value) : 0m;
        var tax = rules.ReducedRate.Value * reducedSlice + rules.StandardRate.Value * (taxed - reducedSlice);

        return new CorporateTaxResult(taxableIncome, condition, Money.RoundToCent(tax));
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
