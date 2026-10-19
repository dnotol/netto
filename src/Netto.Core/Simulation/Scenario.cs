using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.Rules;

namespace Netto.Core.Simulation;

// One income year as the user describes it: the company's revenue and running costs, excluding VAT,
// what they declare about the company, the director pay and the municipal rate it is taxed at, the
// reserve they keep in the company out of the year's profit after tax, and the withholding rate they
// declare for the dividend paid out of the rest. A lever adds its own inputs here.
public sealed record Scenario(
    int IncomeYear,
    decimal Revenue,
    decimal RunningCosts,
    CompanyStatus Company,
    decimal DirectorPay,
    RuleAmount MunicipalRate,
    decimal Reserve,
    WithholdingCategory DividendCategory)
{
    // What the director pay and the company's profit share between them.
    public decimal ProfitBeforePay => Revenue - RunningCosts;
}
