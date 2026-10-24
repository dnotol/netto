namespace Netto.Core.CorporateTax;

public static class CorporateTaxCalculator
{
    // A loss pays no tax. The reduced rate covers only the first slice; the rest pays the standard
    // rate. Rounded to the cent once: the reduced slice at its ceiling is a whole amount. The director
    // pay is the pay as art. 215 reads it, with what the company bore for the director and the
    // benefits in kind in it: the taxable pay (YearSimulation), not only the gross on the payslip. The
    // flat-rate benefits are the benefits in that pay valued at a flat rate (art. 36 §2, a company
    // car's), which art. 215 al. 3, 5° holds to a share of it; none unless given.
    public static CorporateTaxResult Compute(
        CorporateTaxRules rules, decimal taxableIncome, decimal directorPay, CompanyStatus status, decimal flatRateBenefits = 0m)
    {
        var condition = Condition(rules, taxableIncome, directorPay, status, flatRateBenefits);
        var taxed = Math.Max(0m, taxableIncome);

        var reducedSlice = condition.AppliesReducedRate() ? Math.Min(taxed, rules.ReducedRateCeiling.Value) : 0m;
        var tax = rules.ReducedRate.Value * reducedSlice + rules.StandardRate.Value * (taxed - reducedSlice);

        return new CorporateTaxResult(
            taxableIncome, condition, rules.MinimumDirectorPay.Value, Money.RoundToCent(tax), directorPay, flatRateBenefits, rules.ExcessBenefitsShare.Value);
    }

    // The smallest director pay that keeps the reduced rate out of the taxable income the company would
    // have at no pay, its profit before pay with what it cannot deduct added back, given the flat-rate
    // benefits B the directors receive besides it. The pay is the one the split moves, the gross and the
    // contributions the company pays for the director, B left out: art. 215 al. 3, 4° reads the pay with
    // B, so it is met from the minimum less B, or from half of what that income exceeds B by, rounded up
    // to the cent, where the taxable income the pay leaves no longer exceeds the pay with B; and 5° from
    // the pay that leaves B within its share of the total, 4 B at 20%, rounded up to the cent. Null when
    // the rate does not depend on the pay: not a small company, or in its first four tax periods without
    // benefits; in them, only the cap on benefits depends on the pay.
    public static decimal? SmallestPayKeepingReducedRate(
        CorporateTaxRules rules, decimal taxableIncomeAtNoPay, CompanyStatus status, decimal flatRateBenefits = 0m)
    {
        if (!status.SmallCompany)
            return null;

        var share = rules.ExcessBenefitsShare.Value;
        var withinTheCap = Money.UpToCent(flatRateBenefits * (1m - share) / share);
        if (status.FirstFourTaxPeriods)
            return flatRateBenefits > 0m ? withinTheCap : null;

        var halfTheExcess = Money.UpToCent((taxableIncomeAtNoPay - flatRateBenefits) / 2m);
        var meetingThePayTest = Math.Clamp(halfTheExcess, 0m, Math.Max(0m, rules.MinimumDirectorPay.Value - flatRateBenefits));
        return Math.Max(meetingThePayTest, withinTheCap);
    }

    private static ReducedRateCondition Condition(
        CorporateTaxRules rules, decimal taxableIncome, decimal directorPay, CompanyStatus status, decimal flatRateBenefits)
    {
        if (!status.SmallCompany)
            return ReducedRateCondition.NotSmallCompany;
        if (flatRateBenefits > rules.ExcessBenefitsShare.Value * directorPay)
            return ReducedRateCondition.ExcessBenefits;
        if (status.FirstFourTaxPeriods)
            return ReducedRateCondition.FirstFourTaxPeriods;
        if (directorPay >= rules.MinimumDirectorPay.Value)
            return ReducedRateCondition.MinimumPayMet;

        return taxableIncome <= directorPay ? ReducedRateCondition.TaxableIncomeNotAbovePay : ReducedRateCondition.MinimumPayNotMet;
    }
}
