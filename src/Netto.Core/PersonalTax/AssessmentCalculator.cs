using Netto.Core.Simulation;

namespace Netto.Core.PersonalTax;

// The balance on the director's assessment: the final tax on the year's income, the pay and any rent
// taxed with it, less what was withheld on the pay and paid in advance on its tax, which the
// assessment sets against it and refunds beyond it (CIR 92 art. 304, §2). Every line is rounded
// already, so the balance is their arithmetic; the 2.50 under which nothing is collected or refunded
// (art. 304, §1 and §2) is not modelled.
public static class AssessmentCalculator
{
    public static Assessment Compute(
        PersonalTaxResult tax, IncomeTaxAdvancePaymentResult advancePayments, decimal withheld, bool withheldGiven, AdvancePayments paid)
    {
        var finalTax = tax.IncomeTax + tax.MunicipalSurcharge - tax.LowEarnedIncomeCredit + advancePayments.Surcharge - advancePayments.Bonus;
        var paidInAdvance = paid.ByDeadline.Sum();

        return new Assessment(finalTax, withheld, withheldGiven, paidInAdvance, finalTax - withheld - paidInAdvance, PersonalTaxRules.AssessmentBalance);
    }
}
