namespace Netto.Core.Rent;

// The part of their home the director lets to the company as an office, unfurnished, as the user
// declares it: the dwelling's cadastral income, not indexed, as the property-tax notice gives it; the
// share of it let to the company, a fraction (12% is 0.12), theirs to justify, usually by floor area;
// and the yearly rent the company pays. None unless given; a rent is paid only once it is more than
// nothing.
public sealed record HomeOfficeRent(decimal CadastralIncome, decimal ShareLet, decimal Rent)
{
    public static HomeOfficeRent None { get; } = new(0m, 0m, 0m);

    public bool Any => Rent != 0m;
}
