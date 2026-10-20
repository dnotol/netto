using Netto.Core.PersonalTax;
using Netto.Core.Simulation;

namespace Netto.Core.DirectorPay;

// GrossPay is the pay on the payslip, the pay the split moves, and SocialContributions the
// contributions on it whoever pays them. When the company pays them on the director's behalf they are
// ContributionsPaidByCompany, nothing when the director does, and they are pay too: TaxablePay, what
// the director declares, is the gross plus them, and so is CompanyCost, what the pay takes out of the
// company. Both are the gross itself when the director pays. Steps is the ledger of the year's rules
// on the pay; Statement, the pay as the director sees it month by month, and Assessment, the balance
// the year's tax return leaves, are records beside the ledger, not steps.
public sealed record GrossToNetResult(
    int IncomeYear,
    decimal GrossPay,
    decimal MunicipalRate,
    decimal SocialContributions,
    decimal ContributionsPaidByCompany,
    decimal TaxablePay,
    decimal CompanyCost,
    decimal FlatRateCosts,
    decimal TaxableIncome,
    decimal IncomeTax,
    decimal MunicipalSurcharge,
    decimal LowEarnedIncomeCredit,
    IncomeTaxAdvancePaymentResult AdvancePayments,
    decimal NetInPocket,
    IReadOnlyList<Step> Steps,
    NextThousand NextThousand,
    PayStatement Statement,
    Assessment Assessment);

// Where the next 1,000 of gross pay goes. The six parts add up to 1,000 when the director pays the
// contributions. When the company pays them, SocialContributions is what the next 1,000 adds to them,
// which the company bears on top and the pocket never loses: the other five parts add up to 1,000,
// and all six to what the next 1,000 costs the company. LowEarnedIncomeCredit is the credit the next
// 1,000 loses where the credit falls, and a negative part where it rises: the pocket then gains more
// than the 1,000. AdvancePayments is the surcharge it adds, less the bonus it adds, with the same
// advance payments and the same amount withheld, or, when the key formula gives it, the formula's
// withholding on the next 1,000's own pay.
public sealed record NextThousand(
    decimal SocialContributions,
    decimal IncomeTax,
    decimal MunicipalSurcharge,
    decimal LowEarnedIncomeCredit,
    decimal AdvancePayments,
    decimal NetInPocket);
