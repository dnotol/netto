using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.CorporateTax;

// Income year 2026 (CIR 92 art. 218 and 159 for income 2026, the SPF page on companies' advance
// payments for tax year 2027): the surcharge is 6.75% of the corporate tax, less 9, 7.5, 6 and 4.5%
// of what is paid by 10 April, 10 July, 12 October and 21 December. Made-up companies; the 20,000
// of tax is the Lever catalogue's worked example, the other figures are derived from the articles.
public class AdvancePaymentCalculatorTests
{
    private static readonly AdvancePaymentRules Rules2026 = CorporateTaxRules.IncomeYear2026.AdvancePayments;

    [Fact]
    public void Compute_2026NothingPaid_surchargesTheTaxAt6Point75Percent()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, AdvancePayments.None, SmallPastStartUp);

        (result.SurchargeBeforeCredits, result.Surcharge).ShouldBe((1_350.00m, 1_350.00m));
    }

    [Fact]
    public void Compute_2026SeventyFivePercentPaidBy10April_leavesNoSurcharge()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(15_000m, 0m, 0m, 0m), SmallPastStartUp);

        result.Surcharge.ShouldBe(0m);
    }

    [Fact]
    public void Compute_2026AllOfTheTaxPaidBy21December_leavesAThirdOfTheSurcharge()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(0m, 0m, 0m, 20_000m), SmallPastStartUp);

        result.Surcharge.ShouldBe(450.00m);
    }

    [Fact]
    public void Compute_2026AQuarterOfTheTaxByEachDeadline_leavesNoSurcharge()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(5_000m, 5_000m, 5_000m, 5_000m), SmallPastStartUp);

        result.Surcharge.ShouldBe(0m);
    }

    [Fact]
    public void Compute_2026_creditsEachPaymentAtItsDeadlinesRate()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(5_000m, 5_000m, 5_000m, 5_000m), SmallPastStartUp);

        result.Credits.ShouldBe([450.00m, 375.00m, 300.00m, 225.00m]);
    }

    // Companies get no bonus for paying more than the surcharge needs: the excess is refunded.
    [Fact]
    public void Compute_2026PaidMoreThanTheSurchargeNeeds_leavesNoSurchargeAndNoBonus()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(100_000m, 0m, 0m, 0m), SmallPastStartUp);

        result.Surcharge.ShouldBe(0m);
    }

    // 1,350 less 450 and 375 leaves 525, which 4.5% of 11,666.56 cancels: its credit, 524.9952, rounds
    // half away from zero to 525.00, and a cent less, 524.99475, to 524.99. The smallest payment in cents
    // whose rounded credit covers what is left, as the director's (IncomeTaxAdvancePaymentCalculator).
    [Fact]
    public void Compute_2026PaidByAprilAndJulyOnly_suggestsWhatBy21DecemberCancelsTheRest()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(5_000m, 5_000m, 0m, 0m), SmallPastStartUp);

        (result.Surcharge, result.LastDeadlineTopUp).ShouldBe((525.00m, 11_666.56m));
    }

    [Fact]
    public void Compute_2026SuggestedPaymentMade_leavesNoSurchargeAndSuggestsNothing()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(5_000m, 5_000m, 0m, 11_666.56m), SmallPastStartUp);

        (result.Surcharge, result.LastDeadlineTopUp).ShouldBe((0m, 0m));
    }

    // What is already paid by 21 December counts: 11,666.56 is needed by then, 1,000 is there.
    [Fact]
    public void Compute_2026PartPaidBy21December_suggestsWhatIsMissing()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(5_000m, 5_000m, 0m, 1_000m), SmallPastStartUp);

        result.LastDeadlineTopUp.ShouldBe(10_666.56m);
    }

    // Companies have no de-minimis: 6.75% of 100 is still due.
    [Fact]
    public void Compute_2026SmallTax_isStillSurcharged()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 100m, AdvancePayments.None, SmallPastStartUp);

        result.Surcharge.ShouldBe(6.75m);
    }

    // 6.75% × 12,345.67 = 833.332725.
    [Fact]
    public void Compute_2026_roundsTheSurchargeToTheCent()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 12_345.67m, AdvancePayments.None, SmallPastStartUp);

        result.Surcharge.ShouldBe(833.33m);
    }

    // 4.5% × 1.00 = 0.045, which rounds half away from zero to 0.05 (half to even gives 0.04).
    [Fact]
    public void Compute_2026_roundsEachCreditHalfAwayFromZero()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 100m, new AdvancePayments(0m, 0m, 0m, 1.00m), SmallPastStartUp);

        (result.Credits[3], result.Surcharge).ShouldBe((0.05m, 6.70m));
    }

    [Fact]
    public void Compute_2026NoTax_leavesNoSurcharge()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 0m, AdvancePayments.None, SmallPastStartUp);

        (result.Surcharge, result.LastDeadlineTopUp).ShouldBe((0m, 0m));
    }

    [Fact]
    public void Compute_2026SmallCompanyInItsFirstThreeFinancialYears_isExempt()
    {
        var young = new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: true, FirstThreeFinancialYears: true);

        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, AdvancePayments.None, young);

        (result.Exempt, result.Surcharge, result.LastDeadlineTopUp).ShouldBe((true, 0m, 0m));
    }

    // The exemption of art. 218 §2 is a small company's.
    [Fact]
    public void Compute_2026CompanyNotSmallInItsFirstThreeFinancialYears_isSurcharged()
    {
        var young = new CompanyStatus(SmallCompany: false, FirstFourTaxPeriods: true, FirstThreeFinancialYears: true);

        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, AdvancePayments.None, young);

        (result.Exempt, result.Surcharge).ShouldBe((false, 1_350.00m));
    }

    [Fact]
    public void Compute_2026SmallCompanyInItsFourthFinancialYear_isSurcharged()
    {
        var fourth = new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: true, FirstThreeFinancialYears: false);

        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, AdvancePayments.None, fourth);

        result.Surcharge.ShouldBe(1_350.00m);
    }

    // What cancels the 1,350 of surcharge on 20,000 of tax, paid by one deadline alone: its credit,
    // 9% by 10 April, 4.5% by 21 December, rounds to it, 9% of 14,999.95 being 1,349.9955.
    [Theory]
    [InlineData(0, 14_999.95)]
    [InlineData(1, 17_999.94)]
    [InlineData(2, 22_499.92)]
    [InlineData(3, 29_999.89)]
    public void TopUpBy_2026NothingPaid_isWhatCancelsTheSurchargeByThatDeadline(int deadline, double expected)
    {
        var topUp = AdvancePaymentCalculator.TopUpBy(Rules2026, 20_000m, AdvancePayments.None, SmallPastStartUp, deadline);

        topUp.ShouldBe((decimal)expected);
    }

    // The payments made earn their credits: by 10 April what the 5,000 of 10 July leave, 975 of
    // 1,350, which 9% of 10,833.28 covers, 974.9952 rounding to 975.00.
    [Fact]
    public void TopUpBy_2026SomePaid_isWhatCancelsWhatTheOthersLeave()
    {
        var topUp = AdvancePaymentCalculator.TopUpBy(Rules2026, 20_000m, new AdvancePayments(0m, 5_000m, 0m, 0m), SmallPastStartUp, 0);

        topUp.ShouldBe(10_833.28m);
    }

    // The last deadline's is the one the card shows (LastDeadlineTopUp).
    [Fact]
    public void TopUpBy_lastDeadline_isTheLastDeadlinesTopUp()
    {
        var payments = new AdvancePayments(3_000m, 0m, 0m, 0m);

        AdvancePaymentCalculator.TopUpBy(Rules2026, 20_000m, payments, SmallPastStartUp, 3)
            .ShouldBe(AdvancePaymentCalculator.Compute(Rules2026, 20_000m, payments, SmallPastStartUp).LastDeadlineTopUp);
    }

    // The result gives each deadline's, so the calendar and the moves read the same figure: the 5,000
    // of 10 July leave 975 of the 1,350 at 9, 6 and 4.5%; by 10 July itself, the whole 1,350 at 7.5%
    // less the 5,000 already paid.
    [Fact]
    public void Compute_surchargeLeft_givesWhatCancelsItByEachDeadline()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(0m, 5_000m, 0m, 0m), SmallPastStartUp);

        result.TopUps.ShouldBe([10_833.28m, 12_999.94m, 16_249.92m, 21_666.56m]);
    }

    [Fact]
    public void Compute_noSurcharge_givesNothingByEveryDeadline()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(15_000m, 0m, 0m, 0m), SmallPastStartUp);

        result.TopUps.ShouldBe([0m, 0m, 0m, 0m]);
    }

    [Fact]
    public void TopUpBy_noSurcharge_isNothing()
    {
        AdvancePaymentCalculator.TopUpBy(Rules2026, 20_000m, new AdvancePayments(15_000m, 0m, 0m, 0m), SmallPastStartUp, 2).ShouldBe(0m);
    }

    // Over taxes from 123.45 to 49,380.00 and payments already made by none, one or two deadlines: paying a
    // deadline's top-up by it leaves no surcharge, and paying a cent less by it leaves one, so the top-up is
    // never more than the surcharge needs.
    [Fact]
    public void Compute_surchargeLeft_eachTopUpIsTheSmallestPaymentThatCancelsIt()
    {
        AdvancePayments[] paid = [AdvancePayments.None, new(0m, 5_000m, 0m, 0m), new(3_000m, 0m, 0m, 1_000m), new(1_234.56m, 0m, 2_345.67m, 0m)];
        var cases = (
            from tax in Enumerable.Range(1, 400).Select(i => i * 123.45m)
            from payments in paid
            from deadline in Enumerable.Range(0, 4)
            let topUp = AdvancePaymentCalculator.Compute(Rules2026, tax, payments, SmallPastStartUp).TopUps[deadline]
            where topUp > 0m
            select (Tax: tax, Payments: payments, Deadline: deadline, TopUp: topUp)).ToList();

        var wrong = cases
            .Where(@case => SurchargeAfter(@case.Tax, @case.Payments.Add(@case.Deadline, @case.TopUp)) != 0m
                || SurchargeAfter(@case.Tax, @case.Payments.Add(@case.Deadline, @case.TopUp - 0.01m)) == 0m)
            .ToList();

        cases.Count.ShouldBeGreaterThan(4_000);
        wrong.ShouldBeEmpty();
    }

    [Fact]
    public void TopUpBy_exemptCompany_isNothing()
    {
        AdvancePaymentCalculator.TopUpBy(Rules2026, 20_000m, AdvancePayments.None, new CompanyStatus(true, true, true), 0).ShouldBe(0m);
    }

    private static decimal SurchargeAfter(decimal tax, AdvancePayments payments) =>
        AdvancePaymentCalculator.Compute(Rules2026, tax, payments, SmallPastStartUp).Surcharge;
}
