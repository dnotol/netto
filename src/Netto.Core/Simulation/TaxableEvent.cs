namespace Netto.Core.Simulation;

// What the money is, as each layer taxes it. Other benefits in kind join as values when their levers
// arrive.
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

    // A car the company puts at the director's disposal for private use: its costs to the company, its
    // benefit in kind, taxed as pay, the director's contribution and what the company adds back.
    CompanyCar,

    // A copyright royalty the company pays the director for the rights on their works, movable income:
    // what the company deducts, the part within the limits taxed apart, and the part beyond them taxed
    // as pay.
    CopyrightRoyalty,
}
