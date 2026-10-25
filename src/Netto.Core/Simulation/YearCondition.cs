using Netto.Core.CorporateTax;

namespace Netto.Core.Simulation;

// A condition one part of the year sets on another, as the year meets it (YearConditions): whether it
// holds in this plan, and the figures it is measured by. Only Core declares a kind of condition, so a
// page that draws them names every kind there is; a new kind is a record here, its place in
// YearConditions.Of and its case wherever a page draws it.
public abstract record YearCondition
{
    private protected YearCondition(bool holds) => Holds = holds;

    public bool Holds { get; }
}

// Art. 215 al. 3, 4°, which the pay sets on the corporate rate: the pay reaches the minimum, or the
// company's taxable income does not exceed it. Test is what the article found (MinimumPayMet,
// TaxableIncomeNotAbovePay or MinimumPayNotMet), MinimumPay the year's minimum, Pay the pay as it reads
// it, the taxable pay with the benefits in kind, and TaxableIncome the company's.
public sealed record PayKeepsReducedRate(ReducedRateCondition Test, decimal MinimumPay, decimal Pay, decimal TaxableIncome)
    : YearCondition(Test.AppliesReducedRate());

// Art. 215 al. 3, 5°, which flat-rate benefits such as a company car's set on the corporate rate: they
// are worth no more than their Share of the total Pay, the Benefits in it, as the year found.
public sealed record BenefitsWithinShareOfPay(bool Within, decimal Benefits, decimal Pay, decimal Share) : YearCondition(Within);

// The office allowance covers the room, its rent included (circular 2021/C/20, §31): a rent and an
// office allowance never go together. The engine refuses a rent beside one, so a year that ran keeps it,
// with the Rent or the OfficeAllowance it pays.
public sealed record NoOfficeAllowanceBesideRent(decimal Rent, decimal OfficeAllowance) : YearCondition(true);

// A copyright royalty, which the company deducts, lowers the taxable income art. 215 al. 3, 4° compares
// the pay with, and so the smallest pay that keeps the reduced rate: With the royalty as the plan holds
// it, fixed or following the pay (FollowsPay), Without for the same year without it, each null where no
// pay the split gives keeps it, never both. It holds where the royalty lowers that pay.
public sealed record RoyaltyLowersPayKeepingReducedRate(decimal? With, decimal? Without, bool FollowsPay)
    : YearCondition(With is { } with && (Without is not { } without || with < without));

// The Reserve the user keeps in the company, which sets on the pay: the most the year can pay out
// (MaximumDividend) covers it.
public sealed record PayOutCoversReserve(decimal Reserve, decimal MaximumDividend) : YearCondition(YearResult.ReserveKept(MaximumDividend, Reserve));
