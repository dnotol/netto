using Netto.Core.Copyright;
using Netto.Core.MovableIncome;
using Shouldly;

namespace Netto.Tests.Copyright;

// Made-up royalties, derived from art. 37 al. 2, art. 171, 2°bis and art. 269 §1, 4° and 1° (revenus
// 2026): the part within the limits is the smallest of the royalty, 30% of the royalty and the gross pay
// taken down to the cent, and 77,220; the rest is taxed as pay. The company withholds 15% on the part
// within them and 30% on the rest.
public class CopyrightCalculatorTests
{
    private static readonly CopyrightRules Rules2026 = CopyrightRules.IncomeYear2026;

    private static CopyrightResult Compute(decimal royalty, decimal grossPay, bool averageWithinCap = true, bool attestation = false) =>
        CopyrightCalculator.Compute(Rules2026, new CopyrightRoyalty(royalty) { AverageWithinCap = averageWithinCap, HoldsAttestation = attestation }, grossPay)
            .Value.ShouldNotBeNull();

    [Fact]
    public void Compute_royaltyWithinTheLimits_isAllTaxedApart()
    {
        var result = Compute(10_000m, 50_000m);

        (result.RelativeLimit, result.Cap, result.WithinLimits, result.TaxedAsPay, result.Bound).ShouldBe((18_000.00m, 77_220m, 10_000m, 0m, CopyrightBound.None));
    }

    [Fact]
    public void Compute_royaltyWithinTheLimits_withholds15Percent()
    {
        var result = Compute(10_000m, 50_000m);

        (result.WithheldWithinLimits, result.WithheldBeyondLimits, result.Withheld).ShouldBe((1_500.00m, 0m, 1_500.00m));
    }

    [Fact]
    public void Compute_royaltyWithinTheLimits_isTaxedApartAtTheYearsRate()
    {
        var result = Compute(10_000m, 50_000m);

        result.TaxedApart.ShouldBe(new Core.PersonalTax.IncomeTaxedApart(10_000m, Rules2026.TaxedApartRate));
    }

    // Royalty 20,000 beside 30,000 of pay: 30% of 50,000 is 15,000 within, 5,000 taxed as pay; 2,250
    // withheld on the first and 1,500 on the rest (Tax-Calc T18's figures).
    [Fact]
    public void Compute_royaltyAbove30PercentOfTheTotal_taxesTheExcessAsPay()
    {
        var result = Compute(20_000m, 30_000m);

        (result.WithinLimits, result.TaxedAsPay, result.Bound).ShouldBe((15_000.00m, 5_000.00m, CopyrightBound.Relative));
    }

    [Fact]
    public void Compute_royaltyAbove30PercentOfTheTotal_withholds30PercentOnTheExcess()
    {
        var result = Compute(20_000m, 30_000m);

        (result.WithheldWithinLimits, result.WithheldBeyondLimits, result.Withheld).ShouldBe((2_250.00m, 1_500.00m, 3_750.00m));
    }

    // 3/7 of the pay is exactly 30% of the total: 30,000 beside 70,000 is within; a cent more is not,
    // and 30% of 100,000.01 taken down to the cent leaves that cent beyond.
    [Theory]
    [InlineData(30_000.00, 30_000.00, 0.00)]
    [InlineData(30_000.01, 30_000.00, 0.01)]
    public void Compute_royaltyAt3SeventhsOfThePay_isTheEdgeOfTheRelativeLimit(decimal royalty, decimal within, decimal asPay)
    {
        var result = Compute(royalty, 70_000m);

        (result.WithinLimits, result.TaxedAsPay).ShouldBe((within, asPay));
    }

    [Fact]
    public void Compute_royaltyAboveTheCap_keepsTheCapWithinAndTaxesTheRestAsPay()
    {
        var result = Compute(100_000m, 300_000m);

        (result.RelativeLimit, result.WithinLimits, result.TaxedAsPay, result.Bound).ShouldBe((120_000.00m, 77_220m, 22_780m, CopyrightBound.Cap));
    }

    [Fact]
    public void Compute_royaltyAboveTheCap_withholds15PercentOfTheCapAnd30PercentOfTheRest()
    {
        var result = Compute(100_000m, 300_000m);

        (result.WithheldWithinLimits, result.WithheldBeyondLimits).ShouldBe((11_583.00m, 6_834.00m));
    }

    // Without pay, 30% of the royalty alone stays within.
    [Fact]
    public void Compute_royaltyWithoutPay_keeps30PercentOfItWithin()
    {
        var result = Compute(10_000m, 0m);

        (result.RelativeLimit, result.WithinLimits, result.TaxedAsPay).ShouldBe((3_000.00m, 3_000.00m, 7_000.00m));
    }

    // Art. 37 al. 2 keeps the royalty movable only while the average holds: Netto taxes the whole
    // royalty as pay when it does not (decided unattended, the reading that never overstates the gain),
    // and withholds 30% on all of it.
    [Fact]
    public void Compute_averageOfThePreviousYearsAboveTheCap_taxesTheWholeRoyaltyAsPay()
    {
        var result = Compute(10_000m, 50_000m, averageWithinCap: false);

        (result.WithinLimits, result.TaxedAsPay, result.Bound, result.TaxedApart).ShouldBe((0m, 10_000m, CopyrightBound.Average, null));
    }

    [Fact]
    public void Compute_averageOfThePreviousYearsAboveTheCap_withholds30PercentOnAll()
    {
        var result = Compute(10_000m, 50_000m, averageWithinCap: false);

        (result.WithheldWithinLimits, result.WithheldBeyondLimits).ShouldBe((0m, 3_000.00m));
    }

    [Fact]
    public void Compute_noRoyalty_movesNothing()
    {
        var result = Compute(0m, 50_000m);

        (result.Royalty, result.WithinLimits, result.TaxedAsPay, result.Withheld, result.TaxedApart).ShouldBe((0m, 0m, 0m, 0m, null));
    }

    [Fact]
    public void Compute_negativeRoyalty_isRefused()
    {
        CopyrightCalculator.Compute(Rules2026, new CopyrightRoyalty(-1m), 50_000m).ShouldBe((null, CopyrightCalculator.RoyaltyNegative));
    }

    // A pay the engine refuses later counts as none for the limit, so the limit is never below zero.
    [Fact]
    public void Compute_negativePay_countsAsNoPay()
    {
        var result = Compute(10_000m, -5_000m);

        result.RelativeLimit.ShouldBe(3_000.00m);
    }

    [Fact]
    public void Compute_2025RoyaltyAboveTheCap_keeps2025sCapWithin()
    {
        var result = CopyrightCalculator.Compute(CopyrightRules.IncomeYear2025, new CopyrightRoyalty(100_000m), 300_000m).Value.ShouldNotBeNull();

        (result.WithinLimits, result.TaxedAsPay).ShouldBe((75_360m, 24_640m));
    }

    // AR/CIR 92 art. 3 al. 2 and art. 4 (revenus 2026): with an attestation du travail des arts, the part
    // within the limits is taxed on less its flat-rate costs, 50% of the first 20,590 and 25% of the part
    // from 20,590 to 41,180 (the indexation notice for tax year 2027). 30,000 within beside 80,000 of pay:
    // 10,295 + 2,352.50 = 12,647.50.
    [Fact]
    public void Compute_attestationHolder_takesTheFlatRateCostsOffThePartWithinTheLimits()
    {
        var result = Compute(30_000m, 80_000m, attestation: true);

        (result.WithinLimits, result.FlatRateCosts, result.TaxedApart?.Income).ShouldBe((30_000m, 12_647.50m, 17_352.50m));
    }

    // The 15% withheld within the limits is on that net (programme law of 30.05.2026, art. 12 al. 2).
    [Fact]
    public void Compute_attestationHolder_withholds15PercentOnTheNet()
    {
        Compute(30_000m, 80_000m, attestation: true).WithheldWithinLimits.ShouldBe(2_602.88m);
    }

    [Fact]
    public void Compute_attestationHolderWithinTheFirstBand_takesHalfOff()
    {
        var result = Compute(10_000m, 50_000m, attestation: true);

        (result.FlatRateCosts, result.TaxedApart?.Income, result.WithheldWithinLimits).ShouldBe((5_000m, 5_000m, 750m));
    }

    // Beyond 41,180 within the limits, the costs stay at their most: 10,295 + 5,147.50.
    [Fact]
    public void Compute_attestationHolderAboveTheSecondBand_takesAtMost15442()
    {
        Compute(60_000m, 200_000m, attestation: true).FlatRateCosts.ShouldBe(15_442.50m);
    }

    // The costs are movable income's, never the part taxed as pay's: 24,000 within beside 50,000 of pay,
    // costs 10,295 + 852.50, and the 6,000 beyond as it was.
    [Fact]
    public void Compute_attestationHolderBeyondTheLimits_takesTheCostsOffThePartWithinAlone()
    {
        var result = Compute(30_000m, 50_000m, attestation: true);

        (result.WithinLimits, result.FlatRateCosts, result.TaxedAsPay, result.WithheldBeyondLimits).ShouldBe((24_000m, 11_147.50m, 6_000m, 1_800m));
    }

    // The limits are on the gross royalty, the costs coming after them: the cap keeps 77,220 within.
    [Fact]
    public void Compute_attestationHolderAboveTheCap_keepsTheCapOnTheGross()
    {
        var result = Compute(100_000m, 300_000m, attestation: true);

        (result.WithinLimits, result.FlatRateCosts, result.TaxedApart?.Income).ShouldBe((77_220m, 15_442.50m, 61_777.50m));
    }

    // From 2026, without an attestation, no flat-rate costs.
    [Fact]
    public void Compute_2026WithoutAttestation_takesNoFlatRateCosts()
    {
        var result = Compute(30_000m, 80_000m);

        (result.FlatRateCosts, result.TaxedApart?.Income).ShouldBe((0m, 30_000m));
    }

    [Fact]
    public void Compute_attestationHolderWhoseAverageIsAboveTheCap_hasNothingToTakeCostsOff()
    {
        Compute(10_000m, 50_000m, averageWithinCap: false, attestation: true).FlatRateCosts.ShouldBe(0m);
    }

    // In 2025 every author took them, 50% of the first 20,100 and 25% up to 40,190 (the indexation
    // notice for tax year 2026): Tax-Calc T19's 12,525 on 30,000.
    [Fact]
    public void Compute_2025WithoutAttestation_takesTheFlatRateCostsAll2025AuthorsHad()
    {
        var result = CopyrightCalculator.Compute(CopyrightRules.IncomeYear2025, new CopyrightRoyalty(30_000m), 80_000m).Value.ShouldNotBeNull();

        (result.FlatRateCosts, result.TaxedApart?.Income, result.WithheldWithinLimits).ShouldBe((12_525m, 17_475m, 2_621.25m));
    }

    // The year reads the royalty with its costs as plain figures: the company pays and the pocket gets
    // the whole royalty, the tax and the withholding are on its net.
    [Fact]
    public void MovableIncome_attestationHolder_isPaidWholeAndTaxedOnTheNet()
    {
        var result = Compute(30_000m, 80_000m, attestation: true);

        (result.MovableIncome, result.MovableIncome.PaidApart)
            .ShouldBe((new MovableIncomePaid(30_000m, new Core.PersonalTax.IncomeTaxedApart(17_352.50m, Rules2026.TaxedApartRate), 0m, 2_602.88m), 30_000m));
    }

    // The year reads the royalty as plain figures: what the company pays, the part taxed apart, the part
    // taxed as pay and what it withheld.
    [Fact]
    public void MovableIncome_royaltyBeyondTheLimits_handsTheYearItsPlainFigures()
    {
        var result = Compute(20_000m, 30_000m);

        result.MovableIncome.ShouldBe(new MovableIncomePaid(20_000m, new Core.PersonalTax.IncomeTaxedApart(15_000.00m, Rules2026.TaxedApartRate), 5_000.00m, 3_750.00m));
    }
}
