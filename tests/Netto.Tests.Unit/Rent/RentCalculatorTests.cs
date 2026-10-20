using Netto.Core.Rent;
using Shouldly;

namespace Netto.Tests.Rent;

// Made-up dwellings. The 2025 case is the rent of Tax-Calc T13 and T14 (2026-10-20): a cadastral
// income of 144 let for 5/3 × 144 × 5.63 = 1,351.20, whose « R.C. bâtiments » is 810.72. The 2026
// cases are derived from the official amounts: 12% of a cadastral income of 1,200 is 144, capped at
// 5/3 × 144 × 5.75 = 1,380.00, whose 40% of charges, 552.00, is the limit 2/3 × 144 × 5.75 exactly,
// and whose floor is 144 × 2.3000 × 1.40 = 463.68.
public class RentCalculatorTests
{
    private static readonly RentRules Rules2025 = RentRules.IncomeYear2025;
    private static readonly RentRules Rules2026 = RentRules.IncomeYear2026;

    private static HomeOfficeRent TwelvePercentOf1200(decimal rent) => new(1_200m, 0.12m, rent);

    [Fact]
    public void Compute_2025TaxCalcT13RentAtTheCap_isTaxedOnTheRentLessItsCappedCharges()
    {
        var result = Compute(Rules2025, TwelvePercentOf1200(1_351.20m));

        (result.CadastralIncomeLet, result.Cap, result.TaxableRent, result.Charges, result.NetRent)
            .ShouldBe((144.00m, 1_351.20m, 1_351.20m, 540.48m, 810.72m));
    }

    [Fact]
    public void Compute_2026DerivedRentAtTheCap_hasChargesAtTheirLimit()
    {
        var result = Compute(Rules2026, TwelvePercentOf1200(1_380.00m));

        (result.Rent, result.Cap, result.TaxableRent, result.Charges, result.NetRent).ShouldBe((1_380.00m, 1_380.00m, 1_380.00m, 552.00m, 828.00m));
    }

    [Fact]
    public void Compute_2026DerivedRentACentAboveTheCap_isRefused()
    {
        RentCalculator.Compute(Rules2026, TwelvePercentOf1200(1_380.01m), 0m).ShouldBe((null, RentCalculator.RentAboveCap));
    }

    // Under the floor the rent is taxed on the floor, the indexed cadastral income plus 40%, and its
    // charges are 40% of that.
    [Fact]
    public void Compute_2026DerivedRentUnderTheFloor_isTaxedOnTheFloor()
    {
        var result = Compute(Rules2026, TwelvePercentOf1200(400.00m));

        (result.IndexedCadastralIncome, result.Floor, result.TaxableRent, result.Charges, result.NetRent)
            .ShouldBe((331.20m, 463.68m, 463.68m, 185.47m, 278.21m));
    }

    // Between the floor and the cap: 40% of 1,000.00 is under the limit of 552.00.
    [Fact]
    public void Compute_2026DerivedRentBetweenTheFloorAndTheCap_takesFortyPercentOfCharges()
    {
        var result = Compute(Rules2026, TwelvePercentOf1200(1_000.00m));

        (result.TaxableRent, result.Charges, result.NetRent).ShouldBe((1_000.00m, 400.00m, 600.00m));
    }

    // 5/3 × 100 × 5.75 is 958.333…: the cap is taken down to the cent, so the most the card offers
    // stays at or below it, and a cent more is refused.
    [Fact]
    public void Cap_2026NotAWholeCent_isTakenDownToTheCent()
    {
        RentCalculator.Cap(Rules2026, new HomeOfficeRent(100m, 1m, 0m)).ShouldBe(958.33m);
    }

    [Fact]
    public void Compute_2026RentAtTheCapTakenDown_isAcceptedAndACentMoreRefused()
    {
        (RentCalculator.Compute(Rules2026, new HomeOfficeRent(100m, 1m, 958.33m), 0m).Error,
                RentCalculator.Compute(Rules2026, new HomeOfficeRent(100m, 1m, 958.34m), 0m).Error)
            .ShouldBe((null, RentCalculator.RentAboveCap));
    }

    // The share is a fraction of the dwelling's cadastral income, the let part's to the cent.
    [Fact]
    public void Compute_2026ShareOfACadastralIncome_takesItsPartToTheCent()
    {
        var result = Compute(Rules2026, new HomeOfficeRent(1_234m, 0.155m, 500m));

        result.CadastralIncomeLet.ShouldBe(191.27m);
    }

    // No rent, no letting: nothing is taxed, whatever the cadastral income.
    [Fact]
    public void Compute_2026NoRent_taxesNothing()
    {
        var result = Compute(Rules2026, TwelvePercentOf1200(0m));

        (result.TaxableRent, result.Charges, result.NetRent).ShouldBe((0m, 0m, 0m));
    }

    // The office allowance covers the room, its rent included (circular 2021/C/20, §31).
    [Fact]
    public void Compute_2026RentWithAnOfficeAllowance_isRefused()
    {
        RentCalculator.Compute(Rules2026, TwelvePercentOf1200(1_000m), 0.01m).ShouldBe((null, RentCalculator.RentWithOfficeAllowance));
    }

    [Fact]
    public void Compute_2026OfficeAllowanceWithoutRent_isAccepted()
    {
        RentCalculator.Compute(Rules2026, TwelvePercentOf1200(0m), 1_938.44m).Error.ShouldBeNull();
    }

    public static TheoryData<decimal, decimal, decimal, string> OutsideTheRules => new()
    {
        { -1m, 0.12m, 100m, RentCalculator.CadastralIncomeNegative },
        { 1_200m, -0.01m, 100m, RentCalculator.ShareLetOutOfRange },
        { 1_200m, 1.01m, 100m, RentCalculator.ShareLetOutOfRange },
        { 1_200m, 0.12m, -0.01m, RentCalculator.RentNegative },
        { 0m, 0m, 0.01m, RentCalculator.RentAboveCap },
    };

    [Theory]
    [MemberData(nameof(OutsideTheRules))]
    public void Compute_2026OutsideTheRules_isRefusedWithItsKey(decimal cadastralIncome, decimal shareLet, decimal rent, string expected)
    {
        RentCalculator.Compute(Rules2026, new HomeOfficeRent(cadastralIncome, shareLet, rent), 0m).ShouldBe((null, expected));
    }

    [Fact]
    public void Compute_2026WholeDwellingLet_isAccepted()
    {
        RentCalculator.Compute(Rules2026, new HomeOfficeRent(144m, 1m, 1_380m), 0m).Error.ShouldBeNull();
    }

    private static RentResult Compute(RentRules rules, HomeOfficeRent rent)
    {
        var (value, error) = RentCalculator.Compute(rules, rent, 0m);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
