using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.DirectorPay;

// The pay statement of a director taxed jointly with a partner who has no professional income: each
// month withheld on by the marital quotient of annex III n° 34 (WithholdingCalculatorQuotientTests), by
// January's rules to October and those from 1 November in November and December; the single method
// for a partner with income of their own or a year taxed separately (WithholdingHouseholdsTests).
// Income year 2026 at a 7% municipal rate, the formula's withholding and nothing prepaid. Expected
// values are derived from the official amounts by the SPF simulator's chain, Tax-Calc for income year
// 2026 not being online: 50,000 over twelve is 4,166.67, on which the quotient withholds 457.86 to
// October and 440.92 from November, the single method 890.94 and 882.47.
public class GrossToNetWithholdingQuotientTests
{
    [Fact]
    public void Compute_2026Gross50000PartnerWithoutIncome_withholdsByTheQuotientInEachPeriod()
    {
        var statement = Compute(50_000m, NonEarningPartner).Statement;

        statement.Months.Select(month => (month.Withholding, month.NetPaid)).ShouldBe(
        [
            .. Enumerable.Repeat((457.86m, 3_708.81m), 10),
            .. Enumerable.Repeat((440.92m, 3_725.75m), 2),
        ]);
    }

    // 10 × 457.86 + 2 × 440.92, where the single method withholds 10,674.34.
    [Fact]
    public void Compute_2026Gross50000PartnerWithoutIncome_assessmentCreditsTheQuotientsWithholding()
    {
        Compute(50_000m, NonEarningPartner).Assessment.Withheld.ShouldBe(5_460.44m);
    }

    // What the advance payments' surcharge counts as withheld is that same withholding, as if typed.
    [Fact]
    public void Compute_2026Gross50000PartnerWithoutIncome_advancePaymentsCountTheQuotientsWithholding()
    {
        var pay = Compute(50_000m, NonEarningPartner);

        var typed = Compute(50_000m, NonEarningPartner, new TaxPrepayments(5_460.44m, AdvancePayments.None));

        (pay.AdvancePayments.Base, pay.AdvancePayments.Surcharge).ShouldBe((typed.AdvancePayments.Base, typed.AdvancePayments.Surcharge));
    }

    // Each period carries the quotient's lines and cites the amounts it applied.
    [Fact]
    public void Compute_2026Gross50000PartnerWithoutIncome_periodsCarryTheQuotientAndCiteItsAmounts()
    {
        var periods = Compute(50_000m, NonEarningPartner).Statement.Periods;

        periods.ShouldSatisfyAllConditions(
            () => periods.Select(period => (period.Withholding.Household, period.Withholding.ImputedToPartner, period.Withholding.PartnerTax))
                .ShouldBe([(WithholdingHousehold.PartnerWithoutIncome, 11_177.23m, 2_989.91m), (WithholdingHousehold.PartnerWithoutIncome, 11_177.23m, 2_989.91m)]),
            () => periods.Select(period => period.AmountsUsed).ShouldBe(periods.Select(period => period.Rules.AmountsFor(WithholdingHousehold.PartnerWithoutIncome))));
    }

    // 5,000 a month imputes 13,461.56 to October, under its 13,790; from November 12,790 for partners
    // both 66, where any other couple's 11,780 withholds 720.45.
    [Fact]
    public void Compute_2026Gross60000PartnersBothAged66_withholdsByTheirMaximumFromNovember()
    {
        var household = NonEarningPartner with { BothAged66 = true };

        var statement = Compute(60_000m, household).Statement;

        statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(707.41m, 10), .. Enumerable.Repeat(702.44m, 2)]);
    }

    [Fact]
    public void Compute_2026Gross60000PartnerWithoutIncome_withholdsByTheOtherMaximumFromNovember()
    {
        var statement = Compute(60_000m, NonEarningPartner).Statement;

        statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(707.41m, 10), .. Enumerable.Repeat(720.45m, 2)]);
    }

    // A partner with income of their own, a year taxed separately and a de facto cohabitant leave the
    // single method's withholding.
    public static TheoryData<Household> SingleMethodHouseholds => new()
    {
        EarningPartner,
        NonEarningPartner with { TaxedSeparately = true },
        new Household(HouseholdStatus.Alone),
    };

    [Theory]
    [MemberData(nameof(SingleMethodHouseholds))]
    public void Compute_2026Gross50000_withholdsAsForADirectorAlone(Household household)
    {
        var statement = Compute(50_000m, household).Statement;

        statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(890.94m, 10), .. Enumerable.Repeat(882.47m, 2)]);
    }

    // The river's year passes the scenario's household to the pay's withholding.
    [Fact]
    public void Run_2026PartnerWithoutIncome_paysTheDirectorWithholdingByTheQuotient()
    {
        var year = Run(Scenario(taxPrepayments: TaxPrepayments.AsWithheld, household: NonEarningPartner));

        year.DirectorPay.Statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(457.86m, 10), .. Enumerable.Repeat(440.92m, 2)]);
    }

    private static GrossToNetResult Compute(decimal grossPay, Household household, TaxPrepayments? prepayments = null)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), prepayments ?? TaxPrepayments.AsWithheld, household: household);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
