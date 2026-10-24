using Netto.Core.Rules;
using Netto.Core.SocialContributions;
using Netto.Tests.Rules;
using Shouldly;

namespace Netto.Tests.SocialContributions;

// INASTI's yearly contribution every company owes, for 2026, as its FAQ publishes it: 409.30 while the
// balance-sheet total of 2024 is at most 879,163.44, 1,022.38 above it. They are the law's own amounts of
// art. 91 (347.50, 868.00 and 746,410.17) indexed by its formula, the consumer price index of November 2025,
// 191.59, over 162.66, to the cent.
public class CompanyContributionRulesTests
{
    private const string Faq = "social-contributions/inasti_faq-cotisation-a-charge-des-societes_2026-10-24.html";

    private const string Law = "social-contributions/ejustice_loi-dispositions-sociales-diverses-1992_2022-12-30.html";

    public static TheoryData<CompanyContributionBand, decimal> Bands => new()
    {
        { CompanyContributionBand.Lower, 409.30m },
        { CompanyContributionBand.Upper, 1_022.38m },
        { CompanyContributionBand.Exempt, 0m },
    };

    [Fact]
    public void IncomeYear2026_holdsTheTwoAmountsAndTheBalanceSheetTotalBetweenThem()
    {
        var rules = CompanyContributionRules.IncomeYear2026;

        new[] { rules.BalanceSheetThreshold, rules.LowerBand, rules.UpperBand }.Select(amount => (amount.Key, amount.Value)).ShouldBe(new[]
        {
            ("balance_sheet_threshold", 879_163.44m),
            ("company_contribution_lower_band", 409.30m),
            ("company_contribution_upper_band", 1_022.38m),
        });
    }

    [Theory]
    [MemberData(nameof(Bands))]
    public void Contribution_band_isThatBandsAmount(CompanyContributionBand band, decimal amount)
    {
        CompanyContributionRules.IncomeYear2026.Contribution(band).Value.ShouldBe(amount);
    }

    // A company the law exempts, a société de personnes in its first three years (art. 94, 9°) or one with
    // no activity in a whole calendar year (art. 92bis), owes nothing, as the law says.
    [Fact]
    public void IncomeYear2026_exemptCompanyOwesNothingUnderTheLawsTwoExemptions()
    {
        var exemption = CompanyContributionRules.IncomeYear2026.Exemption;

        (exemption.Key, exemption.Value, exemption.Source.Document, exemption.Source.Label)
            .ShouldBe(("company_contribution_exempt", 0m, Law, "Law of 30.12.1992, art. 94 and 92bis"));
    }

    // A step cites the band's amount and the balance-sheet total that sets it; an exempt company's, the
    // exemption alone, which no balance sheet sets.
    [Theory]
    [InlineData(CompanyContributionBand.Lower)]
    [InlineData(CompanyContributionBand.Upper)]
    public void Used_band_isItsAmountAndTheThreshold(CompanyContributionBand band)
    {
        var rules = CompanyContributionRules.IncomeYear2026;

        rules.Used(band).ShouldBe([rules.Contribution(band), rules.BalanceSheetThreshold]);
    }

    [Fact]
    public void Used_exempt_isTheExemptionAlone()
    {
        var rules = CompanyContributionRules.IncomeYear2026;

        rules.Used(CompanyContributionBand.Exempt).ShouldBe([rules.Exemption]);
    }

    // The band is set by the balance-sheet total of the penultimate closed financial year as it stood on
    // 1 January of the contribution year (art. 91): 2024's for 2026, a calendar accounting year assumed.
    [Fact]
    public void IncomeYear2026_readsTheBalanceSheetOf2024()
    {
        CompanyContributionRules.IncomeYear2026.BalanceSheetYear.ShouldBe(2024);
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public void For_incomeYearOtherThan2026_hasNoRules(int incomeYear)
    {
        CompanyContributionRules.For(incomeYear).ShouldBeNull();
    }

    [Fact]
    public void For_2026_isTheIncomeYearsRules()
    {
        CompanyContributionRules.For(2026).ShouldBe(CompanyContributionRules.IncomeYear2026);
    }

    // Each slot holds the amount named for it: the two bands swapped would pass a test of the values alone.
    [Fact]
    public void IncomeYear2026_eachAmountSlotHoldsItsKey()
    {
        RuleSets.Slots<RuleAmount>(CompanyContributionRules.IncomeYear2026).Select(slot => (slot.Slot, slot.Value?.Key)).ShouldBe(
        [
            ("BalanceSheetThreshold", "balance_sheet_threshold"),
            ("LowerBand", "company_contribution_lower_band"),
            ("UpperBand", "company_contribution_upper_band"),
            ("Exemption", "company_contribution_exempt"),
        ]);
    }

    // The amounts are INASTI's for the year; the contribution is the law's, and the company deducts it as a
    // professional cost under CIR 92, art. 49, the law making it of the same nature as social contributions.
    [Fact]
    public void IncomeYear2026_eachTextSlotHoldsItsDocument()
    {
        RuleSets.Slots<Source>(CompanyContributionRules.IncomeYear2026).Select(slot => (slot.Slot, slot.Value?.Document)).ShouldBe(
        [
            ("Source", Faq),
            ("Law", Law),
            ("Deduction", "corporate-tax/spf-finances_cir92-art-49-revenus-2026_2026-02-04.html"),
        ]);
    }

    [Fact]
    public void IncomeYear2026_amountsAreTheFaqsAndListed()
    {
        var rules = CompanyContributionRules.IncomeYear2026;

        rules.ShouldSatisfyAllConditions(
            () => rules.Amounts.ShouldBe([rules.BalanceSheetThreshold, rules.LowerBand, rules.UpperBand, rules.Exemption]),
            () => rules.Amounts.Select(amount => amount.Source.Document).ShouldBe([Faq, Faq, Faq, Law]));
    }

    // Netto counts the contribution for every company: the two cases the law exempts, which it cannot see,
    // are conditions the user confirms, each citing its article.
    [Fact]
    public void Conditions_2026_areTheTwoExemptionsTheLawGrants()
    {
        CompanyContributionRules.IncomeYear2026.Conditions.SelectMany(condition => condition.Sources, (condition, source) => (condition.Key, source.Document, source.Label))
            .ShouldBe(new[]
            {
                ("not_a_starting_partnership", Law, "Law of 30.12.1992, art. 94"),
                ("active_all_year", Law, "Law of 30.12.1992, art. 92bis"),
            });
    }
}
