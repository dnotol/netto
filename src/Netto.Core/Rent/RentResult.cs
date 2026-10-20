namespace Netto.Core.Rent;

// The rent as the rules take it, each line rounded to the cent. CadastralIncomeLet is the cadastral
// income of the part let; Cap the most the company can pay before the excess is director pay, taken
// down to the cent; IndexedCadastralIncome and Floor the least the rent is taxed on, that part's
// indexed cadastral income plus 40%. TaxableRent is the rent, never under the floor, Charges the flat
// charges on it and NetRent what is left, the income taxed with the pay. Without a rent nothing is
// let, and nothing is taxed.
public sealed record RentResult(
    decimal Rent,
    decimal CadastralIncomeLet,
    decimal Cap,
    decimal IndexedCadastralIncome,
    decimal Floor,
    decimal TaxableRent,
    decimal Charges,
    decimal NetRent);
