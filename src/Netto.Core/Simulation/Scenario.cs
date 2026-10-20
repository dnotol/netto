using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.Rules;

namespace Netto.Core.Simulation;

// One income year as the user describes it: the company's revenue and running costs, excluding VAT,
// what they declare about the company, the director pay and the municipal rate it is taxed at, the
// reserve they keep in the company at year end, the withholding rate they declare for the dividend
// paid out of the rest, and the profits the company kept from earlier years, after tax, which that
// dividend can pay out too (none unless given). A lever adds its own inputs here, as a property when
// it has a default: the company's advance payments on its corporate tax, none unless given.
public sealed record Scenario(
    int IncomeYear,
    decimal Revenue,
    decimal RunningCosts,
    CompanyStatus Company,
    decimal DirectorPay,
    RuleAmount MunicipalRate,
    decimal Reserve,
    WithholdingCategory DividendCategory,
    decimal EarlierProfits = 0m)
{
    public AdvancePayments AdvancePayments { get; init; } = AdvancePayments.None;

    // What the director pay and the company's profit share between them.
    public decimal ProfitBeforePay => Revenue - RunningCosts;
}
