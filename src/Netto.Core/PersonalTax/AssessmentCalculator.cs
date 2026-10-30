using Netto.Core.Simulation;

namespace Netto.Core.PersonalTax;

// The balance on the director's assessment: the final tax on the year's income, the pay, any rent
// taxed with it and any income taxed apart, less what was withheld on the pay and paid in advance on
// its tax, the credits for low earned income and for children coming off it (art. 134 §3 and 289ter),
// and what was withheld at source on movable income paid besides the pay, a copyright
// royalty's, which the assessment sets against it and refunds beyond it (CIR 92 art. 279 and 304, §2;
// Tax-Calc T16 to T18). Every line is rounded already, so the balance is their arithmetic; the 2.50
// under which nothing is collected or refunded (art. 304, §1 and §2) is not modelled. Neither the final tax
// nor the balance is a negative zero when nothing is due: a balance of nothing is settled, not refunded.
public static class AssessmentCalculator
{
    public static Assessment Compute(
        PersonalTaxRules rules, PersonalTaxResult tax, IncomeTaxAdvancePaymentResult advancePayments, decimal withheld, bool withheldGiven, AdvancePayments paid,
        decimal withheldBesidesPay = 0m)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(withheldBesidesPay, 0m);

        var finalTax = Money.PlainZero(tax.IncomeTax + tax.MunicipalSurcharge - tax.LowEarnedIncomeCredit - tax.ChildTaxCredit + advancePayments.Surcharge - advancePayments.Bonus);
        var paidInAdvance = paid.ByDeadline.Sum();

        return new Assessment(finalTax, withheld, withheldGiven, paidInAdvance, Money.PlainZero(finalTax - withheld - withheldBesidesPay - paidInAdvance), rules.AssessmentBalance)
        {
            WithheldBesidesPay = withheldBesidesPay,
        };
    }
}
