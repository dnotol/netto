namespace Netto.Core.Simulation;

// What the money is, as each layer taxes it. Benefits in kind, rent and IP income join as values when
// their levers arrive.
public enum TaxableEvent
{
    // The company's profit after director pay, which corporate tax takes its share of.
    CompanyProfit,

    DirectorPay,

    Dividend,
}
