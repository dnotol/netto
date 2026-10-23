using Netto.Core.Rules;
using Netto.Web.Client;
using Shouldly;

namespace Netto.Tests.WebClient;

public class FormatsTests
{
    private static readonly Source AnySource = new("Title", "Reference", new Uri("https://example.org"), "layer/issuer_title_2026-01-01.pdf");

    [Fact]
    public void Euros_amount_isEnglishStyleToTheCent()
    {
        var text = Formats.Euros(50_000m);

        text.ShouldBe("€50,000.00");
    }

    [Fact]
    public void Euros_negativeAmount_carriesALeadingMinusSign()
    {
        var text = Formats.Euros(-3_561.68m);

        text.ShouldBe("−€3,561.68");
    }

    // The MudBlazor rate field parses with this culture, and a user types a hyphen, not U+2212.
    [Fact]
    public void Culture_parsingATypedHyphen_readsANegativeNumber()
    {
        var value = decimal.Parse("-1.5", Formats.Culture);

        value.ShouldBe(-1.5m);
    }

    [Fact]
    public void WholeEuros_amount_hasNoCents()
    {
        var text = Formats.WholeEuros(50_000m);

        text.ShouldBe("€50,000");
    }

    // A chart's axis: short enough to sit beside the plot on a phone.
    [Theory]
    [InlineData("0", "€0")]
    [InlineData("25000", "€25k")]
    [InlineData("2500", "€2.5k")]
    [InlineData("1000000", "€1,000k")]
    [InlineData("-5000", "−€5k")]
    public void ThousandsOfEuros_amount_isCountedInThousands(string value, string expected)
    {
        var text = Formats.ThousandsOfEuros(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

        text.ShouldBe(expected);
    }

    // The statement names its months in English, whatever the browser's language.
    [Theory]
    [InlineData(1, "January")]
    [InlineData(11, "November")]
    [InlineData(12, "December")]
    public void Month_ofTheIncomeYear_isItsEnglishName(int month, string expected)
    {
        var text = Formats.Month(2026, month);

        text.ShouldBe(expected);
    }

    [Fact]
    public void Deduction_amount_isPrefixedWithAMinusSign()
    {
        var text = Formats.Deduction(8_293.36m);

        text.ShouldBe("−€8,293.36");
    }

    [Fact]
    public void Addition_amount_isPrefixedWithAPlusSign()
    {
        var text = Formats.Addition(149.94m);

        text.ShouldBe("+€149.94");
    }

    [Fact]
    public void Share_fraction_isAPercentageWithOneDecimal()
    {
        var text = Formats.Share(0.16588m);

        text.ShouldBe("16.6%");
    }

    // A share at a glance, in the bar of buckets and a card's summary: a whole percent, rounded half
    // away from zero like every amount Netto shows.
    [Theory]
    [InlineData("0.6197574", "62%")]
    [InlineData("0.615", "62%")]
    [InlineData("0.0049", "0%")]
    [InlineData("1", "100%")]
    public void WholeShare_fraction_isAWholePercentage(string value, string expected)
    {
        var text = Formats.WholeShare(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

        text.ShouldBe(expected);
    }

    // Municipalities set their rate with one decimal, so 8% reads 8.0%, like the rate field.
    [Theory]
    [InlineData("0.08", "8.0%")]
    [InlineData("0.085", "8.5%")]
    public void MunicipalRate_fraction_isAPercentageWithOneDecimal(string value, string expected)
    {
        var text = Formats.MunicipalRate(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

        text.ShouldBe(expected);
    }

    [Fact]
    public void Day_date_isTheDayTheMonthNameAndTheYear()
    {
        var text = Formats.Day(new DateOnly(2027, 1, 1));

        text.ShouldBe("1 January 2027");
    }

    // A tab's badge, short enough to sit after its name: the day and the month's first three letters.
    [Theory]
    [InlineData(2026, 12, 21, "21 Dec")]
    [InlineData(2026, 4, 1, "1 Apr")]
    public void ShortDay_date_isTheDayAndTheMonthShort(int year, int month, int day, string expected)
    {
        Formats.ShortDay(new DateOnly(year, month, day)).ShouldBe(expected);
    }

    // A month of the timeline's axis, short enough to sit over a month of a narrow screen.
    [Theory]
    [InlineData(1, "Jan")]
    [InlineData(9, "Sep")]
    [InlineData(10, "Oct")]
    [InlineData(12, "Dec")]
    public void ShortMonth_monthOfTheIncomeYear_isItsFirstThreeLetters(int month, string expected)
    {
        Formats.ShortMonth(2026, month).ShouldBe(expected);
    }

    // A car's CO2 percentage or deduction rate, as exact as the rules make it: 9.75% is not 9.8%.
    [Theory]
    [InlineData("0.105", "10.5%")]
    [InlineData("0.0975", "9.75%")]
    [InlineData("0.18", "18%")]
    [InlineData("0.616", "61.6%")]
    public void Percentage_fraction_readsWithTheDecimalsItHas(string fraction, string expected)
    {
        Formats.Percentage(decimal.Parse(fraction, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("120", "120 g/km")]
    [InlineData("112.5", "112.5 g/km")]
    public void Grams_emission_readsInGramsPerKilometre(string grams, string expected)
    {
        Formats.Grams(decimal.Parse(grams, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(expected);
    }

    // Rates are fractions in the rule sets (20.50% is 0.205), and their keys end in _rate or _factor;
    // a multiple of a rate ends in _multiplier; a coefficient, a bare number to four decimals, in
    // _coefficient: art. 518's 2.2446 must not read 2.24; an emission in grams of CO2 per km, in _grams.
    [Theory]
    [InlineData("petrol_reference_grams", "70", "70 g/km")]
    [InlineData("high_emission_grams", "200", "200 g/km")]
    [InlineData("cadastral_income_indexation_coefficient", "2.2446", "2.2446")]
    [InlineData("cadastral_income_indexation_coefficient", "2.3000", "2.3")]
    [InlineData("cadastral_income_revaluation_coefficient", "5.75", "5.75")]
    [InlineData("first_band_rate", "0.205", "20.5%")]
    [InlineData("second_band_rate", "0.1416", "14.16%")]
    [InlineData("autonomy_factor", "0.24957", "24.957%")]
    [InlineData("municipal_rate", "0.07", "7%")]
    [InlineData("entered_municipal_rate", "0.073", "7.3%")]
    [InlineData("minimum_income", "17374.08", "€17,374.08")]
    [InlineData("bracket_1_upper_limit", "16720", "€16,720.00")]
    [InlineData("flat_rate_costs_maximum", "3200", "€3,200.00")]
    [InlineData("surcharge_multiplier", "2.25", "2.25×")]
    [InlineData("first_credit_multiplier", "3", "3×")]
    public void RuleAmount_byKey_isARateOrEuros(string key, string value, string expected)
    {
        var amount = new RuleAmount(key, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), AnySource);

        var text = Formats.RuleAmount(amount);

        text.ShouldBe(expected);
    }
}
