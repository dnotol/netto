using System.Globalization;
using Netto.Core.Rules;
using Netto.Web.Client;
using Shouldly;

namespace Netto.Tests.WebClient;

// In French, amounts read like the administration's (glossary, « How French is written »): a dot between
// thousands, a comma before the decimals, and the unit after the number with a no-break space (U+00A0):
// « 1.234,56 € », « 20,5 % ». The minus is U+2212, as in English. Never a narrow no-break space (U+202F), which
// some fonts draw as nothing and a typed text never holds. Dates: « 1er janvier 2027 », months in lower case.
// Every code point is pinned:   and − in each expected text. English reads as FormatsTests hold it.
public class FormatsInFrenchTests
{
    private const string Space = " ";
    private const string Minus = "−";

    private static readonly Source AnySource = new("any_passage", new Uri("https://example.org"), "layer/issuer_title_2026-01-01.pdf");

    [Theory]
    [InlineData("1234.56", "1.234,56" + Space + "€")]
    [InlineData("-1234.56", Minus + "1.234,56" + Space + "€")]
    [InlineData("50000", "50.000,00" + Space + "€")]
    [InlineData("1234567.8", "1.234.567,80" + Space + "€")]
    [InlineData("0.5", "0,50" + Space + "€")]
    [InlineData("0", "0,00" + Space + "€")]
    public void Euros_inFrench_readsLikeTheAdministration(string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.Euros(Number(value)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("51000", "51.000" + Space + "€")]
    [InlineData("999", "999" + Space + "€")]
    public void WholeEuros_inFrench_hasNoCents(string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.WholeEuros(Number(value)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("0", "0" + Space + "€")]
    [InlineData("25000", "25" + Space + "k€")]
    [InlineData("2500", "2,5" + Space + "k€")]
    [InlineData("1000000", "1.000" + Space + "k€")]
    [InlineData("-5000", Minus + "5" + Space + "k€")]
    public void ThousandsOfEuros_inFrench_isCountedInKiloEuros(string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.ThousandsOfEuros(Number(value)).ShouldBe(expected);
    }

    [Fact]
    public void Deduction_inFrench_isPrefixedWithAMinusSign()
    {
        using var french = InLanguage.French();

        Formats.Deduction(8_293.36m).ShouldBe(Minus + "8.293,36" + Space + "€");
    }

    [Fact]
    public void Addition_inFrench_isPrefixedWithAPlusSign()
    {
        using var french = InLanguage.French();

        Formats.Addition(149.94m).ShouldBe("+149,94" + Space + "€");
    }

    [Theory]
    [InlineData("0.16588", "16,6" + Space + "%")]
    [InlineData("0.205", "20,5" + Space + "%")]
    public void Share_inFrench_hasOneDecimalAfterAComma(string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.Share(Number(value)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("0.615", "62" + Space + "%")]
    [InlineData("1", "100" + Space + "%")]
    public void WholeShare_inFrench_isAWholePercentage(string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.WholeShare(Number(value)).ShouldBe(expected);
    }

    [Fact]
    public void MunicipalRate_inFrench_hasOneDecimal()
    {
        using var french = InLanguage.French();

        Formats.MunicipalRate(0.08m).ShouldBe("8,0" + Space + "%");
    }

    [Theory]
    [InlineData("0.0975", "9,75" + Space + "%")]
    [InlineData("0.18", "18" + Space + "%")]
    public void Percentage_inFrench_readsWithTheDecimalsItHas(string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.Percentage(Number(value)).ShouldBe(expected);
    }

    // The first of a month is « 1er », as French writes it.
    [Theory]
    [InlineData(2027, 1, 1, "1er janvier 2027")]
    [InlineData(2026, 10, 30, "30 octobre 2026")]
    [InlineData(2026, 4, 10, "10 avril 2026")]
    [InlineData(2026, 8, 31, "31 août 2026")]
    public void Day_inFrench_isTheDayTheMonthInLowerCaseAndTheYear(int year, int month, int day, string expected)
    {
        using var french = InLanguage.French();

        Formats.Day(new DateOnly(year, month, day)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(2026, 12, 21, "21 déc.")]
    [InlineData(2026, 4, 1, "1er avr.")]
    [InlineData(2026, 5, 3, "3 mai")]
    public void ShortDay_inFrench_isTheDayAndTheMonthShort(int year, int month, int day, string expected)
    {
        using var french = InLanguage.French();

        Formats.ShortDay(new DateOnly(year, month, day)).ShouldBe(expected);
    }

    [Fact]
    public void Month_inFrench_isEachMonthsNameInLowerCase()
    {
        using var french = InLanguage.French();

        Enumerable.Range(1, 12).Select(month => Formats.Month(2026, month)).ShouldBe(
            ["janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre"]);
    }

    [Fact]
    public void ShortMonth_inFrench_isEachMonthsShortName()
    {
        using var french = InLanguage.French();

        Enumerable.Range(1, 12).Select(month => Formats.ShortMonth(2026, month)).ShouldBe(
            ["janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc."]);
    }

    [Theory]
    [InlineData("120", "120" + Space + "g/km")]
    [InlineData("112.5", "112,5" + Space + "g/km")]
    public void Grams_inFrench_readsInGramsPerKilometre(string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.Grams(Number(value)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("2.5", "2,5")]
    [InlineData("0", "0,0")]
    [InlineData("-2.5", Minus + "2,5")]
    public void Points_inFrench_readsToATenthAfterAComma(string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.Points(Number(value)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("first_band_rate", "0.205", "20,5" + Space + "%")]
    [InlineData("second_band_rate", "0.1416", "14,16" + Space + "%")]
    [InlineData("cadastral_income_indexation_coefficient", "2.2446", "2,2446")]
    [InlineData("minimum_income", "17374.08", "17.374,08" + Space + "€")]
    [InlineData("surcharge_multiplier", "2.25", "2,25×")]
    [InlineData("first_credit_multiplier", "3", "3×")]
    [InlineData("petrol_reference_grams", "70", "70" + Space + "g/km")]
    public void RuleAmount_inFrench_isARateOrEurosTheFrenchWay(string key, string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.RuleAmount(new RuleAmount(key, Number(value), AnySource)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("rent_cap_multiplier", "1.6666666666666666666666666667", "5/3")]
    [InlineData("odd_multiplier", "2.37", "2,37×")]
    public void Fraction_inFrench_readsAsAFractionOrAMultiplier(string key, string value, string expected)
    {
        using var french = InLanguage.French();

        Formats.Fraction(new RuleAmount(key, Number(value), AnySource)).ShouldBe(expected);
    }

    // Nothing Formats writes in French holds a narrow no-break space, nor an ordinary one between a number and
    // its unit, where a line could break.
    [Fact]
    public void Formats_inFrench_writeNoNarrowNorBreakingSpace()
    {
        using var french = InLanguage.French();

        string[] texts =
        [
            Formats.Euros(-1_234_567.89m), Formats.WholeEuros(51_000m), Formats.ThousandsOfEuros(25_000m), Formats.Share(0.205m),
            Formats.WholeShare(0.62m), Formats.MunicipalRate(0.08m), Formats.Percentage(0.0975m), Formats.Grams(112.5m),
        ];

        texts.Where(text => text.Contains(' ', StringComparison.Ordinal) || text.Contains(' ', StringComparison.Ordinal)).ShouldBeEmpty();
    }

    // The culture MudBlazor's fields take: French while the page is in French, the English one otherwise.
    [Fact]
    public void Culture_inFrench_hasACommaBeforeDecimalsAndADotBetweenThousands()
    {
        using var french = InLanguage.French();

        (Formats.Culture.NumberFormat.NumberDecimalSeparator, Formats.Culture.NumberFormat.NumberGroupSeparator).ShouldBe((",", "."));
    }

    [Fact]
    public void Culture_inEnglish_isTheEnglishOne()
    {
        (Formats.Culture.NumberFormat.NumberDecimalSeparator, Formats.Culture.NumberFormat.NumberGroupSeparator).ShouldBe((".", ","));
    }

    private static decimal Number(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
