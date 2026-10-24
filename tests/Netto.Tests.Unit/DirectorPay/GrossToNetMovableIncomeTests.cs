using Netto.Core.Copyright;
using Netto.Core.DirectorPay;
using Netto.Core.MovableIncome;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.DirectorPay;

// Movable income the company pays the director besides the payslip, a copyright royalty: the part
// beyond the law's limits is taxed as director pay paid in cash but never on the payslip, so in
// INASTI's base, on the scale and in the surcharge's base, never in the key formula's withholding nor in
// what the pay costs the company; the part within them is taxed apart at its rate, on the whole year's
// figures, the pay's own staying the pay's; what the company withheld at source on it is credited on the
// assessment. Income year 2026 at a 7% municipal rate, derived from the official amounts.
public class GrossToNetMovableIncomeTests
{
    private const decimal AsPay = 5_000m;

    private static readonly RuleAmountOf2026 Rate = new(CopyrightRules.IncomeYear2026.TaxedApartRate);

    public static TheoryData<decimal, ContributionsPaidBy> Pays => new()
    {
        { 0m, ContributionsPaidBy.Director },
        { 20_000m, ContributionsPaidBy.Director },
        { 50_000m, ContributionsPaidBy.Director },
        { 160_000m, ContributionsPaidBy.Director },
        { 20_000m, ContributionsPaidBy.Company },
        { 50_000m, ContributionsPaidBy.Company },
    };

    [Theory]
    [MemberData(nameof(Pays))]
    public void Compute_partTaxedAsPay_taxesItAsCashPayOfTheSameAmount(decimal grossPay, ContributionsPaidBy paidBy)
    {
        var withIt = Compute(grossPay, paidBy, Withheld, Movable(asPay: AsPay));
        var asCash = Compute(grossPay + AsPay, paidBy, Withheld);

        (withIt.SocialContributions, withIt.TaxablePay, withIt.FlatRateCosts, withIt.TaxableIncome, withIt.IncomeTax, withIt.MunicipalSurcharge,
                withIt.LowEarnedIncomeCredit, withIt.NetInPocket)
            .ShouldBe((asCash.SocialContributions, asCash.TaxablePay, asCash.FlatRateCosts, asCash.TaxableIncome, asCash.IncomeTax, asCash.MunicipalSurcharge,
                asCash.LowEarnedIncomeCredit, asCash.NetInPocket));
    }

    // The company pays the royalty, not pay: the pay costs it the payslip's gross and the contributions
    // it pays for the director, those on the part taxed as pay among them.
    [Theory]
    [MemberData(nameof(Pays))]
    public void Compute_partTaxedAsPay_costsTheCompanyTheCashPayLessThatPart(decimal grossPay, ContributionsPaidBy paidBy)
    {
        var withIt = Compute(grossPay, paidBy, Withheld, Movable(asPay: AsPay));
        var asCash = Compute(grossPay + AsPay, paidBy, Withheld);

        (withIt.GrossPay, withIt.RequalifiedPay, withIt.CompanyCost).ShouldBe((grossPay, AsPay, asCash.CompanyCost - AsPay));
    }

    // Art. 215's minimum pay never counts it (decided unattended): the pay it tests is the payslip's, with
    // what the company pays for the director on that alone.
    [Theory]
    [MemberData(nameof(Pays))]
    public void Compute_partTaxedAsPay_leavesItOutOfThePayWithoutIt(decimal grossPay, ContributionsPaidBy paidBy)
    {
        var withIt = Compute(grossPay, paidBy, Withheld, Movable(asPay: AsPay));
        var alone = Compute(grossPay, paidBy, Withheld);

        withIt.TaxablePayWithoutRequalified.ShouldBe(alone.TaxablePay);
    }

    // Not on the payslip, so the key formula withholds on the payslip's pay alone.
    [Fact]
    public void Compute_partTaxedAsPay_isNotWithheldOnByTheKeyFormula()
    {
        var withIt = Compute(50_000m, ContributionsPaidBy.Director, TaxPrepayments.AsWithheld, Movable(asPay: AsPay));
        var alone = Compute(50_000m, ContributionsPaidBy.Director, TaxPrepayments.AsWithheld);

        (withIt.Statement.Months[0], withIt.Assessment.Withheld).ShouldBe((alone.Statement.Months[0], alone.Assessment.Withheld));
    }

    // It is director pay (art. 30, 2°), so its tax is in the surcharge's base (art. 157).
    [Fact]
    public void Compute_partTaxedAsPayAndNothingWithheld_surchargesTheTaxOnIt()
    {
        var withIt = Compute(50_000m, ContributionsPaidBy.Director, TaxPrepayments.None, Movable(asPay: AsPay));
        var asCash = Compute(50_000m + AsPay, ContributionsPaidBy.Director, TaxPrepayments.None);

        withIt.AdvancePayments.Surcharge.ShouldBe(asCash.AdvancePayments.Surcharge);
    }

    // Within its limits it is taxed apart: the pay's own figures stay the pay's.
    [Fact]
    public void Compute_partTaxedApart_leavesThePaysOwnFiguresAsTheyAre()
    {
        var withIt = Compute(50_000m, ContributionsPaidBy.Director, Withheld, Movable(apart: 10_000m));
        var alone = Compute(50_000m, ContributionsPaidBy.Director, Withheld);

        (withIt.IncomeTax, withIt.MunicipalSurcharge, withIt.NetInPocket).ShouldBe((alone.IncomeTax, alone.MunicipalSurcharge, alone.NetInPocket));
    }

    [Fact]
    public void Compute_partTaxedApart_leavesThePaysStepsAsTheyAre()
    {
        var withIt = Compute(50_000m, ContributionsPaidBy.Director, Withheld, Movable(apart: 10_000m));
        var alone = Compute(50_000m, ContributionsPaidBy.Director, Withheld);

        withIt.Steps.Select(step => (step.RuleKey, step.Amount)).ShouldBe(alone.Steps.Select(step => (step.RuleKey, step.Amount)));
    }

    // What it adds is the tax on the whole income less the pay's: its 15%, which joins the State tax
    // before the split, and the municipal surcharge on it.
    [Fact]
    public void Compute_partTaxedApart_addsTheTaxOnTheWholeIncomeLessThePays()
    {
        var withIt = Compute(50_000m, ContributionsPaidBy.Director, Withheld, Movable(apart: 10_000m));
        var whole = PersonalTaxCalculator.Compute(PersonalTaxRules.IncomeYear2026, withIt.TaxablePay, withIt.SocialContributions, 0.07m, taxedApart: Rate.Of(10_000m));

        (withIt.TaxedApartTax.Income, withIt.TaxedApartTax.IncomeTax, withIt.TaxedApartTax.MunicipalSurcharge)
            .ShouldBe((10_000m, whole.IncomeTax - withIt.IncomeTax, whole.MunicipalSurcharge - withIt.MunicipalSurcharge));
    }

    // 1,500 of tax apart, give or take the split's cents.
    [Fact]
    public void Compute_partTaxedApart_taxesItAt15Percent()
    {
        var withIt = Compute(50_000m, ContributionsPaidBy.Director, Withheld, Movable(apart: 10_000m));

        withIt.TaxedApartTax.IncomeTax.ShouldBeInRange(1_499.99m, 1_500.01m);
    }

    // On a pay whose credit falls, the income taxed apart raises the total net income the credit is
    // limited by: it takes some credit away, and that lost credit raises the surcharge on the pay's tax.
    [Fact]
    public void Compute_partTaxedApartBesideALowPay_takesCreditAway()
    {
        var withIt = Compute(27_000m, ContributionsPaidBy.Director, TaxPrepayments.None, Movable(apart: 3_000m));
        var alone = Compute(27_000m, ContributionsPaidBy.Director, TaxPrepayments.None);
        var limited = PersonalTaxCalculator.LowEarnedIncomeCredit(PersonalTaxRules.IncomeYear2026, alone.TaxableIncome, alone.TaxableIncome + 3_000m);

        (withIt.TaxedApartTax.LowEarnedIncomeCreditLost, withIt.TaxedApartTax.AdvancePayments > 0m).ShouldBe((alone.LowEarnedIncomeCredit - limited, true));
    }

    [Fact]
    public void Compute_partTaxedApartBesideALowPay_isOnACreditThatFalls()
    {
        var alone = Compute(27_000m, ContributionsPaidBy.Director, TaxPrepayments.None);

        alone.LowEarnedIncomeCredit.ShouldBeGreaterThan(0m);
    }

    // The assessment credits what the company withheld on it (art. 279).
    [Fact]
    public void Compute_withheldOnMovableIncome_isCreditedOnTheAssessment()
    {
        var withIt = Compute(50_000m, ContributionsPaidBy.Director, Withheld, Movable(apart: 10_000m, withheld: 1_500m));
        var alone = Compute(50_000m, ContributionsPaidBy.Director, Withheld);

        (withIt.Assessment.WithheldBesidesPay, withIt.Assessment.Balance)
            .ShouldBe((1_500m, alone.Assessment.Balance + withIt.TaxedApartTax.IncomeTax + withIt.TaxedApartTax.MunicipalSurcharge - 1_500m));
    }

    // A rent taxed with the pay adds what it adds to the pay alone, and the income taxed apart what it
    // adds on top of both.
    [Fact]
    public void Compute_rentAndPartTaxedApart_addEachTheirOwn()
    {
        var both = Compute(50_000m, ContributionsPaidBy.Director, Withheld, Movable(apart: 10_000m), otherIncome: 828m);
        var rentAlone = Compute(50_000m, ContributionsPaidBy.Director, Withheld, otherIncome: 828m);

        (both.OtherIncomeTax, both.TaxedApartTax.Income).ShouldBe((rentAlone.OtherIncomeTax, 10_000m));
    }

    // What the pay leaves once the rent's tax and the tax apart are paid too: the year's figure the
    // pocket starts from, the two incomes themselves aside.
    [Fact]
    public void Compute_rentAndPartTaxedApart_leaveThePayItsNetLessWhatEachAdds()
    {
        var both = Compute(50_000m, ContributionsPaidBy.Director, Withheld, Movable(apart: 10_000m), otherIncome: 828m);

        both.YearNetInPocket.ShouldBe(both.NetInPocket - both.OtherIncomeTax.Total - both.TaxedApartTax.Total);
    }

    [Fact]
    public void Compute_withoutMovableIncome_recordsNone()
    {
        var alone = Compute(50_000m, ContributionsPaidBy.Director, Withheld);

        (alone.RequalifiedPay, alone.TaxedApartTax, alone.Assessment.WithheldBesidesPay, alone.TaxablePayWithoutRequalified)
            .ShouldBe((0m, OtherIncomeTax.None, 0m, alone.TaxablePay));
    }

    // The engine's own rules give them, never below zero.
    [Fact]
    public void Compute_negativePartTaxedAsPay_isABug()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            GrossToNet.Compute(
                2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, ContributionsPaidBy.Director, new TaxedWithPay { Movable = Movable(asPay: -0.01m) }));
    }

    private static MovableIncomePaid Movable(decimal asPay = 0m, decimal apart = 0m, decimal withheld = 0m) =>
        new(asPay + apart, apart > 0m ? Rate.Of(apart) : null, asPay, withheld);

    private static GrossToNetResult Compute(
        decimal grossPay, ContributionsPaidBy paidBy, TaxPrepayments prepayments, MovableIncomePaid? movable = null, decimal otherIncome = 0m)
    {
        var (value, error) = GrossToNet.Compute(
            2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), prepayments, paidBy,
            new TaxedWithPay { OtherIncome = otherIncome, Movable = movable ?? MovableIncomePaid.None });

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    private sealed record RuleAmountOf2026(Core.Rules.RuleAmount Rate)
    {
        public IncomeTaxedApart Of(decimal income) => new(income, Rate);
    }
}
