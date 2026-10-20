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

    // Rates are fractions in the rule sets (20.50% is 0.205), and their keys end in _rate or _factor;
    // a multiple of a rate ends in _multiplier.
    [Theory]
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
