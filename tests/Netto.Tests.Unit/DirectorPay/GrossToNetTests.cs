using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// Income year 2026 at a 7% municipal rate. Expected values are derived from the official amounts
// (INASTI « Cotisations 2026 », the assessment year 2027 indexation notice), not checked against
// Tax-Calc, which does not cover 2026 yet; the method is proven on 2025 in PersonalTaxCalculatorTests.
public class GrossToNetTests
{
    public static TheoryData<decimal> GrossPays => [0m, 10_000m, 30_000m, 50_000m, 160_000m];

    public static TheoryData<decimal> GrossPaysInFirstBand => [30_000m, 30_000.01m, 50_000m, 70_000m];

    public static TheoryData<decimal> MunicipalRatesOutOfRange => [-0.0001m, 0.1001m];

    public static TheoryData<decimal> MunicipalRatesAtTheLimits => [0m, 0.10m];

    [Fact]
    public void Compute_2026GrossZero_paysTheMinimumContributionsAndNoTax()
    {
        var result = Compute2026(0m);

        (result.SocialContributions, result.FlatRateCosts, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge, result.NetInPocket)
            .ShouldBe((3_561.68m, 0m, 0m, 0m, 0m, -3_561.68m));
    }

    [Fact]
    public void Compute_2026BaseUnderMinimumIncome_paysTheMinimumAndTheNextThousandAddsNoContributions()
    {
        var result = Compute2026(10_000m);

        (result.SocialContributions, result.NextThousand.SocialContributions).ShouldBe((3_561.68m, 0m));
    }

    [Fact]
    public void Compute_2026BaseAboveSecondBandCeiling_paysTheMaximumAndTheNextThousandAddsNoContributions()
    {
        var result = Compute2026(160_000m);

        (result.SocialContributions, result.NextThousand.SocialContributions).ShouldBe((20_412.20m, 0m));
    }

    // The item's worked example, to the cent: contributions 19.885/119.885 of the gross before
    // quarterly rounding, flat-rate costs 3% × 41,706.64, tax on the 45% bracket less 2,887.50.
    [Fact]
    public void Compute_2026Gross50000_matchesTheWorkedExample()
    {
        var result = Compute2026(50_000m);

        (result.SocialContributions, result.FlatRateCosts, result.TaxableIncome, result.IncomeTax, result.MunicipalSurcharge, result.NetInPocket)
            .ShouldBe((8_293.36m, 1_251.20m, 40_455.44m, 11_333.96m, 793.38m, 29_579.30m));
    }

    [Fact]
    public void Compute_2026Gross50000_splitsTheNextThousandAsTheWorkedExample()
    {
        var result = Compute2026(50_000m);

        result.NextThousand.ShouldBe(new NextThousand(
            SocialContributions: 165.88m, IncomeTax: 364.09m, MunicipalSurcharge: 25.48m, NetInPocket: 444.55m));
    }

    [Fact]
    public void Compute_2026HighGross_capsFlatRateCostsAt3200()
    {
        var result = Compute2026(160_000m);

        result.FlatRateCosts.ShouldBe(3_200m);
    }

    [Theory]
    [MemberData(nameof(GrossPaysInFirstBand))]
    public void Compute_2026InFirstBand_taxableIncomeIsGrossLessContributionsAndFlatRateCosts(decimal grossPay)
    {
        var result = Compute2026(grossPay);

        result.TaxableIncome.ShouldBe(grossPay - result.SocialContributions - result.FlatRateCosts);
    }

    // Contributions are four rounded quarters, so they move in 0.04 steps, and at some pay levels no
    // amount equals the contributions on its own base to the cent: at 30,000 the engine returns
    // 4,976.00 and the base it leaves gives 4,976.04 (decided 2026-10-19, see the Notion item).
    [Theory]
    [MemberData(nameof(GrossPaysInFirstBand))]
    public void Compute_2026InFirstBand_contributionsRecomputedFromTheirBaseAreWithinFourCents(decimal grossPay)
    {
        var result = Compute2026(grossPay);

        var recomputed = SocialContributionCalculator.Annual(SocialContributionRules.IncomeYear2026, result.TaxableIncome);

        Math.Abs(recomputed - result.SocialContributions).ShouldBeLessThanOrEqualTo(0.04m);
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026_netInPocketIsGrossLessWhatIsPaidOut(decimal grossPay)
    {
        var result = Compute2026(grossPay);

        result.NetInPocket.ShouldBe(grossPay - result.SocialContributions - result.IncomeTax - result.MunicipalSurcharge);
    }

    [Theory]
    [MemberData(nameof(GrossPays))]
    public void Compute_2026_nextThousandPartsAddUpToOneThousand(decimal grossPay)
    {
        var next = Compute2026(grossPay).NextThousand;

        (next.SocialContributions + next.IncomeTax + next.MunicipalSurcharge + next.NetInPocket).ShouldBe(1_000m);
    }

    [Fact]
    public void Compute_2026_returnsEveryStepWithItsTrack()
    {
        var result = Compute2026(50_000m);

        result.Steps.Select(step => (step.RuleKey, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("social_contributions", Track.PaidOut, 8_293.36m),
            ("flat_rate_costs", Track.TaxBaseOnly, 1_251.20m),
            ("taxable_income", Track.TaxBaseOnly, 40_455.44m),
            ("income_tax", Track.PaidOut, 11_333.96m),
            ("municipal_surcharge", Track.PaidOut, 793.38m),
        });
    }

    [Fact]
    public void Compute_2026_everyStepHasASourceWithAReferenceAndAnAddress()
    {
        var result = Compute2026(50_000m);

        result.Steps.ShouldAllBe(step => step.Source.Reference.Length > 0 && step.Source.Url.IsAbsoluteUri);
    }

    [Fact]
    public void Compute_2026_everyStepListsTheAmountsItUsed()
    {
        var result = Compute2026(50_000m);

        result.Steps.Select(step => (step.RuleKey, string.Join(' ', step.AmountsUsed.Select(amount => amount.Key)))).ShouldBe(new[]
        {
            ("social_contributions", "minimum_income first_band_rate first_band_ceiling second_band_rate second_band_ceiling"),
            ("flat_rate_costs", "flat_rate_costs_rate flat_rate_costs_maximum"),
            ("taxable_income", ""),
            ("income_tax", "bracket_1_rate bracket_1_upper_limit bracket_2_rate bracket_2_upper_limit bracket_3_rate "
                + "bracket_3_upper_limit bracket_4_rate tax_free_amount tax_free_amount_rate autonomy_factor regional_additional_rate"),
            ("municipal_surcharge", "entered_municipal_rate"),
        });
    }

    // The surcharge step cites the articles that set it up as its rule, and the rate under its own
    // source: the SPW file for a municipality's rate.
    [Fact]
    public void Compute_2026MunicipalityRate_surchargeStepUsesItWithItsSource()
    {
        var namur = MunicipalRates.IncomeYear2026.Municipalities.Single(municipality => municipality.Name == "Namur");

        var surcharge = Compute2026(50_000m, namur.Rate).Steps.Single(step => step.RuleKey == "municipal_surcharge");

        (surcharge.Source, surcharge.AmountsUsed.Single()).ShouldBe((PersonalTaxRules.MunicipalSurcharge, namur.Rate));
    }

    [Fact]
    public void Compute_2026EnteredRate_surchargeStepUsesItWithTheArticlesAsSource()
    {
        var entered = MunicipalRates.Entered(0.07m);

        var surcharge = Compute2026(50_000m, entered).Steps.Single(step => step.RuleKey == "municipal_surcharge");

        surcharge.AmountsUsed.Single().ShouldBe(new RuleAmount("entered_municipal_rate", 0.07m, PersonalTaxRules.MunicipalSurcharge));
    }

    // Namur's 8.5%: where the rate comes from changes its source, never the figures.
    [Fact]
    public void Compute_2026MunicipalityRate_givesTheFiguresOfTheSameRateEntered()
    {
        var namur = MunicipalRates.IncomeYear2026.Municipalities.Single(municipality => municipality.Name == "Namur");

        var official = Compute2026(50_000m, namur.Rate);
        var entered = Compute2026(50_000m, MunicipalRates.Entered(0.085m));

        (official.MunicipalRate, official.MunicipalSurcharge, official.NetInPocket)
            .ShouldBe((0.085m, entered.MunicipalSurcharge, entered.NetInPocket));
    }

    [Fact]
    public void Compute_2026_incomeTaxStepUsesThe2026TaxFreeAmount()
    {
        var incomeTax = Compute2026(50_000m).Steps.Single(step => step.RuleKey == "income_tax");

        incomeTax.AmountsUsed.Single(amount => amount.Key == "tax_free_amount").Value.ShouldBe(11_550m);
    }

    [Fact]
    public void Compute_negativeGrossPay_isRefused()
    {
        var (value, error) = GrossToNet.Compute(2026, -0.01m, MunicipalRates.Entered(0.07m));

        (value, error).ShouldBe((null, GrossToNet.GrossPayNegative));
    }

    [Theory]
    [MemberData(nameof(MunicipalRatesOutOfRange))]
    public void Compute_municipalRateOutsideZeroToTenPercent_isRefused(decimal municipalRate)
    {
        var (value, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(municipalRate));

        (value, error).ShouldBe((null, GrossToNet.MunicipalRateOutOfRange));
    }

    [Theory]
    [MemberData(nameof(MunicipalRatesAtTheLimits))]
    public void Compute_municipalRateAtZeroOrTenPercent_isAccepted(decimal municipalRate)
    {
        var (_, error) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(municipalRate));

        error.ShouldBeNull();
    }

    // 2025 has personal-tax amounts for the Tax-Calc tests but no INASTI amounts, and 2027's
    // amounts are not published.
    [Theory]
    [InlineData(2025)]
    [InlineData(2027)]
    public void Compute_incomeYearOtherThan2026_isRefused(int incomeYear)
    {
        var (value, error) = GrossToNet.Compute(incomeYear, 50_000m, MunicipalRates.Entered(0.07m));

        (value, error).ShouldBe((null, GrossToNet.IncomeYearNotSupported));
    }

    private static GrossToNetResult Compute2026(decimal grossPay) => Compute2026(grossPay, MunicipalRates.Entered(0.07m));

    private static GrossToNetResult Compute2026(decimal grossPay, RuleAmount municipalRate)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, municipalRate);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
