using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.SocialContributions;

// Expected values from INASTI « Cotisations 2026 », §1 B.2: 20.50% up to 75,024.54 on a minimum
// income of 17,374.08, then 14.16% up to 110,562.42, billed per quarter at a minimum of 890.42 and a
// maximum of 5,103.05.
public class SocialContributionCalculatorTests
{
    private static readonly SocialContributionRules Rules2026 = SocialContributionRules.IncomeYear2026;

    public static TheoryData<decimal> BasesAtOrUnderMinimumIncome => [-5_000.00m, 0.00m, 10_000.00m, 17_374.08m];

    public static TheoryData<decimal> BasesAtOrAboveSecondBandCeiling => [110_562.42m, 200_000.00m];

    [Theory]
    [MemberData(nameof(BasesAtOrUnderMinimumIncome))]
    public void Annual_2026BaseAtOrUnderMinimumIncome_isFourMinimumQuarters(decimal incomeBase)
    {
        var contributions = SocialContributionCalculator.Annual(Rules2026, incomeBase);

        contributions.ShouldBe(4 * 890.42m);
    }

    [Fact]
    public void Annual_2026BaseInFirstBand_is20Point50PercentOfBase()
    {
        var contributions = SocialContributionCalculator.Annual(Rules2026, 40_000.00m);

        contributions.ShouldBe(8_200.00m);
    }

    // 20.50% × 75,024.54 = 15,380.0307: rounded once a year it would be 15,380.03, but each of the
    // four quarters is 3,845.007675, billed as 3,845.01.
    [Fact]
    public void Annual_2026BaseAtFirstBandCeiling_isRoundedPerQuarter()
    {
        var contributions = SocialContributionCalculator.Annual(Rules2026, 75_024.54m);

        contributions.ShouldBe(4 * 3_845.01m);
    }

    // 15,380.0307 + 14.16% × (90,000 − 75,024.54) = 17,500.555836, so 4,375.14 a quarter.
    [Fact]
    public void Annual_2026BaseInSecondBand_adds14Point16PercentAboveFirstBandCeiling()
    {
        var contributions = SocialContributionCalculator.Annual(Rules2026, 90_000.00m);

        contributions.ShouldBe(4 * 4_375.14m);
    }

    [Theory]
    [MemberData(nameof(BasesAtOrAboveSecondBandCeiling))]
    public void Annual_2026BaseAtOrAboveSecondBandCeiling_isFourMaximumQuarters(decimal incomeBase)
    {
        var contributions = SocialContributionCalculator.Annual(Rules2026, incomeBase);

        contributions.ShouldBe(4 * 5_103.05m);
    }

    // The fund's fee rides on each quarter it bills (AR n° 38, art. 20 §4, perceived as the contributions
    // are): at the minimum, 3.05% of 890.42 is 27.15781, billed 27.16, so 108.64 a year, where 3.05% of the
    // year's 3,561.68 rounded once would give 108.63.
    [Fact]
    public void Fee_rateOnTheMinimum_isTheRateOnEachBilledQuarterRoundedToTheCent()
    {
        var fee = SocialContributionCalculator.Fee(4 * 890.42m, 0.0305m);

        fee.ShouldBe(4 * 27.16m);
    }

    // 3.05% of a quarter of 2,050.00 is 62.525, billed 62.53: 250.12 a year, not the 250.10 of 8,200.00.
    [Fact]
    public void Fee_rateInTheFirstBand_roundsEachQuarterHalfAwayFromZero()
    {
        var fee = SocialContributionCalculator.Fee(8_200.00m, 0.0305m);

        fee.ShouldBe(4 * 62.53m);
    }

    [Fact]
    public void Fee_noRate_isNothing()
    {
        var fee = SocialContributionCalculator.Fee(8_200.00m, null);

        (fee, decimal.IsNegative(fee)).ShouldBe((0m, false));
    }

    [Fact]
    public void Fee_rateOfNothing_isNothing()
    {
        var fee = SocialContributionCalculator.Fee(8_200.00m, 0m);

        (fee, decimal.IsNegative(fee)).ShouldBe((0m, false));
    }
}
