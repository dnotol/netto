using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The example prepays nothing and withholds on its pay as the key formula says: on 22 October 2026 the
// company's 675.00 surcharge is cancelled by 14,999.89 more by 21 December, and the director's falls
// under its minimum. With nothing withheld the director's 486.57 needs 14,317.50 by 21 December
// (PaymentCalendarTests).
public class CalendarViewTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    private static readonly DateOnly On21December = new(2026, 12, 21);

    private static CalendarView View(RiverInputs inputs, DateOnly today) => CalendarView.From(inputs, inputs.Run(), today).ShouldNotBeNull();

    [Fact]
    public void From_example_listsOnly21DecemberWithTheCompanysPayment()
    {
        var view = View(RiverInputs.Example, On22October);

        view.Rows.ShouldBe([new CalendarRow(On21December, false, 0m, 14_999.89m, 0m, 0m)]);
    }

    [Fact]
    public void From_companyPaidBy10April_listsItPastBeforeWhatIsLeft()
    {
        var view = View(RiverInputs.Example.WithAdvancePayment(0, 5_000m), On22October);

        view.Rows.ShouldBe([new CalendarRow(new DateOnly(2026, 4, 10), true, 5_000m, 0m, 0m, 0m), new CalendarRow(On21December, false, 0m, 4_999.89m, 0m, 0m)]);
    }

    [Fact]
    public void From_nothingWithheld_putsTheDirectorsPaymentBesideTheCompanys()
    {
        var view = View(RiverInputs.Example.WithTaxWithheld(0m), On22October);

        view.Rows.ShouldBe([new CalendarRow(On21December, false, 0m, 14_999.89m, 0m, 14_317.50m)]);
    }

    [Fact]
    public void From_example_saysWhatEachTaxsPaymentsLeave()
    {
        var view = View(RiverInputs.Example, On22October);

        (view.Company, view.Director).ShouldBe((
            new CalendarTax<SurchargeState>(SurchargeState.Due, 675.00m, 14_999.89m, On21December),
            new CalendarTax<TaxSurchargeState>(TaxSurchargeState.BelowMinimum, 0m, 0m, On21December)));
    }

    [Fact]
    public void From_example_nextIs21December()
    {
        View(RiverInputs.Example, On22October).Next.ShouldBe(new CalendarRow(On21December, false, 0m, 14_999.89m, 0m, 0m));
    }

    [Fact]
    public void From_afterTheYear_listsNoRowAndSaysItIsTooLate()
    {
        var view = View(RiverInputs.Example, new DateOnly(2026, 12, 22));

        (view.Rows.Count, view.Next, view.Company.ShouldNotBeNull().TooLate).ShouldBe((0, null, true));
    }

    [Fact]
    public void From_youngSmallCompany_needsNothingFromIt()
    {
        var view = View(RiverInputs.Example.WithFinancialYear(FinancialYear.Second), On22October);

        (view.Rows.Count, view.Company.ShouldNotBeNull().State).ShouldBe((0, SurchargeState.Exempt));
    }

    [Fact]
    public void From_riverStartingAtThePay_hasTheDirectorAlone()
    {
        var view = View(RiverInputs.Example.SkipCompany().WithTaxWithheld(0m), On22October);

        (view.Company, view.Rows.Single()).ShouldBe((null, new CalendarRow(On21December, false, 0m, 0m, 0m, 14_317.50m)));
    }

    // The pay slip's months (GrossToNetViewTests): 4,166.67 gross, 890.94 withheld to October and 882.47
    // from November, by the rules from 1 November.
    [Fact]
    public void From_example_givesThePaySlipsMonthsInRunsThatPayTheSame()
    {
        var pay = View(RiverInputs.Example, On22October).Pay;

        pay.Runs.ShouldBe(
        [
            new PayRun(new DateOnly(2026, 1, 1), new DateOnly(2026, 10, 1), 4_166.67m, 890.94m, 3_275.73m),
            new PayRun(new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 1), 4_166.67m, 882.47m, 3_284.20m),
        ]);
    }

    [Fact]
    public void From_example_givesTheMonthsLeft()
    {
        var pay = View(RiverInputs.Example, On22October).Pay;

        (pay.Left, pay.WithheldGiven).ShouldBe(((new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 1)), null));
    }

    // 300 a month is under the 385 the formula takes off any pay: both periods withhold nothing, so the
    // year is one run.
    [Fact]
    public void From_payBothPeriodsWithholdNothingOn_isOneRun()
    {
        var pay = View(RiverInputs.Example.WithDirectorPay(3_600m), On22October).Pay;

        pay.Runs.ShouldBe([new PayRun(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 1), 300m, 0m, 300m)]);
    }

    [Fact]
    public void From_withholdingGiven_saysTheYearsAmount()
    {
        View(RiverInputs.Example.WithTaxWithheld(9_000m), On22October).Pay.WithheldGiven.ShouldBe(9_000m);
    }

    [Fact]
    public void From_afterTheYear_hasNoMonthLeft()
    {
        View(RiverInputs.Example, new DateOnly(2027, 1, 1)).Pay.Left.ShouldBeNull();
    }

    [Fact]
    public void From_example_givesBothBalancesForTaxYear2027()
    {
        var view = View(RiverInputs.Example, On22October);

        (view.CompanyBalance?.Balance, view.DirectorBalance.Balance, view.DirectorBalance.TaxYear).ShouldBe((10_675.00m, 1_453.00m, 2027));
    }

    [Fact]
    public void From_riverStartingAtThePay_hasNoCompanyBalance()
    {
        View(RiverInputs.Example.SkipCompany(), On22October).CompanyBalance.ShouldBeNull();
    }

    [Fact]
    public void From_exampleOn22October_hasOneQuarterLeftAndNothingBilled()
    {
        var view = View(RiverInputs.Example, On22October);

        (view.QuartersLeft, view.Contributions.Billed, view.Contributions.Regularisation, view.BilledRefused).ShouldBe((1, null, null, null));
    }

    [Fact]
    public void From_billedGiven_regularisesWhatTheYearLeavesOverIt()
    {
        var view = View(RiverInputs.Example.WithBilledContributions(2_000m), On22October);

        view.Contributions.Regularisation.ShouldBe(view.Contributions.EachQuarter * 4 - 8_000m);
    }

    // A refused bill leaves the rest of the calendar, with no regularisation.
    [Fact]
    public void From_negativeBilled_isRefusedOnTheCard()
    {
        var view = View(RiverInputs.Example.WithBilledContributions(-1m), On22October);

        (view.BilledRefused, view.Contributions.Regularisation, view.Rows.Count).ShouldBe((Netto.Core.Calendar.PaymentCalendar.BilledContributionsNegative, null, 1));
    }

    [Fact]
    public void From_refusedInput_isNone()
    {
        var inputs = RiverInputs.Example.WithReserve(-1m);

        CalendarView.From(inputs, inputs.Run(), On22October).ShouldBeNull();
    }
}
