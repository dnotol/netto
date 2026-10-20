namespace Netto.Core.Simulation;

// What the money is, as each layer taxes it. Benefits in kind and IP income join as values when their
// levers arrive.
public enum TaxableEvent
{
    // The company's profit after director pay, which corporate tax takes its share of.
    CompanyProfit,

    DirectorPay,

    Dividend,

    // The company's share of the meal, eco and sport/culture vouchers it grants the director.
    Vouchers,

    // The company's own costs it reimburses the director: the flat-rate office, internet and computer
    // allowances, and the phone costs on invoices.
    ExpenseAllowances,

    // The rent the company pays the director for the part of their home it uses as an office, income
    // from immovable property taxed with the pay.
    Rent,
}
