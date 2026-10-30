using Netto.Core.Rules;

namespace Netto.Core.Allowances;

// The flat-rate allowances a company pays its director for an office, internet and a computer at
// home, and the phone costs it reimburses, for one income year. A reimbursement of the company's own
// costs is not a director's pay (art. 32, al. 2, 1°): it reaches the pocket untaxed and outside the
// income INASTI is owed on, and the company deducts it like any other cost. The amounts are those the
// administration accepts from an employer (circular 2021/C/20 and its addenda): the office allowance's
// monthly maximum, which follows the one ONSS accepts and so changes during the year, and the
// internet and computer allowances', not indexed. No official text gives the phone a flat amount: the
// company reimburses the professional share of the director's own bills on invoices, as declared,
// with no maximum. The circular's flat amounts are not written for directors (§7–8) and Netto cannot
// see the costs, so the conditions are the user's to confirm and it lists them with their texts.
public sealed record AllowanceRules(
    int IncomeYear,
    Source Reimbursement,
    IReadOnlyList<MonthlyMaximum> OfficeAllowanceMaxima,
    RuleAmount InternetAllowanceMonthlyMaximum,
    RuleAmount ComputerAllowanceMonthlyMaximum,
    IReadOnlyList<LeverCondition> Conditions)
{
    private static readonly Source Article32Income2026 = SharedSources.Article32Income2026 with { Passage = "director_pay_every_sum" };

    private static readonly Source Circular2021C20 = SharedSources.Circular2021C20 with { Passage = "expense_allowances" };

    private static readonly Source Circular2026C84Annex = new(
        "office_allowance_maxima",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/a9a62115-ca5d-4e3f-9f3d-49a3ae0e1265"),
        "personal-tax/spf-finances_circulaire-2026-c-84-teletravail-annexe_2026-09-09.html");

    public static AllowanceRules IncomeYear2026 { get; } = new(
        2026,
        Article32Income2026,
        [
            new MonthlyMaximum(1, new RuleAmount("office_allowance_maximum_from_january", 157.83m, Circular2026C84Annex)),
            new MonthlyMaximum(3, new RuleAmount("office_allowance_maximum_from_march", 160.99m, Circular2026C84Annex)),
            new MonthlyMaximum(9, new RuleAmount("office_allowance_maximum_from_september", 164.21m, Circular2026C84Annex)),
        ],
        new RuleAmount("internet_allowance_monthly_maximum", 20m, Circular2021C20),
        new RuleAmount("computer_allowance_monthly_maximum", 20m, Circular2021C20),
        [
            new LeverCondition("directors_case_by_case", [Circular2021C20]),
            new LeverCondition("regular_telework", [Circular2021C20]),
            new LeverCondition("no_other_company_share", [Circular2021C20]),
            new LeverCondition("office_covers_rent", [Circular2021C20]),
            new LeverCondition("phone_on_invoices", [Article32Income2026, Circular2021C20]),
        ]);

    public static AllowanceRules? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };
}

// One maximum a month of the year, from the month it starts in, 1 being January, to the month before
// the next one starts, or to December.
public sealed record MonthlyMaximum(int FromMonth, RuleAmount Amount);
