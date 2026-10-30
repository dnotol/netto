using Netto.Core.Vouchers;
using Shouldly;

namespace Netto.Tests.Vouchers;

// Art. 38/1 and art. 53, 14° as they stand for vouchers granted from 01.01.2026 (law of 18.12.2025).
public class VoucherRulesTests
{
    [Fact]
    public void IncomeYear2026_holdsTheAmountsOfArticles38_1And53()
    {
        var rules = VoucherRules.IncomeYear2026;

        new[]
        {
            rules.MealVoucherCompanyShare, rules.MealVoucherHolderShare, rules.MealVoucherDeduction, rules.MealVoucherDeductionAtMaximum,
            rules.SportCultureVouchersMaximum, rules.EcoVouchersMaximum, rules.EcoVoucherFaceValueMaximum,
        }.Select(amount => (amount.Key, amount.Value)).ShouldBe(new[]
        {
            ("meal_voucher_company_share", 8.91m),
            ("meal_voucher_holder_share", 1.09m),
            ("meal_voucher_deduction", 2m),
            ("meal_voucher_deduction_at_maximum", 4m),
            ("sport_culture_vouchers_maximum", 100m),
            ("eco_vouchers_maximum", 250m),
            ("eco_voucher_face_value_maximum", 10m),
        });
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public void For_incomeYearOtherThan2026_hasNoRules(int incomeYear)
    {
        VoucherRules.For(incomeYear).ShouldBeNull();
    }

    // The conditions the user confirms, each with the text it comes from: the written agreement, not
    // in replacement of pay, one voucher per day actually worked and provable, electronic and in the
    // director's name, and what the agreement states about eco vouchers.
    [Fact]
    public void Conditions_2026_eachCitesItsText()
    {
        VoucherRules.IncomeYear2026.Conditions.Select(condition => (condition.Key, condition.Sources.Count)).ShouldBe(new[]
        {
            ("written_agreement", 1),
            ("not_instead_of_pay", 1),
            ("days_actually_worked", 2),
            ("in_your_name", 1),
            ("eco_agreement_terms", 1),
        });
    }

    [Fact]
    public void Conditions_2026_citeEachTextByAShortLabel()
    {
        VoucherRules.IncomeYear2026.Conditions.Select(condition => condition.Sources.Select(source => SourceWordsInEnglish.Of(source).Label)).ShouldBe(new string[][]
        {
            ["CIR 92, art. 38/1"],
            ["CIR 92, art. 38/1"],
            ["CIR 92, art. 38/1", "Question 15220"],
            ["CIR 92, art. 38/1"],
            ["CIR 92, art. 38/1"],
        });
    }

    [Fact]
    public void Conditions_2026EcoAgreement_namesTheFaceValueItStates()
    {
        VoucherRules.IncomeYear2026.Conditions.Single(condition => condition.Key == "eco_agreement_terms").Amounts
            .ShouldBe([VoucherRules.IncomeYear2026.EcoVoucherFaceValueMaximum]);
    }
}
