using Netto.Core.Rules;
using Netto.Web.Client;
using Shouldly;

namespace Netto.Tests.WebClient;

public class FormatsTests
{
    private static readonly Source AnySource = new("Title", "Reference", new Uri("https://example.org"));

    [Fact]
    public void Euros_amount_isEnglishStyleToTheCent()
    {
        var text = Formats.Euros(50_000m);

        text.ShouldBe("€50,000.00");
    }

    [Fact]
    public void Euros_negativeAmount_carriesALeadingMinus()
    {
        var text = Formats.Euros(-3_561.68m);

        text.ShouldBe("-€3,561.68");
    }

    [Fact]
    public void WholeEuros_amount_hasNoCents()
    {
        var text = Formats.WholeEuros(50_000m);

        text.ShouldBe("€50,000");
    }

    [Fact]
    public void Deduction_amount_isPrefixedWithAMinusSign()
    {
        var text = Formats.Deduction(8_293.36m);

        text.ShouldBe("−€8,293.36");
    }

    [Fact]
    public void Share_fraction_isAPercentageWithOneDecimal()
    {
        var text = Formats.Share(0.16588m);

        text.ShouldBe("16.6%");
    }

    // Rates are fractions in the rule sets (20.50% is 0.205), and their keys end in _rate or _factor.
    [Theory]
    [InlineData("first_band_rate", "0.205", "20.5%")]
    [InlineData("second_band_rate", "0.1416", "14.16%")]
    [InlineData("autonomy_factor", "0.24957", "24.957%")]
    [InlineData("municipal_rate", "0.07", "7%")]
    [InlineData("minimum_income", "17374.08", "€17,374.08")]
    [InlineData("bracket_1_upper_limit", "16720", "€16,720.00")]
    [InlineData("flat_rate_costs_maximum", "3200", "€3,200.00")]
    public void RuleAmount_byKey_isARateOrEuros(string key, string value, string expected)
    {
        var amount = new RuleAmount(key, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), AnySource);

        var text = Formats.RuleAmount(amount);

        text.ShouldBe(expected);
    }
}
