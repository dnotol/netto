using Netto.Core.PersonalTax;
using Netto.Core.Simulation;

namespace Netto.Core.DirectorPay;

public sealed record GrossToNetResult(
    int IncomeYear,
    decimal GrossPay,
    decimal MunicipalRate,
    decimal SocialContributions,
    decimal FlatRateCosts,
    decimal TaxableIncome,
    decimal IncomeTax,
    decimal MunicipalSurcharge,
    decimal LowEarnedIncomeCredit,
    IncomeTaxAdvancePaymentResult AdvancePayments,
    decimal NetInPocket,
    IReadOnlyList<Step> Steps,
    NextThousand NextThousand);

// Where the next 1,000 of gross pay goes. The six parts add up to 1,000. LowEarnedIncomeCredit is
// the credit the next 1,000 loses where the credit falls, and a negative part where it rises: the
// pocket then gains more than the 1,000. AdvancePayments is the surcharge it adds, less the bonus
// it adds, with the same withholding and advance payments.
public sealed record NextThousand(
    decimal SocialContributions,
    decimal IncomeTax,
    decimal MunicipalSurcharge,
    decimal LowEarnedIncomeCredit,
    decimal AdvancePayments,
    decimal NetInPocket);
