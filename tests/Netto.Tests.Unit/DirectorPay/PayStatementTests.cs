using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.DirectorPay;

// The pay statement GrossToNet returns for income year 2026 at a 7% municipal rate: the taxable pay
// paid in twelve equal months, each with the withholding the key formula ESS-SR/2025-0928 gives on it
// by the rules in force when it is paid (WithholdingCalculatorTests), January's to October and those
// from 1 November in November and December, and the net paid. Expected values are derived from the
// formula with the official amounts, as Tax-Calc takes the withholding as an input; the check of the
// November ones against the SPF's November simulator waits for landing. The pays and contributions are
// GrossToNetTests' (50,000: contributions 8,293.36 paid by the director, 9,942.52 when the company pays
// them).
public class PayStatementTests
{
    private static readonly WithholdingRules Rules2026 = WithholdingRules.IncomeYear2026;

    public static TheoryData<decimal> GrossPays => [0m, 3_600m, 10_000m, 20_000m, 50_000m, 160_000m];

    [Fact]
    public void Compute_2026_statementHasTheTwelveMonthsOfTheYear()
    {
        var statement = Statement2026(50_000m);

        statement.Months.Select(month => month.Month).ShouldBe(Enumerable.Range(1, 12));
    }

    // 50,000 over twelve is 4,166.67, on which the formula withholds 890.94 to October and 882.47 from
    // November (WithholdingCalculatorTests).
    [Fact]
    public void Compute_2026Gross50000_withholdsJanuaryToOctoberByTheJanuaryRulesAndNovemberAndDecemberByTheNovemberRules()
    {
        var statement = Statement2026(50_000m);

        statement.Months.Select(month => (month.GrossPay, month.ContributionsPaidByCompany, month.TaxablePay, month.Withholding, month.NetPaid))
            .ShouldBe(
            [
                .. Enumerable.Repeat((4_166.67m, 0m, 4_166.67m, 890.94m, 3_275.73m), 10),
                .. Enumerable.Repeat((4_166.67m, 0m, 4_166.67m, 882.47m, 3_284.20m), 2),
            ]);
    }

    [Fact]
    public void Compute_2026_hasAPeriodForEachOfTheYearsRulesWithTheMonthsItCovers()
    {
        var statement = Statement2026(50_000m);

        statement.Periods.Select(period => (period.Rules, period.FirstMonth, period.LastMonth))
            .ShouldBe([(Rules2026, 1, 10), (Rules2026.InMonth(11), 11, 12)]);
    }

    [Fact]
    public void Compute_2026Gross50000_carriesTheFormulasLinesForAMonthOfEachPeriod()
    {
        var statement = Statement2026(50_000m);

        statement.Periods.Select(period => period.Withholding).ShouldBe(
        [
            new WithholdingResult(
                MonthlyTaxablePay: 4_166.67m, Reduction: 965.86m, AnnualGross: 38_409.72m, FlatRateCosts: 1_152.29m, AnnualNetTaxable: 37_257.43m,
                BasicTax: 13_679.25m, TaxFreeAmountReduction: 2_987.98m, AnnualTax: 10_691.27m, MonthlyWithholding: 890.94m),
            new WithholdingResult(
                MonthlyTaxablePay: 4_166.67m, Reduction: 965.86m, AnnualGross: 38_409.72m, FlatRateCosts: 1_152.29m, AnnualNetTaxable: 37_257.43m,
                BasicTax: 13_679.25m, TaxFreeAmountReduction: 3_089.63m, AnnualTax: 10_589.62m, MonthlyWithholding: 882.47m),
        ]);
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026_everyMonthWithholdsWhatTheFormulaGivesOnItsTaxablePayByTheRulesInForceThatMonth(decimal grossPay)
    {
        var statement = Statement2026(grossPay);

        var withheld = statement.Months.Select(month => WithholdingCalculator.Compute(Rules2026.InMonth(month.Month), month.TaxablePay).Value.ShouldNotBeNull().MonthlyWithholding);

        statement.Months.Select(month => month.Withholding).ShouldBe(withheld);
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026_everyMonthPaysTheGrossLessItsWithholding(decimal grossPay)
    {
        var statement = Statement2026(grossPay);

        statement.Months.ShouldAllBe(month => month.NetPaid == month.GrossPay - month.Withholding);
    }

    // Each twelfth is rounded on its own: twelve of 4,166.67 are 50,000.04, and the cents are not
    // spread over the months.
    [Fact]
    public void Compute_2026Gross50000_twelveRoundedTwelfthsDifferFromTheYearByCents()
    {
        var statement = Statement2026(50_000m);

        statement.Months.Sum(month => month.GrossPay).ShouldBe(50_000.04m);
    }

    // The company's 9,942.52 of contributions are pay (cadre XVI, 1), 828.54 a month on top of the
    // 4,166.67, so the formula withholds on 4,995.21: 1,144.00 of reduction, 46,214.52 of annual gross,
    // 1,386.44 of costs, 17,324.52 of basic tax, 14,336.54 of annual tax to October, 14,234.89 from
    // November. The net paid is the payslip gross less the withholding: the contributions never reach
    // the director.
    [Fact]
    public void Compute_2026CompanyPaysGross50000_taxableMonthlyPayIncludesTheContributionsAndTheNetPaidDoesNot()
    {
        var statement = CompanyPaysStatement2026(50_000m);

        statement.Months.Select(month => (month.GrossPay, month.ContributionsPaidByCompany, month.TaxablePay, month.Withholding, month.NetPaid))
            .ShouldBe(
            [
                .. Enumerable.Repeat((4_166.67m, 828.54m, 4_995.21m, 1_194.71m, 2_971.96m), 10),
                .. Enumerable.Repeat((4_166.67m, 828.54m, 4_995.21m, 1_186.24m, 2_980.43m), 2),
            ]);
    }

    // 3,600 over twelve is 300, under the 385 the formula takes off any pay: the reduction stays 385,
    // the year's lines fall below zero, nothing is withheld by either period's rules, and the statement
    // shows that, nothing hidden (WithholdingCalculatorTests).
    [Fact]
    public void Compute_2026GrossUnderTheReduction_withholdsNothingAndShowsTheFormulasLines()
    {
        var statement = Statement2026(3_600m);

        (statement.Months[0].TaxablePay, statement.Months.Sum(month => month.Withholding), statement.Months[0].NetPaid,
                statement.Periods[0].Withholding.Reduction, statement.Periods[0].Withholding.AnnualGross, statement.Periods[1].Withholding.AnnualTax)
            .ShouldBe((300m, 0m, 300m, 385m, -1_020m, 0m));
    }

    // Each period cites its rules as a step cites its rule: the key formula, and the annex's amounts it
    // applied, n° 32 from 1 November cited to the coordinated annex.
    [Fact]
    public void Compute_2026_eachPeriodCitesTheKeyFormulaAndItsAnnexsAmounts()
    {
        var statement = Statement2026(50_000m);

        statement.Periods.ShouldSatisfyAllConditions(
            periods => periods.Select(period => (period.RulesFrom, period.Source)).ShouldBe(Rules2026.Periods().Select(rules => (rules.RulesFrom, rules.Source))),
            periods => periods[0].AmountsUsed.ShouldBe(Rules2026.Amounts),
            periods => periods[1].AmountsUsed.ShouldBe(Rules2026.InMonth(11).Amounts));
    }

    // A period withheld on by the marital quotient cites the amounts that method applied, the
    // maximum for partners both 66 or over from 1 November among them.
    [Theory]
    [InlineData(1, WithholdingHousehold.PartnerWithoutIncome)]
    [InlineData(11, WithholdingHousehold.PartnerWithoutIncome)]
    [InlineData(11, WithholdingHousehold.PartnerWithoutIncomeBothAged66)]
    public void AmountsUsed_quotientPeriod_areTheAmountsOfItsHouseholdsMethod(int month, WithholdingHousehold household)
    {
        var rules = Rules2026.InMonth(month);
        var withholding = WithholdingCalculator.Compute(rules, 4_166.67m, household).Value.ShouldNotBeNull();

        var period = new PayStatementPeriod(rules, month, month, withholding);

        period.AmountsUsed.ShouldBe(rules.AmountsFor(household));
    }

    private static PayStatement Statement2026(decimal grossPay)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld);

        error.ShouldBeNull();
        return value.ShouldNotBeNull().Statement;
    }

    private static PayStatement CompanyPaysStatement2026(decimal grossPay)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, ContributionsPaidBy.Company);

        error.ShouldBeNull();
        return value.ShouldNotBeNull().Statement;
    }
}
