using Netto.Core.Rules;

namespace Netto.Core.DirectorPay;

// The director's pay statement for the income year: the taxable pay paid in twelve equal months, each
// with the withholding tax the key formula gives on it by the rules in force when it is paid, and the
// net paid; and a period for each of the year's withholding rules (WithholdingRules.Periods), the
// months it covers with the formula's lines for one of them. Each twelfth is rounded to the cent on its
// own, so twelve lines can differ from the year's figure by a few cents, which are not spread over the
// months, and the gross and the company-paid contributions over twelve can differ from the taxable pay
// over twelve by a cent.
public sealed record PayStatement(
    IReadOnlyList<PayStatementMonth> Months,
    IReadOnlyList<PayStatementPeriod> Periods);

// The months FirstMonth to LastMonth of the income year (1 is January), withheld on by the rules in
// force from RulesFrom: the key formula's lines on a month's taxable pay, the same for each of them.
// Source and AmountsUsed are the key formula and the annex's amounts those rules apply, as a step
// carries its rule.
public sealed record PayStatementPeriod(WithholdingRules Rules, int FirstMonth, int LastMonth, WithholdingResult Withholding)
{
    public int MonthCount => LastMonth - FirstMonth + 1;

    public RuleDate RulesFrom => Rules.RulesFrom;

    public Source Source => Rules.Source;

    public IReadOnlyList<RuleAmount> AmountsUsed => Rules.Amounts;
}

// One month of the statement: the month of the income year (1 is January), the payslip gross over
// twelve, the contributions the company pays on the director's behalf, the fund's fee with them, over
// twelve, the benefit in kind over twelve, the taxable pay over twelve, which is all three, the
// withholding the key formula gives on it, and the net paid, the gross over twelve less the
// withholding, since neither the company-paid contributions nor the benefit ever reach the director in
// cash.
public sealed record PayStatementMonth(
    int Month,
    decimal GrossPay,
    decimal ContributionsPaidByCompany,
    decimal BenefitInKind,
    decimal TaxablePay,
    decimal Withholding,
    decimal NetPaid);
