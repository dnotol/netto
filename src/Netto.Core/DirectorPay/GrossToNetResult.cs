using Netto.Core.PersonalTax;
using Netto.Core.Simulation;

namespace Netto.Core.DirectorPay;

// The pay of a director living in Region, taxed by that region's rules for IncomeYear. GrossPay is the
// pay on the payslip, the pay the split moves, and SocialContributions the contributions on it whoever
// pays them; FundFee is the social insurance fund's management fee on them, nothing without its rate,
// deducted with them as the director's own. When the company pays them on the director's behalf, the
// contributions and the fee are ContributionsPaidByCompany, nothing when the director does, and they are
// pay too: TaxablePay, what the director declares, is the gross plus them, and so is CompanyCost, what
// the pay takes out of the company. Both are the gross itself when the director pays. BenefitInKind is
// a benefit taxed as pay and never paid, a company car's, nothing without one: it is in TaxablePay,
// never in CompanyCost nor in NetInPocket. Steps is the ledger of the year's rules on the pay;
// Statement, the pay as the director sees it month by month, and Assessment, the balance the year's tax
// return leaves, are records beside the ledger, not steps. The pay's figures, its taxes, its credit,
// its net and its steps, are the pay's taken alone; OtherIncomeTax is what the income taxed with it
// adds to the year's tax, and the year's figures, AdvancePayments with its suggested payment,
// NextThousand and Assessment, are on the whole income. RequalifiedPay is movable income taxed as pay,
// paid besides the payslip: in TaxablePay, the pay's taxes and its net, never in CompanyCost nor in the
// statement's withholding; TaxablePayWithoutRequalified is the taxable pay as it would be without it,
// what art. 215's minimum pay reads. TaxedApartTax is what an income taxed apart adds to the year's tax
// on top of the pay and the rent.
public sealed record GrossToNetResult(
    int IncomeYear,
    Region Region,
    decimal GrossPay,
    decimal MunicipalRate,
    decimal SocialContributions,
    decimal FundFee,
    decimal ContributionsPaidByCompany,
    decimal BenefitInKind,
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
    Assessment Assessment,
    OtherIncomeTax OtherIncomeTax,
    decimal RequalifiedPay,
    decimal TaxablePayWithoutRequalified,
    OtherIncomeTax TaxedApartTax)
{
    // What the pay leaves once the tax the other income adds and the tax apart are paid too, those
    // incomes themselves aside: the year's net the pocket starts from.
    public decimal YearNetInPocket => NetInPocket - OtherIncomeTax.Total - TaxedApartTax.Total;
}

// What another income taxed with the pay, a home office's net rent (Income), adds to the year's
// personal tax: the income tax and the municipal surcharge on the total less the pay's own, the credit
// for low earned income it takes away by raising the total net income, and the surcharge less the
// bonus that lost credit adds on the pay's tax. Nothing without one.
public sealed record OtherIncomeTax(
    decimal Income,
    decimal IncomeTax,
    decimal MunicipalSurcharge,
    decimal LowEarnedIncomeCreditLost,
    decimal AdvancePayments)
{
    public static OtherIncomeTax None { get; } = new(0m, 0m, 0m, 0m, 0m);

    public decimal Total => IncomeTax + MunicipalSurcharge + LowEarnedIncomeCreditLost + AdvancePayments;
}

// Where the next 1,000 of gross pay goes in the year: with an income taxed with the pay, the taxes
// and the credit are the whole income's, so what the next 1,000 does to that income's tax is in them,
// and the pocket's part is net of it. The parts add up to 1,000 when the director pays the
// contributions, FundFee being what the next 1,000 adds to the fund's fee, nothing without its rate.
// When the company pays them, SocialContributions and FundFee are what the next 1,000 adds to them,
// which the company bears on top and the pocket never loses: the other five parts add up to 1,000,
// and all of them to what the next 1,000 costs the company. LowEarnedIncomeCredit is the credit the next
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
    decimal NetInPocket)
{
    public decimal FundFee { get; init; }
}
