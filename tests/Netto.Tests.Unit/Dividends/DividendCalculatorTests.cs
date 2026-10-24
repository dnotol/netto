using Netto.Core.Dividends;
using Shouldly;

namespace Netto.Tests.Dividends;

// CIR 92 art. 269 and art. 21, 14° for income 2025 and 2026. Made-up dividends; the 40,000 cases are
// the Lever catalogue's VVPRbis worked example. Expected values derived from the articles.
public class DividendCalculatorTests
{
    private static readonly DividendRules Rules2025 = DividendRules.IncomeYear2025;
    private static readonly DividendRules Rules2026 = DividendRules.IncomeYear2026;

    [Fact]
    public void Compute_2026VvprbisThirdYearOnwards_withholds18PercentAndRefundsItOn833()
    {
        var result = DividendCalculator.Compute(Rules2026, 40_000m, WithholdingCategory.VvprbisThirdYearOnwards);

        result.ShouldBe(new DividendResult(
            Dividend: 40_000m, Rate: 0.18m, Withholding: 7_200.00m, ExemptionRefund: 149.94m, NetDividend: 32_949.94m));
    }

    [Fact]
    public void Compute_2026Standard_withholds30Percent()
    {
        var result = DividendCalculator.Compute(Rules2026, 40_000m, WithholdingCategory.Standard);

        (result.Withholding, result.ExemptionRefund, result.NetDividend).ShouldBe((12_000.00m, 249.90m, 28_249.90m));
    }

    [Fact]
    public void Compute_2026VvprbisSecondYear_withholds20Percent()
    {
        var result = DividendCalculator.Compute(Rules2026, 40_000m, WithholdingCategory.VvprbisSecondYear);

        (result.Withholding, result.ExemptionRefund, result.NetDividend).ShouldBe((8_000.00m, 166.60m, 32_166.60m));
    }

    [Fact]
    public void Compute_2025VvprbisThirdYearOnwards_withholds15Percent()
    {
        var result = DividendCalculator.Compute(Rules2025, 40_000m, WithholdingCategory.VvprbisThirdYearOnwards);

        (result.Withholding, result.ExemptionRefund).ShouldBe((6_000.00m, 124.95m));
    }

    [Fact]
    public void Compute_2026DividendUnder833_refundsAllTheWithholding()
    {
        var result = DividendCalculator.Compute(Rules2026, 500m, WithholdingCategory.VvprbisThirdYearOnwards);

        (result.Withholding, result.ExemptionRefund, result.NetDividend).ShouldBe((90.00m, 90.00m, 500m));
    }

    [Fact]
    public void Compute_2026NoDividend_isNothing()
    {
        var result = DividendCalculator.Compute(Rules2026, 0m, WithholdingCategory.Standard);

        (result.Withholding, result.ExemptionRefund, result.NetDividend).ShouldBe((0m, 0m, 0m));
    }

    // 30% × 1,000.05 = 300.015.
    [Fact]
    public void Compute_2026Withholding_roundsHalfAwayFromZero()
    {
        var result = DividendCalculator.Compute(Rules2026, 1_000.05m, WithholdingCategory.Standard);

        result.Withholding.ShouldBe(300.02m);
    }

    [Theory]
    [InlineData(2024)]
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public void For_yearWithoutRules_isNull(int incomeYear)
    {
        DividendRules.For(incomeYear).ShouldBeNull();
    }
}
