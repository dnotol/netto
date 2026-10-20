using Netto.Core.Vouchers;
using Shouldly;

namespace Netto.Tests.Vouchers;

// CIR 92 art. 38, §1, al. 1, 25°, art. 38/1 and art. 53, 14° for income 2026. Made-up grants;
// expected values derived from the articles: the company's share of a meal voucher is the 8.91
// maximum, of which it deducts 4, and eco and sport/culture vouchers are not deductible at all.
public class VoucherCalculatorTests
{
    private static readonly VoucherRules Rules2026 = VoucherRules.IncomeYear2026;

    // 220 × 8.91 reaches the pocket, 220 × 4 is deducted and 220 × 4.91 is not.
    [Fact]
    public void Compute_2026MealVouchersOn220Days_deductsFourOfEach()
    {
        var result = Compute(new VoucherGrants(220, 0m, 0m));

        (result.MealVouchers, result.Total, result.Deductible, result.Disallowed).ShouldBe((1_960.20m, 1_960.20m, 880.00m, 1_080.20m));
    }

    [Fact]
    public void Compute_2026EcoAndSportCultureAtTheirMaximum_deductsNoneOfThem()
    {
        var result = Compute(new VoucherGrants(0, 250m, 100m));

        (result.EcoVouchers, result.SportCultureVouchers, result.Total, result.Deductible, result.Disallowed)
            .ShouldBe((250m, 100m, 350m, 0m, 350m));
    }

    [Fact]
    public void Compute_2026AllThree_addsThemUp()
    {
        var result = Compute(new VoucherGrants(220, 250m, 100m));

        (result.Total, result.Deductible, result.Disallowed).ShouldBe((2_310.20m, 880.00m, 1_430.20m));
    }

    [Fact]
    public void Compute_2026None_isNothing()
    {
        Compute(VoucherGrants.None).ShouldBe(new VoucherResult(MealVouchers: 0m, EcoVouchers: 0m, SportCultureVouchers: 0m, Deductible: 0m));
    }

    // 2026 has 365 days.
    [Fact]
    public void Compute_2026EveryDayOfTheYear_isAccepted()
    {
        Compute(new VoucherGrants(365, 0m, 0m)).MealVouchers.ShouldBe(3_252.15m);
    }

    public static TheoryData<int, decimal, decimal, string> GrantsOutsideTheRules => new()
    {
        { -1, 0m, 0m, VoucherCalculator.MealVoucherDaysNegative },
        { 366, 0m, 0m, VoucherCalculator.MealVoucherDaysAboveYear },
        { 0, -0.01m, 0m, VoucherCalculator.EcoVouchersNegative },
        { 0, 250.01m, 0m, VoucherCalculator.EcoVouchersAboveMaximum },
        { 0, 0m, -0.01m, VoucherCalculator.SportCultureVouchersNegative },
        { 0, 0m, 100.01m, VoucherCalculator.SportCultureVouchersAboveMaximum },
    };

    [Theory]
    [MemberData(nameof(GrantsOutsideTheRules))]
    public void Compute_2026GrantOutsideTheRules_isRefused(int days, decimal eco, decimal sportCulture, string expected)
    {
        var (value, error) = VoucherCalculator.Compute(Rules2026, new VoucherGrants(days, eco, sportCulture));

        (value, error).ShouldBe((null, expected));
    }

    // What the company pays for the vouchers as given, which the split's ceiling reads while one
    // of them is refused.
    [Fact]
    public void Cost_2026_isWhatTheCompanyPaysAsGivenRefusedOrNot()
    {
        VoucherCalculator.Cost(Rules2026, new VoucherGrants(220, 300m, 100m)).ShouldBe(2_360.20m);
    }

    private static VoucherResult Compute(VoucherGrants grants)
    {
        var (value, error) = VoucherCalculator.Compute(Rules2026, grants);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
