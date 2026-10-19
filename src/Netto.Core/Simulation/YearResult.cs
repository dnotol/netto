using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;

namespace Netto.Core.Simulation;

// The year from the company's profit before pay to the director's pocket. Corporate tax, social
// contributions, personal tax, net in pocket and what stays in the company add up to that profit.
// The dividend is what the profit after tax leaves once the reserve is kept; the largest the year
// allows is its whole profit after tax, none in a loss year (steady state).
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
