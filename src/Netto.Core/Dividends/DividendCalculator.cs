namespace Netto.Core.Dividends;

public static class DividendCalculator
{
    // The company withholds on the whole dividend, rounded to the cent. The exempt part is not
    // declared, but the withholding on it is, and the return credits it back.
    public static DividendResult Compute(DividendRules rules, decimal dividend, WithholdingCategory category)
    {
        var rate = rules.Rate(category).Value;
        var withholding = Money.RoundToCent(rate * dividend);
        var exemptionRefund = Money.RoundToCent(rate * Math.Min(dividend, rules.ExemptAmount.Value));

        return new DividendResult(dividend, rate, withholding, exemptionRefund, dividend - withholding + exemptionRefund);
    }
}
