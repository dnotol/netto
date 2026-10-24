using Netto.Core.DirectorPay;
using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// Income year 2026's withholding rules cite the texts filed in official-docs/personal-tax/: the key
// formula ESS-SR/2025-0928 for the method, annex III to the AR/CIR 92 as the royal decree of
// 11.12.2025 replaced it (« Règles 1er janvier 2026 ») for each amount, and the coordinated annex III
// for the dates: the one those rules hold from and the one of the decree of 27.09.2026, whose rules
// Netto does not model.
public class WithholdingRulesTests
{
    private const string Fisconetplus = "https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/";
    private const string KeyFormula = "389035ca-432e-422b-859d-2d32fbdaa4cf";
    private const string DecreedAnnex = "536a741e-81d6-4605-ad11-8e07ebc4a440";
    private const string CoordinatedAnnex = "0c174ec5-448b-4d1e-983b-fc457e5e7e28";

    private static readonly WithholdingRules Rules2026 = WithholdingRules.IncomeYear2026;

    [Fact]
    public void For_2026_isTheIncomeYear2026Rules()
    {
        WithholdingRules.For(2026).ShouldBeSameAs(Rules2026);
    }

    // 2025 has no withholding rules filed, and 2027's are not published.
    [Theory]
    [InlineData(2025)]
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public void For_yearWithoutWithholdingRules_isNull(int incomeYear)
    {
        WithholdingRules.For(incomeYear).ShouldBeNull();
    }

    [Fact]
    public void RulesFrom_2026_is1January2026()
    {
        Rules2026.RulesFrom.Value.ShouldBe(new DateOnly(2026, 1, 1));
    }

    // The royal decree of 27.09.2026 (MB 02.10.2026) changes n° 32 for pay from 1 November 2026.
    [Fact]
    public void UnmodelledRulesFrom_2026_is1November2026()
    {
        Rules2026.UnmodelledRulesFrom.Value.ShouldBe(new DateOnly(2026, 11, 1));
    }

    // N° 25.2: 385 up to 1,465; 385 + 21.50% of the slice over 1,465 up to 6,310; 1,426.68 + 14.50%
    // of the slice over 6,310 up to 9,290; 1,858.78 above.
    [Fact]
    public void Reduction_2026_isTheTableOfNumber25Point2()
    {
        Rules2026.Reduction.Select(band => (band.Base?.Value, band.Rate?.Value, band.UpperLimit?.Value)).ShouldBe(
        [
            (385m, null, 1_465m),
            (385m, 0.215m, 6_310m),
            (1_426.68m, 0.145m, 9_290m),
            (1_858.78m, null, null),
        ]);
    }

    [Fact]
    public void FlatRateCosts_2026_are3PercentUpTo3200()
    {
        (Rules2026.FlatRateCostsRate.Value, Rules2026.FlatRateCostsMaximum.Value).ShouldBe((0.03m, 3_200m));
    }

    // N° 31, with the 7% municipal surcharge built in: 26.75% up to 16,710; 4,469.93 + 42.80% of the
    // slice over 16,710 up to 29,500; 9,944.05 + 48.15% of the slice over 29,500 up to 51,050;
    // 20,320.38 + 53.50% of the slice over 51,050.
    [Fact]
    public void Scale_2026_isTheScaleOfNumber31()
    {
        Rules2026.Scale.Select(band => (band.Base?.Value, band.Rate?.Value, band.UpperLimit?.Value)).ShouldBe(
        [
            (null, 0.2675m, 16_710m),
            (4_469.93m, 0.428m, 29_500m),
            (9_944.05m, 0.4815m, 51_050m),
            (20_320.38m, 0.535m, null),
        ]);
    }

    // N° 32: the tax on the tax-free amount of 11,170 for a single person.
    [Fact]
    public void TaxFreeAmountReduction_2026_is2987Point98()
    {
        Rules2026.TaxFreeAmountReduction.Value.ShouldBe(2_987.98m);
    }

    // The 385 the first two bands of the reduction share is listed once.
    [Fact]
    public void Amounts_2026_listEachFigureOfTheAnnexOnce()
    {
        string.Join(' ', Rules2026.Amounts.Select(amount => amount.Key)).ShouldBe(
            "reduction_minimum reduction_band_1_upper_limit reduction_band_2_rate reduction_band_2_upper_limit "
            + "reduction_band_3_base reduction_band_3_rate reduction_band_3_upper_limit reduction_maximum "
            + "flat_rate_costs_rate flat_rate_costs_maximum "
            + "withholding_bracket_1_rate withholding_bracket_1_upper_limit "
            + "withholding_bracket_2_base withholding_bracket_2_rate withholding_bracket_2_upper_limit "
            + "withholding_bracket_3_base withholding_bracket_3_rate withholding_bracket_3_upper_limit "
            + "withholding_bracket_4_base withholding_bracket_4_rate "
            + "tax_free_amount_reduction");
    }

    [Fact]
    public void Amounts_2026_haveUniqueKeys()
    {
        Rules2026.Amounts.Select(amount => amount.Key).ShouldBeUnique();
    }

    // The web formats an amount whose key ends in _rate as a percentage.
    [Fact]
    public void Rates_2026_haveKeysEndingInRate()
    {
        var rates = Rules2026.Reduction.Concat(Rules2026.Scale).Select(band => band.Rate).OfType<RuleAmount>().Append(Rules2026.FlatRateCostsRate);

        rates.ShouldAllBe(rate => rate.Key.EndsWith("_rate", StringComparison.Ordinal));
    }

    [Fact]
    public void Source_2026_isTheKeyFormula()
    {
        Rules2026.Source.Url.ShouldBe(new Uri(Fisconetplus + KeyFormula));
    }

    [Fact]
    public void Amounts_2026_citeTheAnnexAsDecreedOn11December2025()
    {
        Rules2026.Amounts.ShouldAllBe(amount => amount.Source.Url == new Uri(Fisconetplus + DecreedAnnex));
    }

    // The coordinated annex's header dates each decree's rules, the 11.12.2025 and 27.09.2026 ones
    // among them.
    [Fact]
    public void Dates_2026_citeTheCoordinatedAnnex()
    {
        new[] { Rules2026.RulesFrom, Rules2026.UnmodelledRulesFrom }.ShouldAllBe(date => date.Source.Url == new Uri(Fisconetplus + CoordinatedAnnex));
    }
}
