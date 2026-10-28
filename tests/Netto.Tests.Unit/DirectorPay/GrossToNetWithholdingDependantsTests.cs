using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.DirectorPay;

// The pay statement of a director with dependants: each month withheld on less the reductions of annex III
// n° 38 to 40 (WithholdingCalculatorDependantsTests), by January's rules to October and those from
// 1 November in November and December, under the method the household takes (WithholdingHouseholdsTests),
// for the dependants the director's withholding takes (WithholdingDependantsTests): none when the
// assessment gives their supplements to a partner with the higher income (CIR 92 art. 134 §4). Income
// year 2026 at a 7% municipal rate, the formula's withholding and nothing prepaid. Expected values are
// derived from the official amounts by the SPF simulator's chain, Tax-Calc for income year 2026 not
// being online: 50,000 over twelve is 4,166.67, whose single method withholds 890.94 to October and
// 882.47 from November, 752.94 and 745.47 with two children (1,656 and 1,644 off the year's tax).
public class GrossToNetWithholdingDependantsTests
{
    private static readonly Dependants TwoChildren = new(Children: 2);

    private static Household Alone(Dependants dependants) => Household.None with { Dependants = dependants };

    // 10 × 752.94 + 2 × 745.47.
    [Fact]
    public void Compute_2026Gross50000TwoChildren_withholdsLessTheChildrensReductionInEachPeriod()
    {
        var pay = Compute(50_000m, Alone(TwoChildren));

        pay.ShouldSatisfyAllConditions(
            () => pay.Statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(752.94m, 10), .. Enumerable.Repeat(745.47m, 2)]),
            () => pay.Assessment.Withheld.ShouldBe(9_020.34m));
    }

    // A single parent of two: 624 more to October, 636 from November; 10 × 700.94 + 2 × 692.47.
    [Fact]
    public void Compute_2026Gross50000SingleParentOfTwo_withholdsLessTheSingleParentsReductionToo()
    {
        var pay = Compute(50_000m, Alone(TwoChildren with { SingleParent = true }));

        pay.ShouldSatisfyAllConditions(
            () => pay.Statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(700.94m, 10), .. Enumerable.Repeat(692.47m, 2)]),
            () => pay.Assessment.Withheld.ShouldBe(8_394.34m));
    }

    // Under the marital quotient, 457.86 and 440.92 less the same reductions; 10 × 319.86 + 2 × 303.92.
    [Fact]
    public void Compute_2026Gross50000PartnerWithoutIncomeTwoChildren_withholdsByTheQuotientLessTheChildrensReduction()
    {
        var pay = Compute(50_000m, NonEarningPartner with { Dependants = TwoChildren });

        pay.ShouldSatisfyAllConditions(
            () => pay.Statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(319.86m, 10), .. Enumerable.Repeat(303.92m, 2)]),
            () => pay.Assessment.Withheld.ShouldBe(3_806.44m));
    }

    // A partner who earns 10,000, less than the director: the supplements go to the director, whose single
    // method takes the children's reduction.
    [Fact]
    public void Compute_2026Gross50000PartnerEarningLess_withholdsLessTheChildrensReduction()
    {
        var pay = Compute(50_000m, new Household(HouseholdStatus.MarriedOrLegalCohabitant, 10_000m) { Dependants = TwoChildren });

        pay.ShouldSatisfyAllConditions(
            () => pay.SupplementsTo.ShouldBe(Taxpayer.Director),
            () => pay.Statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(752.94m, 10), .. Enumerable.Repeat(745.47m, 2)]));
    }

    // A partner who earns 80,000, more than the director: the supplements go to them, who claims the reductions on
    // their own pay; the director's is withheld on as without dependants.
    [Fact]
    public void Compute_2026Gross50000PartnerEarningMore_withholdsWithoutTheReductions()
    {
        var pay = Compute(50_000m, new Household(HouseholdStatus.MarriedOrLegalCohabitant, 80_000m) { Dependants = TwoChildren });

        pay.ShouldSatisfyAllConditions(
            () => pay.SupplementsTo.ShouldBe(Taxpayer.Partner),
            () => pay.Statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(890.94m, 10), .. Enumerable.Repeat(882.47m, 2)]));
    }

    // Each period carries the dependants it was withheld for and cites the reduction of n° 38 it applied.
    [Fact]
    public void Compute_2026Gross50000TwoChildren_periodsCiteTheChildrensReduction()
    {
        var periods = Compute(50_000m, Alone(TwoChildren)).Statement.Periods;

        periods.Select(period => (period.Withholding.Dependants, period.AmountsUsed[^1])).ShouldBe(
        [
            (new WithholdingDependants(2), WithholdingRules.IncomeYear2026.ChildReductions[1]),
            (new WithholdingDependants(2), WithholdingRules.IncomeYear2026.InMonth(11).ChildReductions[1]),
        ]);
    }

    // What the advance payments' surcharge counts as withheld is that same withholding, as if typed.
    [Fact]
    public void Compute_2026Gross50000TwoChildren_advancePaymentsCountTheReducedWithholding()
    {
        var household = Alone(TwoChildren);
        var pay = Compute(50_000m, household);

        var typed = Compute(50_000m, household, new TaxPrepayments(9_020.34m, AdvancePayments.None));

        (pay.AdvancePayments.Base, pay.AdvancePayments.Surcharge).ShouldBe((typed.AdvancePayments.Base, typed.AdvancePayments.Surcharge));
    }

    // The river's year passes the scenario's dependants to the pay's withholding.
    [Fact]
    public void Run_2026TwoChildren_paysTheDirectorWithholdingLessTheChildrensReduction()
    {
        var year = Run(Scenario(taxPrepayments: TaxPrepayments.AsWithheld, household: Alone(TwoChildren)));

        year.DirectorPay.Statement.Months.Select(month => month.Withholding).ShouldBe([.. Enumerable.Repeat(752.94m, 10), .. Enumerable.Repeat(745.47m, 2)]);
    }

    private static GrossToNetResult Compute(decimal grossPay, Household household, TaxPrepayments? prepayments = null)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), prepayments ?? TaxPrepayments.AsWithheld, household: household);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
