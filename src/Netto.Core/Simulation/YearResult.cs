using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;

namespace Netto.Core.Simulation;

// The year from the company's profit before pay to the director's pocket. Corporate tax, social
// contributions, personal tax, net in pocket and what stays in the company add up to that profit.
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
    IReadOnlyList<Step> Steps);
