using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What a viewer types in a number field, read strictly before MudBlazor parses it. MudBlazor reads a number
// with NumberStyles.Any, which takes a thousands separator anywhere: in French, where it is a dot, « 1.5 » would
// read 15 and « 12.50 » 1250, an amount ten or a hundred times what was meant. So a thousands separator is
// taken only between groups of exactly three digits, after one to three, and the decimal separator alone ends
// the whole part: anything else is refused, as MudBlazor refuses what is no number. The text is handed on in
// the invariant form MudBlazor reads without doubt.
public class TypedNumberTests
{
    [Theory]
    [InlineData("1.234,56", "1234.56")]
    [InlineData("1234,56", "1234.56")]
    [InlineData("-1,5", "-1.5")]
    [InlineData("−1,5", "-1.5")]
    [InlineData("1.234.567,89", "1234567.89")]
    [InlineData("1.234", "1234")]
    [InlineData("51.000", "51000")]
    [InlineData("0,5", "0.5")]
    [InlineData(",5", "0.5")]
    [InlineData("12,", "12")]
    [InlineData("8,0", "8.0")]
    [InlineData("220", "220")]
    [InlineData("0", "0")]
    [InlineData("007", "007")]
    [InlineData("0,125", "0.125")]
    [InlineData(" 1.234,56 ", "1234.56")]
    public void Invariant_frenchNumber_isReadTheFrenchWay(string typed, string expected)
    {
        using var french = InLanguage.French();

        TypedNumber.Invariant(typed, Formats.Culture.NumberFormat).ShouldBe(expected);
    }

    // A dot is a thousands separator in French, so a typed decimal point is no number: never 15, 1250 or 123456.
    // A grouped number starts with a digit other than 0, so « 0.500 » is never 500 nor « 012.345 » 12345.
    [Theory]
    [InlineData("1.5")]
    [InlineData("12.50")]
    [InlineData("1234.56")]
    [InlineData("1.23")]
    [InlineData("1.2345")]
    [InlineData("1234.567")]
    [InlineData("0.500")]
    [InlineData("0.125")]
    [InlineData("00.123")]
    [InlineData("012.345")]
    [InlineData("1.234.")]
    [InlineData("1..234")]
    [InlineData(".5")]
    [InlineData("1.234,5.6")]
    [InlineData("1,2,3")]
    [InlineData("--1")]
    [InlineData("1-")]
    [InlineData("abc")]
    [InlineData("-")]
    [InlineData(",")]
    public void Invariant_frenchTextThatIsNoFrenchNumber_isRefused(string typed)
    {
        using var french = InLanguage.French();

        TypedNumber.Invariant(typed, Formats.Culture.NumberFormat).ShouldBeNull();
    }

    [Theory]
    [InlineData("1,234.56", "1234.56")]
    [InlineData("1234.56", "1234.56")]
    [InlineData("-1.5", "-1.5")]
    [InlineData("−3,561.68", "-3561.68")]
    [InlineData("50,000.00", "50000.00")]
    [InlineData("8.0", "8.0")]
    [InlineData("0.5", "0.5")]
    [InlineData("0", "0")]
    [InlineData("007", "007")]
    public void Invariant_englishNumber_isReadTheEnglishWay(string typed, string expected)
    {
        TypedNumber.Invariant(typed, Formats.Culture.NumberFormat).ShouldBe(expected);
    }

    // The same rule in English, where the comma is the thousands separator: « 1,5 » is never 15.
    [Theory]
    [InlineData("1,5")]
    [InlineData("12,50")]
    [InlineData("1234,56")]
    [InlineData("1,234,5")]
    [InlineData("0,500")]
    [InlineData("0,125")]
    [InlineData("00,123")]
    [InlineData("012,345")]
    public void Invariant_englishTextThatIsNoEnglishNumber_isRefused(string typed)
    {
        TypedNumber.Invariant(typed, Formats.Culture.NumberFormat).ShouldBeNull();
    }
}
