using Netto.Core.Allowances;
using Shouldly;

namespace Netto.Tests.Allowances;

// Circular 2021/C/20 and the annex of its eleventh addendum, 2026/C/84, as they stand for 2026.
public class AllowanceRulesTests
{
    // The office allowance follows the maximum ONSS accepts: 157.83 since March 2025, 160.99 from
    // March 2026 and 164.21 from September 2026.
    [Fact]
    public void IncomeYear2026_holdsTheOfficeMaximaOfTheAnnexWithTheirMonths()
    {
        AllowanceRules.IncomeYear2026.OfficeAllowanceMaxima.Select(maximum => (maximum.FromMonth, maximum.Amount.Key, maximum.Amount.Value)).ShouldBe(new[]
        {
            (1, "office_allowance_maximum_from_january", 157.83m),
            (3, "office_allowance_maximum_from_march", 160.99m),
            (9, "office_allowance_maximum_from_september", 164.21m),
        });
    }

    // Neither is indexed (§47 and §51).
    [Fact]
    public void IncomeYear2026_holdsTheInternetAndComputerMonthlyMaximaOfTheCircular()
    {
        var rules = AllowanceRules.IncomeYear2026;

        new[] { rules.InternetAllowanceMonthlyMaximum, rules.ComputerAllowanceMonthlyMaximum }.Select(amount => (amount.Key, amount.Value)).ShouldBe(new[]
        {
            ("internet_allowance_monthly_maximum", 20m),
            ("computer_allowance_monthly_maximum", 20m),
        });
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2027)]
    public void For_incomeYearOtherThan2026_hasNoRules(int incomeYear)
    {
        AllowanceRules.For(incomeYear).ShouldBeNull();
    }

    // The conditions the user confirms, each with the text it comes from, the director's own case
    // first: the circular's flat amounts are not written for directors (§7–8); the company bears none
    // of these costs otherwise (§18, §48, §52); the office allowance covers the room, its rent included
    // (§31); the phone, on invoices, is a reimbursement of the company's own costs (art. 32 and §8).
    [Fact]
    public void Conditions_2026_eachCitesItsTextTheDirectorsCaseFirst()
    {
        var rules = AllowanceRules.IncomeYear2026;

        rules.Conditions.SelectMany(condition => condition.Sources, (condition, source) => (condition.Key, source)).ShouldBe(new[]
        {
            ("directors_case_by_case", rules.TeleworkCircular),
            ("no_other_company_share", rules.TeleworkCircular),
            ("office_covers_rent", rules.TeleworkCircular),
            ("phone_on_invoices", rules.Reimbursement),
            ("phone_on_invoices", rules.TeleworkCircular),
        });
    }

    // The circular is cited whole: the allowances' conditions draw on several of its paragraphs.
    [Fact]
    public void Conditions_2026_citeEachTextByAShortLabel()
    {
        var rules = AllowanceRules.IncomeYear2026;

        (rules.Reimbursement.Label, rules.TeleworkCircular.Label).ShouldBe(("CIR 92, art. 32", "Circular 2021/C/20"));
    }
}
