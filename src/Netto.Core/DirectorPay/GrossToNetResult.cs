namespace Netto.Core.DirectorPay;

public sealed record GrossToNetResult(
    int IncomeYear,
    decimal GrossPay,
    decimal MunicipalRate,
    decimal SocialContributions,
    decimal FlatRateCosts,
    decimal TaxableIncome,
    decimal IncomeTax,
    decimal MunicipalSurcharge,
    decimal NetInPocket,
    IReadOnlyList<Step> Steps,
    NextThousand NextThousand);

// Where the next 1,000 of gross pay goes. The four parts add up to 1,000.
public sealed record NextThousand(
    decimal SocialContributions,
    decimal IncomeTax,
    decimal MunicipalSurcharge,
    decimal NetInPocket);
