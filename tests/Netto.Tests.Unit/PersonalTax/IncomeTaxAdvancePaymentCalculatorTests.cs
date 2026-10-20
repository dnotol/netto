using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// The surcharge on the income tax on director pay and the bonus on advance payments. Income year 2025
// expected values are Tax-Calc « exercice 2026 » runs (IPP_EX_2026 V_1.8.0, single, 5000 at 7%, born
// 01/01/1985, pay 26,800, own contributions 3,500): T7 of 2026-10-19, T9 to T11 of 2026-10-20. Their
// total tax, 3,864.91, and credit for low earned income, 780.60, are PersonalTaxCalculatorTests'.
// Income year 2026 values are derived from the official amounts (4.50%, credits 6, 5, 4 and 3%,
// bonus 3, 2.5, 2 and 1.5%), as Tax-Calc does not cover 2026 yet; its 11,333.96 of tax is
// GrossToNetTests' on 50,000 of pay.
public class IncomeTaxAdvancePaymentCalculatorTests
{
    private static readonly IncomeTaxAdvancePaymentRules Rules2025 = PersonalTaxRules.IncomeYear2025.AdvancePayments;

    private static readonly IncomeTaxAdvancePaymentRules Rules2026 = PersonalTaxRules.IncomeYear2026.AdvancePayments;

    private const decimal TaxCalcTax = 3_864.91m;
    private const decimal TaxCalcCredit = 780.60m;
    private const decimal TaxOn50000 = 11_333.96m;

    [Fact]
    public void Compute_2025TaxCalcT7NothingWithheldOrPrepaid_surchargesTheBaseLessTheCredit()
    {
        var result = Compute2025(new TaxPrepayments(0m, AdvancePayments.None));

        (result.TaxAt106Percent, result.Base, result.SurchargeBeforeCredits, result.Surcharge).ShouldBe((4_096.80m, 3_316.20m, 223.84m, 201.46m));
    }

    [Fact]
    public void Compute_2025TaxCalcT9PaidByTheFirstDeadline_countsOnlyWhatAvoidsTheSurchargeAsNeeded()
    {
        var result = Compute2025(new TaxPrepayments(0m, new AdvancePayments(3_000m, 0m, 0m, 0m)));

        (result.Needed[0], result.Credits[0], result.Surcharge).ShouldBe((2_487.06m, 223.84m, 0m));
    }

    [Fact]
    public void Compute_2025TaxCalcT9PaidByTheFirstDeadline_earnsTheBonusOnTheRest()
    {
        var result = Compute2025(new TaxPrepayments(0m, new AdvancePayments(3_000m, 0m, 0m, 0m)));

        result.Bonus.ShouldBe(23.08m);
    }

    // The bonus base, 4,096.80 less the credit and the 2,487.06 needed, 829.14, is filled earliest
    // first: 512.94 at 4.50% and 316.20 at 2.25%.
    [Fact]
    public void Compute_2025TaxCalcT10PaidByTheFirstAndLastDeadlines_fillsTheBonusBaseEarliestFirst()
    {
        var result = Compute2025(new TaxPrepayments(0m, new AdvancePayments(3_000m, 0m, 0m, 3_000m)));

        (result.Bonuses[0], result.Bonuses[3], result.Bonus).ShouldBe((23.08m, 7.11m, 30.19m));
    }

    // 1,516.20 of base, 102.34 at 6.75%, 92.11 at 90%: under 100, so none is due.
    [Fact]
    public void Compute_2025TaxCalcT11Withheld1800_leavesASurchargeUnderTheMinimumSoNoneIsDue()
    {
        var result = Compute2025(new TaxPrepayments(1_800m, AdvancePayments.None));

        (result.Base, result.SurchargeBeforeKeptRate, result.BelowMinimum, result.Surcharge).ShouldBe((1_516.20m, 102.34m, true, 0m));
    }

    [Fact]
    public void Compute_2026Pay50000NothingWithheldOrPrepaid_surchargesItAt4Point5PercentKeptAt90()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, AdvancePayments.None));

        (result.Base, result.SurchargeBeforeCredits, result.Surcharge).ShouldBe((12_014.00m, 540.63m, 486.57m));
    }

    // 680.00 of base left, 30.60 at 4.50%, 27.54 at 90%.
    [Fact]
    public void Compute_2026AlmostAllWithheld_leavesASurchargeUnderTheMinimumSoNoneIsDue()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(11_334m, AdvancePayments.None));

        (result.Base, result.BelowMinimum, result.Surcharge).ShouldBe((680.00m, true, 0m));
    }

    [Fact]
    public void Compute_2026MoreWithheldThanTheTax_leavesNoBaseAndNoSurcharge()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(20_000m, AdvancePayments.None));

        (result.Base, result.Surcharge).ShouldBe((0m, 0m));
    }

    // 28,301.89 at 106% is 30,000.00, whose 0.5% is 150. 19,907.41 by 10 April earns 1,194.44, which
    // leaves 155.56 and 140.00 at 90%: above 100, under 150.
    [Fact]
    public void Compute_2026SurchargeAbove100UnderHalfAPercentOfTheBase_isNotDue()
    {
        var result = Compute2026(28_301.89m, new TaxPrepayments(0m, new AdvancePayments(19_907.41m, 0m, 0m, 0m)));

        (result.SurchargeBeforeKeptRate, result.BelowMinimum, result.Surcharge).ShouldBe((155.56m, true, 0m));
    }

    // 2,329.35 at 106% is 2,469.11; 111.11 at 4.50%, 100.00 at 90%: it reaches the minimum.
    [Fact]
    public void Compute_2026SurchargeOfExactly100_isDue()
    {
        var result = Compute2026(2_329.35m, new TaxPrepayments(0m, AdvancePayments.None));

        (result.BelowMinimum, result.Surcharge).ShouldBe((false, 100.00m));
    }

    [Fact]
    public void Compute_2026SamePaymentByEachDeadline_earnsLessCreditTheLaterItIsPaid()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, new AdvancePayments(1_000m, 1_000m, 1_000m, 1_000m)));

        result.Credits.ShouldBe([60.00m, 50.00m, 40.00m, 30.00m]);
    }

    [Fact]
    public void Compute_2026PaidMoreThanTheSurchargeNeeds_neverLeavesANegativeSurcharge()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, new AdvancePayments(50_000m, 0m, 0m, 0m)));

        (result.SurchargeBeforeKeptRate, result.Surcharge).ShouldBe((0m, 0m));
    }

    // 540.63 needs 9,010.42 by 10 April; the other 3,003.58 of the 12,014 earns 3%.
    [Fact]
    public void Compute_2026TheWholeBaseBy10April_earnsTheBonusOnWhatAvoidingTheSurchargeDoesNotNeed()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, new AdvancePayments(12_014m, 0m, 0m, 0m)));

        (result.Needed[0], result.Bonus).ShouldBe((9_010.42m, 90.11m));
    }

    // The bonus base is the 12,014.00 less the 9,010.42 needed: 3,003.58, whatever is paid beyond.
    [Fact]
    public void Compute_2026PaidFarBeyondTheTax_earnsTheBonusOnlyUpToItsBase()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, new AdvancePayments(50_000m, 0m, 0m, 0m)));

        result.Bonus.ShouldBe(90.11m);
    }

    [Fact]
    public void Compute_2026PaidTooLittleToAvoidTheSurcharge_earnsNoBonus()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, new AdvancePayments(1_000m, 0m, 0m, 0m)));

        result.Bonus.ShouldBe(0m);
    }

    // 540.63 less a credit of 429.53 leaves 111.10, 99.99 at 90%: under 100. A cent less, 14,317.49,
    // earns 429.52 and leaves 100.00, which is due.
    [Fact]
    public void Compute_2026NothingPrepaid_suggestsTheSmallestPaymentBy21DecemberAfterWhichNoneIsDue()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, AdvancePayments.None));

        result.LastDeadlineTopUp.ShouldBe(14_317.50m);
    }

    [Fact]
    public void Compute_2026SuggestedPaymentMade_leavesNoSurchargeAndSuggestsNothing()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, new AdvancePayments(0m, 0m, 0m, 14_317.50m)));

        (result.Surcharge, result.LastDeadlineTopUp).ShouldBe((0m, 0m));
    }

    [Fact]
    public void Compute_2026Starter_owesNoSurchargeAndNeedsNoPayment()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, AdvancePayments.None, Starter: true));

        (result.Exempt, result.Surcharge, result.LastDeadlineTopUp).ShouldBe((true, 0m, 0m));
    }

    // Nothing is needed to avoid a surcharge a starter does not owe: all of the payment earns 3%, up
    // to the 12,014.00 of base.
    [Fact]
    public void Compute_2026StarterPaying_earnsTheBonusOnAllOfIt()
    {
        var result = Compute2026(TaxOn50000, new TaxPrepayments(0m, new AdvancePayments(5_000m, 0m, 0m, 0m), Starter: true));

        result.Bonus.ShouldBe(150.00m);
    }

    [Fact]
    public void Compute_2026NoTax_leavesNoSurchargeAndNoBonus()
    {
        var result = Compute2026(0m, new TaxPrepayments(0m, new AdvancePayments(1_000m, 0m, 0m, 0m)));

        (result.Surcharge, result.Bonus).ShouldBe((0m, 0m));
    }

    // The surcharge needs the amount withheld: what the key formula withholds is GrossToNet's to set
    // before it, from the pay, so a withholding still left to the formula here is a caller's bug.
    [Fact]
    public void Compute_withholdingLeftToTheKeyFormula_isACallersBug()
    {
        Should.Throw<ArgumentException>(() => Compute2026(TaxOn50000, TaxPrepayments.AsWithheld));
    }

    private static IncomeTaxAdvancePaymentResult Compute2025(TaxPrepayments prepayments) =>
        IncomeTaxAdvancePaymentCalculator.Compute(Rules2025, TaxCalcTax, TaxCalcCredit, prepayments);

    private static IncomeTaxAdvancePaymentResult Compute2026(decimal totalTax, TaxPrepayments prepayments) =>
        IncomeTaxAdvancePaymentCalculator.Compute(Rules2026, totalTax, 0m, prepayments);
}
