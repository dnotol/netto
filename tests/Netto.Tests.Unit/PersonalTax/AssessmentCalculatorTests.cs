using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// The balance on the assessment: the final tax, the income tax with the municipal surcharge less the
// credit for low earned income plus the advance-payment surcharge less the bonus, less what was
// withheld and paid in advance. Income year 2025 expected values are Tax-Calc « exercice 2026 » runs
// (IPP_EX_2026 V_1.8.0, single, 5000 at 7%, born 01/01/1985): T12 of 2026-10-20, pay 50,000, own
// contributions 8,300, 10,000 withheld (code 1407), nothing paid in advance, whose « SOLDE » is
// 2,536.23 to pay (the special social security contribution it adds after that line is not a
// self-employed person's); T13 and T14 of 2026-10-20 for a home office's rent taxed with the pay; T8
// of 2026-10-19 for the credit. Income year 2026 values are derived from
// the official amounts, as Tax-Calc does not cover 2026 yet: the tax on 50,000 of pay is
// GrossToNetTests' and the surcharge and the bonus IncomeTaxAdvancePaymentCalculatorTests'.
public class AssessmentCalculatorTests
{
    private static readonly PersonalTaxRules Rules2025 = PersonalTaxRules.IncomeYear2025;
    private static readonly PersonalTaxRules Rules2026 = PersonalTaxRules.IncomeYear2026;

    // Income tax 11,586.55, municipal surcharge 811.06, and 138.62 of advance-payment surcharge: the
    // tax at 106%, 12,281.74, less the 10,000 withheld, at 6.75%, kept at 90%.
    [Fact]
    public void Compute_2025TaxCalcT12Withheld10000_isTheTaxAndTheSurchargeLessTheWithholding()
    {
        var assessment = Assessment2025(50_000.00m, 8_300.00m, new TaxPrepayments(10_000m, AdvancePayments.None));

        (assessment.FinalTax, assessment.Withheld, assessment.AdvancePayments, assessment.Balance).ShouldBe((12_536.23m, 10_000m, 0m, 2_536.23m));
    }

    [Fact]
    public void Compute_2025TaxCalcT12WithholdingBeyondTheTax_isNegativeToBeRefunded()
    {
        var assessment = Assessment2025(50_000.00m, 8_300.00m, new TaxPrepayments(20_000m, AdvancePayments.None));

        (assessment.FinalTax, assessment.Balance).ShouldBe((12_397.61m, -7_602.39m));
    }

    // Tax-Calc T13 of 2026-10-20: T12 with 810.72 of net rent from a home office let to the company.
    // The tax on the total, 11,951.38, and its municipal surcharge, 836.60, with T12's surcharge on the
    // pay alone, 138.62, less the 10,000 withheld: « SOLDE » 2,926.60, the rent adding 364.83 of
    // income tax and 25.54 of municipal surcharge to T12's 2,536.23.
    [Fact]
    public void Compute_2025TaxCalcT13PayAndRent_addsTheRentsTaxToTheBalance()
    {
        var assessment = Assessment2025(50_000.00m, 8_300.00m, new TaxPrepayments(10_000m, AdvancePayments.None), 810.72m);

        (assessment.FinalTax, assessment.Withheld, assessment.Balance).ShouldBe((12_926.60m, 10_000m, 2_926.60m));
    }

    // Tax-Calc T14 of 2026-10-20: 6,364.17 of tax on 28,843.72, 445.49 of municipal surcharge, no
    // credit for low earned income, and 388.80 of surcharge on the pay's tax alone, nothing withheld:
    // « SOLDE » 7,198.46.
    [Fact]
    public void Compute_2025TaxCalcT14PayAndRent_leavesNoCreditAndAllOfItToPay()
    {
        var assessment = Assessment2025(32_500.00m, 3_600.00m, new TaxPrepayments(0m, AdvancePayments.None), 810.72m);

        (assessment.FinalTax, assessment.Balance).ShouldBe((7_198.46m, 7_198.46m));
    }

    // No tax to set the 252.35 of credit against: all of it is refunded (art. 304, §2).
    [Fact]
    public void Compute_2025TaxCalcT8CreditAndNoTax_refundsTheCredit()
    {
        var assessment = Assessment2025(10_800.00m, 3_400.00m, TaxPrepayments.None);

        (assessment.FinalTax, assessment.Balance).ShouldBe((-252.35m, -252.35m));
    }

    // 11,333.96 and 793.38 of tax; 12,014 by 10 April avoids the surcharge and earns 90.11 of bonus,
    // and the payments come off what is left.
    [Fact]
    public void Compute_2026Pay50000TheWholeBaseBy10April_setsThePaymentsAgainstTheTaxLessTheBonus()
    {
        var assessment = Assessment2026(50_000m, 8_293.36m, new TaxPrepayments(0m, new AdvancePayments(12_014m, 0m, 0m, 0m)));

        (assessment.FinalTax, assessment.Withheld, assessment.AdvancePayments, assessment.Balance).ShouldBe((12_037.23m, 0m, 12_014m, 23.23m));
    }

    [Fact]
    public void Compute_2026Pay50000NothingWithheldOrPrepaid_addsTheSurchargeAndLeavesItAllToPay()
    {
        var assessment = Assessment2026(50_000m, 8_293.36m, TaxPrepayments.None);

        (assessment.FinalTax, assessment.Balance).ShouldBe((12_613.91m, 12_613.91m));
    }

    [Fact]
    public void Compute_2026_citesArticle304()
    {
        Assessment2026(50_000m, 8_293.36m, TaxPrepayments.None).Source.ShouldBe(PersonalTaxRules.AssessmentBalance);
    }

    // Whether the amount withheld was given or taken from the key formula is the caller's to say: the
    // balance is the same either way.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compute_withheldGivenOrDerived_saysWhichAndBalancesTheSame(bool withheldGiven)
    {
        var assessment = Assessment2026(50_000m, 8_293.36m, new TaxPrepayments(10_691.28m, AdvancePayments.None), withheldGiven);

        (assessment.WithheldGiven, assessment.Balance).ShouldBe((withheldGiven, 1_436.06m));
    }

    private static Assessment Assessment2025(decimal directorPay, decimal ownContributions, TaxPrepayments prepayments, decimal otherIncome = 0m) =>
        Assessment(Rules2025, directorPay, ownContributions, prepayments, true, otherIncome);

    private static Assessment Assessment2026(decimal directorPay, decimal ownContributions, TaxPrepayments prepayments, bool withheldGiven = true) =>
        Assessment(Rules2026, directorPay, ownContributions, prepayments, withheldGiven, 0m);

    // These tests give the amount withheld, so the surcharge takes it as it is. The tax is on the whole
    // income, the rent's included; the surcharge, on the pay's own tax less the credit granted.
    private static Assessment Assessment(
        PersonalTaxRules rules, decimal directorPay, decimal ownContributions, TaxPrepayments prepayments, bool withheldGiven, decimal otherIncome)
    {
        var withheld = prepayments.Withheld ?? throw new InvalidOperationException("These tests give the amount withheld.");
        var pay = PersonalTaxCalculator.Compute(rules, directorPay, ownContributions, 0.07m);
        var tax = PersonalTaxCalculator.Compute(rules, directorPay, ownContributions, 0.07m, otherIncome);
        var advancePayments = IncomeTaxAdvancePaymentCalculator.Compute(rules.AdvancePayments, pay.IncomeTax, tax.LowEarnedIncomeCredit, prepayments);

        return AssessmentCalculator.Compute(tax, advancePayments, withheld, withheldGiven, prepayments.Paid);
    }
}
