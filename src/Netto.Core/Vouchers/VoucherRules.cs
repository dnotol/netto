using Netto.Core.Rules;

namespace Netto.Core.Vouchers;

// The electronic meal vouchers, eco vouchers and sport/culture vouchers a company grants its director,
// for one income year: what makes the company's share exempt for the director (art. 38, §1, al. 1,
// 25°, on the conditions of art. 38/1), which keeps it out of the income INASTI is owed on too, and
// what the company deducts of it (art. 53, 14°). The conditions are the user's to confirm: Netto
// cannot see an agreement or the days worked, so it lists them with their texts.
public sealed record VoucherRules(
    int IncomeYear,
    Source Exemption,
    Source NonDeductible,
    RuleAmount MealVoucherCompanyShare,
    RuleAmount MealVoucherHolderShare,
    RuleAmount MealVoucherDeduction,
    RuleAmount MealVoucherDeductionAtMaximum,
    RuleAmount SportCultureVouchersMaximum,
    RuleAmount EcoVouchersMaximum,
    RuleAmount EcoVoucherFaceValueMaximum,
    IReadOnlyList<LeverCondition> Conditions)
{
    private static readonly Source Article38Income2026 = Source.Fisconet("personal-tax", "38", 2026, new DateOnly(2026, 7, 31), "24644f80-3b10-49ba-b4fe-0115166ab59d", "vouchers_exempt");

    private static readonly Source Article38_1Income2026 = Source.Fisconet("personal-tax", "38/1", 2026, new DateOnly(2026, 1, 14), "6a028d41-f93b-43f0-85ce-ec749722675a", "voucher_conditions");

    private static readonly Source Article53Income2026 = SharedSources.Article53Income2026 with { Passage = "meal_voucher_deduction" };

    private static readonly Source ParliamentaryQuestion15220 = new(
        "directors_without_staff",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/81a0efae-d13f-4c4b-ba07-efe720ab8c7f"),
        "personal-tax/spf-finances_question-parlementaire-15220_2019-10-04.html");

    private static readonly RuleAmount EcoVoucherFaceValueMaximum2026 = new("eco_voucher_face_value_maximum", 10m, Article38_1Income2026);

    public static VoucherRules IncomeYear2026 { get; } = new(
        2026,
        Article38Income2026,
        Article53Income2026,
        new RuleAmount("meal_voucher_company_share", 8.91m, Article38_1Income2026),
        new RuleAmount("meal_voucher_holder_share", 1.09m, Article38_1Income2026),
        new RuleAmount("meal_voucher_deduction", 2m, Article53Income2026),
        new RuleAmount("meal_voucher_deduction_at_maximum", 4m, Article53Income2026),
        new RuleAmount("sport_culture_vouchers_maximum", 100m, Article38_1Income2026),
        new RuleAmount("eco_vouchers_maximum", 250m, Article38_1Income2026),
        EcoVoucherFaceValueMaximum2026,
        [
            new LeverCondition("written_agreement", [Article38_1Income2026]),
            new LeverCondition("not_instead_of_pay", [Article38_1Income2026]),
            new LeverCondition("days_actually_worked", [Article38_1Income2026, ParliamentaryQuestion15220]),
            new LeverCondition("in_your_name", [Article38_1Income2026]),
            new LeverCondition("eco_agreement_terms", [Article38_1Income2026]) { Amounts = [EcoVoucherFaceValueMaximum2026] },
        ]);

    public static VoucherRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };
}
