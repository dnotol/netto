using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;

namespace Netto.Core.Simulation;

// One income year as the user describes it: the company's revenue and running costs, excluding VAT,
// what they declare about the company, the director pay and the municipal rate it is taxed at, the
// reserve they keep in the company at year end, the withholding rate they declare for the dividend
// paid out of the rest, and the profits the company kept from earlier years, after tax, which that
// dividend can pay out too (none unless given). A lever adds its own inputs here, as a property when
// it has a default: the company's advance payments on its corporate tax, what was withheld on the
// director pay and paid in advance on its income tax, none unless given, who pays the director's
// social contributions, the director unless given, and the vouchers it grants the director, none
// unless given.
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

    public TaxPrepayments TaxPrepayments { get; init; } = TaxPrepayments.None;

    public ContributionsPaidBy ContributionsPaidBy { get; init; } = ContributionsPaidBy.Director;

    public VoucherGrants Vouchers { get; init; } = VoucherGrants.None;

    // What the company earns before it pays its director anything, pay or vouchers. What the director
    // pay and the company's profit share is what the vouchers leave of it (YearSimulation.ProfitBeforePay).
    public decimal ProfitBeforeDirector => Revenue - RunningCosts;
}
