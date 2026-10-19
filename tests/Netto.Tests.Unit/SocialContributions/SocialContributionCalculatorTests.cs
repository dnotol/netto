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
}
