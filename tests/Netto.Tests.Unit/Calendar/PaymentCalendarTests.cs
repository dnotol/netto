using Netto.Core.Calendar;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Calendar;

// The river's example year as its scenario (MoveSearchTests): 50,000 of pay withheld on as the key
// formula says, which leaves the director's surcharge under its minimum, and nothing prepaid by the
// company, whose 10,000 of corporate tax leaves a 675.00 surcharge: 4.5% of 14,999.89 by 21 December, or
// 9% of 7,499.95 by 10 April, cancels it, each credit rounding up to 675.00. With nothing withheld the director's surcharge is 486.57, and
// 14,317.50 by 21 December leaves none due (GrossToNetTests). Income year 2026's deadlines are 10
// April, 10 July, 12 October and 21 December.
public class PaymentCalendarTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    private static PaymentCalendar Of(Scenario scenario, DateOnly today) =>
        PaymentCalendar.Of(YearSimulation.Run(scenario).Value.ShouldNotBeNull(), today).ShouldNotBeNull();

    [Fact]
    public void Of_example_givesTheYearsFourDeadlinesInOrder()
    {
        var calendar = Of(RiverExample, On22October);

        calendar.Company.ShouldNotBeNull().Payments.Select(payment => payment.Deadline.Value).ShouldBe(
            [new DateOnly(2026, 4, 10), new DateOnly(2026, 7, 10), new DateOnly(2026, 10, 12), new DateOnly(2026, 12, 21)]);
    }

    [Fact]
    public void Of_exampleOn22October_cancelsTheCompanysSurchargeBy21December()
    {
        var company = Of(RiverExample, On22October).Company.ShouldNotBeNull();

        (company.Surcharge, company.Open, company.More).ShouldBe((675.00m, 3, 14_999.89m));
    }

    [Fact]
    public void Of_exampleOn22October_marksTheFirstThreeDeadlinesPast()
    {
        var company = Of(RiverExample, On22October).Company.ShouldNotBeNull();

        company.Payments.Select(payment => payment.Past).ShouldBe([true, true, true, false]);
    }

    // 9% of 5,000 leaves 225 of the 675, which 4.5% of 4,999.89 more by 21 December cancels: 224.99505,
    // rounded to 225.00.
    [Fact]
    public void Of_companyPaidBy10April_showsItAndWhatIsLeftBy21December()
    {
        var company = Of(RiverExample with { AdvancePayments = new AdvancePayments(5_000m, 0m, 0m, 0m) }, On22October).Company.ShouldNotBeNull();

        (company.Payments[0].Planned, company.Payments[0].More, company.Payments[3].More).ShouldBe((5_000m, 0m, 4_999.89m));
    }

    // Only the first deadline still open says what more cancels it.
    [Fact]
    public void Of_on1March_cancelsTheSurchargeByTheFirstDeadline()
    {
        var company = Of(RiverExample, new DateOnly(2026, 3, 1)).Company.ShouldNotBeNull();

        company.Payments.Select(payment => payment.More).ShouldBe([7_499.95m, 0m, 0m, 0m]);
    }

    // A deadline is open until the end of its day.
    [Fact]
    public void Of_onTheLastDeadline_stillCancelsTheSurchargeByIt()
    {
        var company = Of(RiverExample, new DateOnly(2026, 12, 21)).Company.ShouldNotBeNull();

        (company.Open, company.More).ShouldBe((3, 14_999.89m));
    }

    [Fact]
    public void Of_afterTheLastDeadline_leavesTheSurchargeWithNothingMore()
    {
        var company = Of(RiverExample, new DateOnly(2026, 12, 22)).Company.ShouldNotBeNull();

        (company.Surcharge, company.Open, company.More, company.Payments.All(payment => payment.Past)).ShouldBe((675.00m, null, 0m, true));
    }

    [Fact]
    public void Of_smallCompanyInItsThirdFinancialYear_isExemptWithNothingMore()
    {
        var company = Of(RiverExample with { Company = new CompanyStatus(true, true, true) }, On22October).Company.ShouldNotBeNull();

        (company.Exempt, company.Surcharge, company.More).ShouldBe((true, 0m, 0m));
    }

    [Fact]
    public void Of_nothingWithheld_leavesNoDirectorsSurchargeDueWith14317Point50By21December()
    {
        var director = Of(RiverExample with { TaxPrepayments = TaxPrepayments.None }, On22October).Director;

        (director.Surcharge, director.Open, director.More).ShouldBe((486.57m, 3, 14_317.50m));
    }

    [Fact]
    public void Of_exampleWithheldOnByTheKeyFormula_needsNothingMoreFromTheDirector()
    {
        var director = Of(RiverExample, On22October).Director;

        (director.Surcharge, director.More).ShouldBe((0m, 0m));
    }

    [Fact]
    public void Of_starter_isExemptWithNothingMore()
    {
        var director = Of(RiverExample with { TaxPrepayments = TaxPrepayments.None with { Starter = true } }, On22October).Director;

        (director.Exempt, director.Surcharge, director.More).ShouldBe((true, 0m, 0m));
    }

    [Fact]
    public void Of_directorsPayments_showWhatThePlanPays()
    {
        var paid = new AdvancePayments(1_000m, 0m, 2_000m, 0m);

        var director = Of(RiverExample with { TaxPrepayments = TaxPrepayments.None with { Paid = paid } }, On22October).Director;

        director.Payments.Select(payment => payment.Planned).ShouldBe([1_000m, 0m, 2_000m, 0m]);
    }

    // What the moves propose by a deadline is the calendar's figure (MoveSearch's finders read the same).
    [Fact]
    public void Of_example_givesWhatTheMovesProposeByTheOpenDeadline()
    {
        var today = RiverExample with { TaxPrepayments = TaxPrepayments.None };
        var calendar = Of(today, On22October);

        var finders = new IMoveFinder[] { new CompanyAdvancePaymentFinder(On22October), new DirectorAdvancePaymentFinder(On22October) };

        finders.Select(finder => finder.Find(today)).ShouldBe([
            new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), calendar.Company.ShouldNotBeNull().More),
            new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), calendar.Director.More),
        ]);
    }

    // A river that starts at the pay: the director's payments alone.
    [Fact]
    public void Of_payAlone_hasNoCompanyAndTheDirectorsPayments()
    {
        var pay = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.None).Value.ShouldNotBeNull();

        var calendar = PaymentCalendar.Of(pay, AdvancePayments.None, On22October).ShouldNotBeNull();

        (calendar.Company, calendar.Director.More).ShouldBe((null, 14_317.50m));
    }

    [Fact]
    public void Of_example_givesTheTwelveMonthsOfThePaySlip()
    {
        var year = YearSimulation.Run(RiverExample).Value.ShouldNotBeNull();

        var calendar = PaymentCalendar.Of(year, On22October).ShouldNotBeNull();

        calendar.Months.Select(month => month.Pay).ShouldBe(year.DirectorPay.Statement.Months);
    }

    // A month is past once it has ended: October is still under way on the 22nd.
    [Fact]
    public void Of_exampleOn22October_marksJanuaryToSeptemberPast()
    {
        var calendar = Of(RiverExample, On22October);

        calendar.Months.Select(month => month.Past).ShouldBe([.. Enumerable.Repeat(true, 9), false, false, false]);
    }

    // The example's 12,127.34 of final tax less the 10,674.34 its twelve months withhold (GrossToNetTests).
    [Fact]
    public void Of_example_givesTheDirectorsBalanceForTaxYear2027()
    {
        var director = Of(RiverExample, On22October).DirectorBalance;

        (director.TaxYear, director.Balance, director.Source).ShouldBe((2027, 1_453.00m, PersonalTaxRules.IncomeYear2026.AssessmentBalance));
    }

    // November and December withhold by the rules from 1 November: 882.47 on the example's 4,166.67.
    [Fact]
    public void Of_example_paysNovemberAndDecemberByTheRulesFrom1November()
    {
        var calendar = Of(RiverExample, On22October);

        calendar.Months.Select(month => month.Pay.Withholding).ShouldBe([.. Enumerable.Repeat(890.94m, 10), 882.47m, 882.47m]);
    }

    // 10,000 of corporate tax and its 675 surcharge, nothing paid in advance.
    [Fact]
    public void Of_example_givesTheCompanysBalanceItsTaxWithTheSurcharge()
    {
        var company = Of(RiverExample, On22October).CompanyBalance.ShouldNotBeNull();

        (company.TaxYear, company.Balance, company.Source).ShouldBe((2027, 10_675.00m, CorporateTaxRules.IncomeYear2026.AssessmentBalance));
    }

    // 15,000 by 10 April leaves no surcharge, and 5,000 more than the 10,000 of tax comes back.
    [Fact]
    public void Of_companyPaidMoreThanItsTax_refundsTheExcess()
    {
        var company = Of(RiverExample with { AdvancePayments = new AdvancePayments(15_000m, 0m, 0m, 0m) }, On22October).CompanyBalance.ShouldNotBeNull();

        company.Balance.ShouldBe(-5_000.00m);
    }

    // The contributions on the example's pay are billed as four rounded quarters (GrossToNet).
    [Fact]
    public void Of_example_givesAQuarterOfTheYearsContributions()
    {
        var year = YearSimulation.Run(RiverExample).Value.ShouldNotBeNull();

        var contributions = PaymentCalendar.Of(year, On22October).ShouldNotBeNull().Contributions;

        (contributions.EachQuarter * 4, contributions.PaidByCompany, contributions.Billed, contributions.Regularisation)
            .ShouldBe((year.DirectorPay.SocialContributions, false, null, null));
    }

    // The fund bills its fee with each quarter of the contributions (AR n° 38, art. 20 §4), each rounded
    // to the cent, so a quarter is a quarter of both to the cent: 2,062.90 and 62.92 on 50,000 at 3.05%
    // (GrossToNetFundFeeTests).
    [Fact]
    public void Of_feeRate_givesAQuarterOfTheContributionsAndTheFeeToTheCent()
    {
        var year = YearSimulation.Run(RiverExample with { FundFeeRate = 0.0305m }).Value.ShouldNotBeNull();

        var contributions = PaymentCalendar.Of(year, On22October).ShouldNotBeNull().Contributions;

        (contributions.EachQuarter, contributions.EachQuarter * 4).ShouldBe((2_062.90m + 62.92m, year.DirectorPay.SocialContributions + year.DirectorPay.FundFee));
    }

    // The quarter says how much of it is the fee, for When to pay to say the fee is in it; nothing without
    // a rate.
    [Theory]
    [InlineData(null, 0)]
    [InlineData(0.0305, 62.92)]
    public void Of_feeRateOrNone_saysTheFeeInEachQuarter(double? rate, double fee)
    {
        var year = YearSimulation.Run(RiverExample with { FundFeeRate = (decimal?)rate }).Value.ShouldNotBeNull();

        PaymentCalendar.Of(year, On22October).ShouldNotBeNull().Contributions.FundFeeEachQuarter.ShouldBe((decimal)fee);
    }

    // What the fund bills each quarter carries its fee, so the regularisation is against the year's
    // contributions with theirs.
    [Fact]
    public void WithBilled_feeRate_regularisesAgainstTheContributionsAndTheFee()
    {
        var year = YearSimulation.Run(RiverExample with { FundFeeRate = 0.0305m }).Value.ShouldNotBeNull();
        var calendar = PaymentCalendar.Of(year, On22October).ShouldNotBeNull();

        var contributions = calendar.WithBilled(2_000m).Value.ShouldNotBeNull().Contributions;

        contributions.Regularisation.ShouldBe(year.DirectorPay.SocialContributions + year.DirectorPay.FundFee - 8_000m);
    }

    // A quarter is past once it has ended: the fourth is under way on 22 October.
    [Fact]
    public void Of_exampleOn22October_marksTheFirstThreeQuartersPast()
    {
        Of(RiverExample, On22October).Contributions.Quarters.Select(quarter => quarter.Past).ShouldBe([true, true, true, false]);
    }

    [Fact]
    public void Of_companyPaysTheContributions_saysSo()
    {
        Of(RiverExample with { ContributionsPaidBy = Netto.Core.SocialContributions.ContributionsPaidBy.Company }, On22October).Contributions.PaidByCompany.ShouldBeTrue();
    }

    // What the fund bills each quarter, four times, against the final contribution: the rest is
    // regularised, to pay when the bills fall short.
    [Fact]
    public void WithBilled_lessThanTheFinal_leavesTheRestToPay()
    {
        var calendar = Of(RiverExample, On22October);
        var final = calendar.Contributions.EachQuarter * 4;

        var contributions = calendar.WithBilled(2_000m).Value.ShouldNotBeNull().Contributions;

        (contributions.Billed, contributions.Regularisation).ShouldBe((2_000m, final - 8_000m));
    }

    [Fact]
    public void WithBilled_moreThanTheFinal_refundsTheExcess()
    {
        var calendar = Of(RiverExample, On22October);
        var final = calendar.Contributions.EachQuarter * 4;

        calendar.WithBilled(5_000m).Value.ShouldNotBeNull().Contributions.Regularisation.ShouldBe(final - 20_000m);
    }

    [Fact]
    public void WithBilled_none_leavesNoRegularisation()
    {
        Of(RiverExample, On22October).WithBilled(null).Value.ShouldNotBeNull().Contributions.Regularisation.ShouldBeNull();
    }

    [Fact]
    public void WithBilled_negative_isRefused()
    {
        Of(RiverExample, On22October).WithBilled(-1m).ShouldBe((null, PaymentCalendar.BilledContributionsNegative));
    }

    [Fact]
    public void Of_payAlone_hasTheDirectorsBalanceAlone()
    {
        var pay = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.AsWithheld).Value.ShouldNotBeNull();

        var calendar = PaymentCalendar.Of(pay, AdvancePayments.None, On22October).ShouldNotBeNull();

        (calendar.CompanyBalance, calendar.DirectorBalance.Balance).ShouldBe((null, pay.Assessment.Balance));
    }

    // No rules for the year, no calendar: none, as every rule lookup gives, never an exception.
    [Fact]
    public void Of_yearWithoutRules_isNone()
    {
        var year = YearSimulation.Run(RiverExample).Value.ShouldNotBeNull();

        PaymentCalendar.Of(year with { Scenario = year.Scenario with { IncomeYear = UnsupportedIncomeYear } }, On22October).ShouldBeNull();
    }

    [Fact]
    public void Of_payOfAYearWithoutRules_isNone()
    {
        var pay = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.AsWithheld).Value.ShouldNotBeNull();

        PaymentCalendar.Of(pay with { IncomeYear = UnsupportedIncomeYear }, AdvancePayments.None, On22October).ShouldBeNull();
    }

    // The director's deadlines are their region's rules': none for a region Netto has no rules for.
    [Fact]
    public void Of_payOfARegionWithoutRules_isNone()
    {
        var pay = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.AsWithheld).Value.ShouldNotBeNull();

        PaymentCalendar.Of(pay with { Region = Region.Flanders }, AdvancePayments.None, On22October).ShouldBeNull();
    }
}
