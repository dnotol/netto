using Netto.Core.Rules;

namespace Netto.Core.DirectorPay;

// The director's pay statement for the income year: the taxable pay paid in twelve equal months, each
// with the withholding tax the key formula gives on it and the net paid, and the formula's lines for
// one month, the same for all twelve. Source and AmountsUsed are the key formula and the annex's
// amounts, as a step carries its rule. Each twelfth is rounded to the cent on its own, so twelve lines
// can differ from the year's figure by a few cents, which are not spread over the months, and the gross
// and the company-paid contributions over twelve can differ from the taxable pay over twelve by a cent.
// From UnmodelledRulesFrom, rules exist that Netto does not model: the months from that date are
// flagged and still use these rules.
public sealed record PayStatement(
    IReadOnlyList<PayStatementMonth> Months,
    WithholdingResult Withholding,
    RuleDate UnmodelledRulesFrom,
    Source Source,
    IReadOnlyList<RuleAmount> AmountsUsed);

// One month of the statement: the month of the income year (1 is January), the payslip gross over
// twelve, the contributions the company pays on the director's behalf over twelve, the taxable pay
// over twelve, which is both of them, the withholding the key formula gives on it, and the net paid,
// the gross over twelve less the withholding, since the company-paid contributions never reach the
// director. UnmodelledRules says rules exist for the month that Netto does not model.
public sealed record PayStatementMonth(
    int Month,
    decimal GrossPay,
    decimal ContributionsPaidByCompany,
    decimal TaxablePay,
    decimal Withholding,
    decimal NetPaid,
    bool UnmodelledRules);
