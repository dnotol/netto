namespace Netto.Core.Rent;

public static class RentCalculator
{
    public const string CadastralIncomeNegative = "cadastral_income_negative";
    public const string ShareLetOutOfRange = "share_let_out_of_range";
    public const string RentNegative = "rent_negative";
    public const string RentWithOfficeAllowance = "rent_with_office_allowance";
    public const string RentAboveCap = "rent_above_cap";

    // The rent as the rules take it, or the key of what they refuse. An office allowance paid beside a
    // rent is refused: it already covers the room, its rent included (circular 2021/C/20, §31). A rent
    // above the cap is refused rather than taxed: the excess would be director pay, with its social
    // contributions and no charges (art. 32, al. 2, 3°), which Netto does not model.
    public static (RentResult? Value, string? Error) Compute(RentRules rules, HomeOfficeRent rent, decimal officeAllowance)
    {
        if (rent.CadastralIncome < 0m)
            return (null, CadastralIncomeNegative);
        if (rent.ShareLet is < 0m or > 1m)
            return (null, ShareLetOutOfRange);
        if (rent.Rent < 0m)
            return (null, RentNegative);
        if (rent.Any && officeAllowance > 0m)
            return (null, RentWithOfficeAllowance);

        var cap = Cap(rules, rent);
        if (rent.Rent > cap)
            return (null, RentAboveCap);

        var cadastralIncomeLet = CadastralIncomeLet(rent);
        var indexed = Money.RoundToCent(cadastralIncomeLet * rules.IndexationCoefficient.Value);
        var floor = Money.RoundToCent(indexed * (1m + rules.FloorUplift.Value));
        var taxableRent = rent.Any ? Math.Max(rent.Rent, floor) : 0m;
        var chargesLimit = Money.RoundToCent(cadastralIncomeLet * rules.RevaluationCoefficient.Value * rules.ChargesLimitMultiplier.Value);
        var charges = Math.Min(Money.RoundToCent(taxableRent * rules.ChargesRate.Value), chargesLimit);

        return (new RentResult(rent.Rent, cadastralIncomeLet, cap, indexed, floor, taxableRent, charges, taxableRent - charges), null);
    }

    // The most the company can pay for the part let before the excess is director pay: 5/3 of its
    // cadastral income revalued by art. 13's coefficient, taken down to the cent so that it never
    // exceeds the law's (decided in this slice: no text rounds it). The multiplier is held a hair above
    // 5/3, so the product is never below the exact cap and a whole cent stays whole.
    public static decimal Cap(RentRules rules, HomeOfficeRent rent) =>
        Math.Floor(CadastralIncomeLet(rent) * rules.RevaluationCoefficient.Value * rules.CapMultiplier.Value * 100m) / 100m;

    // The dwelling's cadastral income times the share let, to the cent.
    public static decimal CadastralIncomeLet(HomeOfficeRent rent) => Money.RoundToCent(rent.CadastralIncome * rent.ShareLet);
}
