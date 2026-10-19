using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// The SPW file of 03.09.2026 (official-docs/municipal-surcharge), sheet « Taux IPP »: its latest
// column is tax year 2026, that is income year 2025, and income year 2026 uses it until the SPW adds
// tax year 2027. The spot rates are read by hand from that column. Gembloux, Spa and Butgenbach
// changed their rate between tax years 2025 and 2026, so they also prove which column was taken.
public class MunicipalRatesTests
{
    public static TheoryData<int, string, decimal> SpotRates => new()
    {
        { 25005, "Beauvechain", 0.070m },
        { 62063, "Liège", 0.080m },
        { 63013, "Butgenbach", 0.070m },
        { 63072, "Spa", 0.070m },
        { 82003, "Bastogne", 0.070m },
        { 92094, "Namur", 0.085m },
        { 92142, "Gembloux", 0.076m },
    };

    [Fact]
    public void For_2026_usesTheRatesPublishedForIncomeYear2025()
    {
        var rates = Rates2026();

        (rates.IncomeYear, rates.RatesIncomeYear).ShouldBe((2026, 2025));
    }

    // CIR 92 art. 469: the surcharge goes to the municipality the taxpayer lives in on 1 January of
    // the tax year, and income year 2026 is tax year 2027.
    [Fact]
    public void For_2026_residenceDateIsTheFirstOfJanuary2027()
    {
        var rates = Rates2026();

        rates.ResidenceDate.ShouldBe(new DateOnly(2027, 1, 1));
    }

    // 2025 has no use yet (GrossToNet covers 2026 only), and 2027's rates are not published.
    [Theory]
    [InlineData(2025)]
    [InlineData(2027)]
    public void For_incomeYearOtherThan2026_isNull(int incomeYear)
    {
        var rates = MunicipalRates.For(incomeYear);

        rates.ShouldBeNull();
    }

    // 262 rows in the file, less Bertogne, merged with Bastogne on 1.12.2025.
    [Fact]
    public void For_2026_lists261Municipalities()
    {
        var rates = Rates2026();

        rates.Municipalities.Count.ShouldBe(261);
    }

    [Fact]
    public void For_2026_leavesOutBertogneMergedWithBastogne()
    {
        var rates = Rates2026();

        rates.Municipalities.ShouldNotContain(municipality => municipality.InsCode == 82005);
    }

    [Theory]
    [MemberData(nameof(SpotRates))]
    public void For_2026_municipalityHasItsTaxYear2026Rate(int insCode, string name, decimal rate)
    {
        var municipality = Rates2026().Municipalities.Single(municipality => municipality.InsCode == insCode);

        (municipality.Name, municipality.Rate.Value).ShouldBe((name, rate));
    }

    // CIR 92 art. 468: a percentage with at most one decimal. The engine refuses a rate above 10%.
    [Fact]
    public void For_2026_everyRateIsBetweenZeroAndTenPercentWithAtMostOneDecimal()
    {
        var rates = Rates2026();

        rates.Municipalities.Where(municipality =>
                municipality.Rate.Value is < 0m or > 0.10m
                || municipality.Rate.Value * 1_000m != decimal.Truncate(municipality.Rate.Value * 1_000m))
            .ShouldBeEmpty();
    }

    [Fact]
    public void For_2026_codesAndNamesAreUnique()
    {
        var municipalities = Rates2026().Municipalities;

        (municipalities.DistinctBy(municipality => municipality.InsCode).Count(),
                municipalities.DistinctBy(municipality => municipality.Name).Count())
            .ShouldBe((municipalities.Count, municipalities.Count));
    }

    [Fact]
    public void For_2026_everyRateCitesTheSpwFile()
    {
        var municipalities = Rates2026().Municipalities;

        municipalities.Select(municipality => (municipality.Rate.Key, municipality.Rate.Source.Url.Host)).Distinct()
            .ShouldBe([("municipal_rate", "interieur.wallonie.be")]);
    }

    // A rate the user types has no published source of its own: it cites the articles that set up
    // the surcharge, under a key that says it was entered.
    [Fact]
    public void Entered_rate_citesTheMunicipalSurchargeArticles()
    {
        var rate = MunicipalRates.Entered(0.073m);

        rate.ShouldBe(new RuleAmount("entered_municipal_rate", 0.073m, PersonalTaxRules.MunicipalSurcharge));
    }

    private static MunicipalRates Rates2026() => MunicipalRates.For(2026).ShouldNotBeNull();
}
