using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;

namespace Netto.Core.Simulation;

// The year from the company's profit before pay to the director's pocket. Corporate tax, social
// contributions, personal tax, net in pocket and what stays in the company add up to that profit
// plus the profits kept from earlier years. The dividend is what the profit after tax and those
// earlier profits leave once the reserve is kept; the largest the year allows is both together, less
// a loss, never below zero.
public sealed record YearResult(
    Scenario Scenario,
    decimal ProfitBeforePay,
    CorporateTaxResult Company,
    GrossToNetResult DirectorPay,
    DividendResult Dividend,
    decimal CorporateTax,
    decimal SocialContributions,
    decimal PersonalTax,
    decimal NetInPocket,
    decimal KeptInCompany,
    decimal ProfitAfterTax,
    decimal MaximumDividend,
    IReadOnlyList<Step> Steps);
