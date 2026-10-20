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
    IReadOnlyList<VoucherCondition> Conditions)
{
    private static readonly Source Article38Income2026 = new(
        "CIR 92, art. 38 (revenus 2026), coordinated text on Fisconetplus, last modified 31.07.2026",
        "§1, al. 1, 25°: the employer's or company's share of electronic meal vouchers, sport/culture vouchers and eco vouchers that meet the conditions of art. 38/1 is exempt",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/24644f80-3b10-49ba-b4fe-0115166ab59d"),
        "personal-tax/spf-finances_cir92-art-38-revenus-2026_2026-07-31.html");

    private static readonly Source Article38_1Income2026 = new(
        "CIR 92, art. 38/1 (revenus 2026), coordinated text on Fisconetplus, last modified 14.01.2026",
        "§1: not in replacement of pay. §2, al. 1: meal vouchers granted by a written individual agreement where no collective one is possible (1°), one per day actually worked (2°), in the holder's name (3°), the company's share at most 8.91 (5°, vouchers granted from 01.01.2026) and the holder's at least 1.09 (6°). §3: sport/culture vouchers, at most 100 a year (4°). §4: eco vouchers, the agreement stating their face value, at most 10, and how often they are granted (2°), at most 250 a year (6°)",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/6a028d41-f93b-43f0-85ce-ec749722675a"),
        "personal-tax/spf-finances_cir92-art-38-1-revenus-2026_2026-01-14.html");

    private static readonly Source Article53Income2026 = new(
        "CIR 92, art. 53 (revenus 2026), coordinated text on Fisconetplus, last modified 18.02.2026",
        "14°: the benefits of art. 38, §1, al. 1, 25° are not professional expenses, except the company's share of an electronic meal voucher, up to 2 a voucher, raised to 4 when that share reaches the 8.91 of art. 38/1, §2, al. 1, 5° (vouchers granted from 01.01.2026)",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/796aa044-5b54-4569-88a4-abe7be55e077"),
        "corporate-tax/spf-finances_cir92-art-53-revenus-2026_2026-02-18.html");

    private static readonly Source ParliamentaryQuestion15220 = new(
        "Parliamentary question n° 15220 of 01.02.2017 (CRIV 54 COM 582), on Fisconetplus, last modified 04.10.2019",
        "The Minister of Finance: directors who employ no staff are not excluded from the meal-voucher exemption, but the vouchers must match the days actually worked; without proof of them the exemption does not apply and the company's share is taxable pay",
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
            new VoucherCondition("written_agreement", [Article38_1Income2026]),
            new VoucherCondition("not_instead_of_pay", [Article38_1Income2026]),
            new VoucherCondition("days_actually_worked", [Article38_1Income2026, ParliamentaryQuestion15220]),
            new VoucherCondition("in_your_name", [Article38_1Income2026]),
            new VoucherCondition("eco_agreement_terms", [Article38_1Income2026]) { Amounts = [EcoVoucherFaceValueMaximum2026] },
        ]);

    public static VoucherRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };
}

// One condition of the exemption the user confirms, under a stable key the page gives its words, with
// the texts it comes from and the amounts those words name.
public sealed record VoucherCondition(string Key, IReadOnlyList<Source> Sources)
{
    public IReadOnlyList<RuleAmount> Amounts { get; init; } = [];
}
