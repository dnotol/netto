namespace Netto.Core.Dividends;

// The gross dividend, the rate withheld, the withholding, what the exemption gives back through the
// personal return, and what reaches the director.
public sealed record DividendResult(decimal Dividend, decimal Rate, decimal Withholding, decimal ExemptionRefund, decimal NetDividend);
