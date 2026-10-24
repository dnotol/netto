using Netto.Core.Allowances;
using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.Allowances;

// Circular 2021/C/20, §47 and §51, and the annex of 2026/C/84 for income 2026. Made-up allowances;
// expected values derived from the monthly maxima: the office allowance's year is 2 × 157.83,
// 6 × 160.99 and 4 × 164.21, the internet and computer allowances' 12 × 20.
public class AllowanceCalculatorTests
{
    private static readonly AllowanceRules Rules2026 = AllowanceRules.IncomeYear2026;

    // 315.66 + 965.94 + 656.84.
    [Fact]
    public void OfficeAllowanceMaximum_2026_isEachMonthsMaximumOverTheYear()
    {
        AllowanceCalculator.OfficeAllowanceMaximum(Rules2026).ShouldBe(1_938.44m);
    }

    [Fact]
    public void InternetAndComputerAllowanceMaximum_2026_areTwelveMonthsAtTheirMaximum()
    {
        (AllowanceCalculator.InternetAllowanceMaximum(Rules2026), AllowanceCalculator.ComputerAllowanceMaximum(Rules2026)).ShouldBe((240m, 240m));
    }

    [Fact]
    public void Accept_2026EachAtItsMaximum_acceptsThemAsGiven()
    {
        var allowances = new ExpenseAllowances(1_938.44m, 240m, 240m, 600m);

        AllowanceCalculator.Accept(Rules2026, allowances).ShouldBe((allowances, null));
    }

    // No official text gives the phone a flat amount: the professional share of the director's own
    // bills, on invoices, taken as declared.
    [Fact]
    public void Accept_2026PhoneCostsOfAnyAmount_acceptsThem()
    {
        var allowances = new ExpenseAllowances(0m, 0m, 0m, 25_000m);

        AllowanceCalculator.Accept(Rules2026, allowances).ShouldBe((allowances, null));
    }

    [Fact]
    public void Accept_2026None_acceptsNothing()
    {
        AllowanceCalculator.Accept(Rules2026, ExpenseAllowances.None).ShouldBe((ExpenseAllowances.None, null));
    }

    public static TheoryData<decimal, decimal, decimal, decimal, Refusal> AllowancesOutsideTheRules => new()
    {
        { -0.01m, 0m, 0m, 0m, AllowanceCalculator.OfficeAllowanceNegative },
        { 1_938.45m, 0m, 0m, 0m, AllowanceCalculator.OfficeAllowanceAboveMaximum },
        { 0m, -0.01m, 0m, 0m, AllowanceCalculator.InternetAllowanceNegative },
        { 0m, 240.01m, 0m, 0m, AllowanceCalculator.InternetAllowanceAboveMaximum },
        { 0m, 0m, -0.01m, 0m, AllowanceCalculator.ComputerAllowanceNegative },
        { 0m, 0m, 240.01m, 0m, AllowanceCalculator.ComputerAllowanceAboveMaximum },
        { 0m, 0m, 0m, -0.01m, AllowanceCalculator.PhoneCostsNegative },
    };

    [Theory]
    [MemberData(nameof(AllowancesOutsideTheRules))]
    public void Accept_2026AllowanceOutsideTheRules_isRefused(decimal office, decimal internet, decimal computer, decimal phone, Refusal expected)
    {
        AllowanceCalculator.Accept(Rules2026, new ExpenseAllowances(office, internet, computer, phone)).ShouldBe((null, expected));
    }

    [Fact]
    public void Total_isWhatTheCompanyPaysForThemAll()
    {
        new ExpenseAllowances(1_938.44m, 240m, 240m, 600m).Total.ShouldBe(3_018.44m);
    }

    [Fact]
    public void Any_onlyPhoneCosts_isTrue()
    {
        (new ExpenseAllowances(0m, 0m, 0m, 0.01m).Any, ExpenseAllowances.None.Any).ShouldBe((true, false));
    }
}
