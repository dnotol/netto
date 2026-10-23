using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.Rent;
using Netto.Core.Vouchers;

namespace Netto.Core.Simulation;

// The year from the company's profit before pay, what the vouchers, the expense allowances, the rent
// of a home office and a company car leave of what it earns, to the director's pocket. Corporate tax,
// social contributions, personal tax, the rent's included, net in pocket, the vouchers, the allowances
// and the rent in it and the car's contribution out of it, the muddy pocket, what the company spends
// on the car, and what stays in the company add up to what the company earns before it pays its
// director anything (Scenario.ProfitBeforeDirector) plus the profits kept from earlier years; the
// corporate tax among them includes the surcharge the advance payments left. Rent is the rent as the
// rules take it, Car the car, all zero without one; the pay's own figures are DirectorPay's, the car's
// benefit taxed in them, and what the rent adds to the personal tax its OtherIncomeTax. The disallowed
// expenses are what the company cannot deduct of what it pays the director besides pay and what it
// adds back of the car's benefit, added to its taxable income. The dividend is what the profit after
// tax and those earlier profits leave once the reserve is kept; the largest the year allows is both
// together, less a loss, never below zero. Copyright is the royalty as the rules took it, all nothing
// without one: its part taxed as pay is in the pay's figures, and what its part taxed apart adds to the
// personal tax is DirectorPay's TaxedApartTax. Scenario is the scenario as the year ran it: a royalty
// that follows the pay holds what it came to at the pay, still following it.
public sealed record YearResult(
    Scenario Scenario,
    decimal ProfitBeforePay,
    VoucherResult Vouchers,
    RentResult Rent,
    CarResult Car,
    CopyrightResult Copyright,
    decimal DisallowedExpenses,
    CorporateTaxResult Company,
    AdvancePaymentResult AdvancePayments,
    GrossToNetResult DirectorPay,
    DividendResult Dividend,
    decimal CorporateTax,
    decimal SocialContributions,
    decimal PersonalTax,
    decimal MuddyPocket,
    decimal NetInPocket,
    decimal KeptInCompany,
    decimal ProfitAfterTax,
    decimal MaximumDividend,
    IReadOnlyList<Step> Steps)
{
    // What the split leaves the company once it bears the pay's cost, before its tax. Its taxable
    // income adds back the disallowed expenses.
    public decimal ProfitAfterPay => ProfitBeforePay - DirectorPay.CompanyCost;
}
