using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.Vouchers;

namespace Netto.Core.Simulation;

// The year from the company's profit before pay, what the vouchers leave of what it earns, to the
// director's pocket. Corporate tax, social contributions, personal tax, net in pocket, the vouchers in
// it, and what stays in the company add up to what the company earns before it pays its director
// anything (Scenario.ProfitBeforeDirector) plus the profits kept from earlier years; the corporate tax
// among them includes the surcharge the advance payments left. The dividend is what the profit after
// tax and those earlier profits leave once the reserve is kept; the largest the year allows is both
// together, less a loss, never below zero.
public sealed record YearResult(
    Scenario Scenario,
    decimal ProfitBeforePay,
    VoucherResult Vouchers,
    CorporateTaxResult Company,
    AdvancePaymentResult AdvancePayments,
    GrossToNetResult DirectorPay,
    DividendResult Dividend,
    decimal CorporateTax,
    decimal SocialContributions,
    decimal PersonalTax,
    decimal NetInPocket,
    decimal KeptInCompany,
    decimal ProfitAfterTax,
    decimal MaximumDividend,
    IReadOnlyList<Step> Steps)
{
    // What the split leaves the company once it bears the pay's cost, before its tax. Its taxable
    // income adds back what it cannot deduct of the vouchers.
    public decimal ProfitAfterPay => ProfitBeforePay - DirectorPay.CompanyCost;
}
