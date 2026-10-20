using Netto.Core.CorporateTax;
using Shouldly;

namespace Netto.Tests.CorporateTax;

// Income year 2026 (CIR 92 art. 218 and 159 for income 2026, the SPF page on companies' advance
// payments for tax year 2027): the surcharge is 6.75% of the corporate tax, less 9, 7.5, 6 and 4.5%
// of what is paid by 10 April, 10 July, 12 October and 21 December. Made-up companies; the 20,000
// of tax is the Lever catalogue's worked example, the other figures are derived from the articles.
public class AdvancePaymentCalculatorTests
{
    private static readonly AdvancePaymentRules Rules2026 = CorporateTaxRules.IncomeYear2026.AdvancePayments;

    private static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

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

    // 1,350 less 450 and 375 leaves 525, which 525 ÷ 4.5% = 11,666.666… cancels, rounded up to the
    // cent so that its credit, 525.00015, rounds to 525.00.
    [Fact]
    public void Compute_2026PaidByAprilAndJulyOnly_suggestsWhatBy21DecemberCancelsTheRest()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(5_000m, 5_000m, 0m, 0m), SmallPastStartUp);

        (result.Surcharge, result.LastDeadlineTopUp).ShouldBe((525.00m, 11_666.67m));
    }

    [Fact]
    public void Compute_2026SuggestedPaymentMade_leavesNoSurchargeAndSuggestsNothing()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(5_000m, 5_000m, 0m, 11_666.67m), SmallPastStartUp);

        (result.Surcharge, result.LastDeadlineTopUp).ShouldBe((0m, 0m));
    }

    // What is already paid by 21 December counts: 11,666.67 is needed by then, 1,000 is there.
    [Fact]
    public void Compute_2026PartPaidBy21December_suggestsWhatIsMissing()
    {
        var result = AdvancePaymentCalculator.Compute(Rules2026, 20_000m, new AdvancePayments(5_000m, 5_000m, 0m, 1_000m), SmallPastStartUp);

        result.LastDeadlineTopUp.ShouldBe(10_666.67m);
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
}
