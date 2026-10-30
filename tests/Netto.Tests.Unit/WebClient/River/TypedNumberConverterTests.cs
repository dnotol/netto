using System.Globalization;
using MudBlazor;
using MudBlazor.Utilities.Exceptions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The converter every river number field reads and shows its text through (RiverNumericField): what is typed is
// read strictly (TypedNumber), then by MudBlazor's own converter in the invariant culture; a value is shown in
// the field's culture and format. A text that is no number is refused with MudBlazor's own key, which the field
// words as its error.
public class TypedNumberConverterTests
{
    // Why the converter exists: MudBlazor's own reads a dot anywhere as a thousands separator in French.
    [Fact]
    public void MudBlazorsOwnConverter_frenchDecimalPoint_readsTenTimesTheAmount()
    {
        using var french = InLanguage.French();
        var culture = Formats.Culture;

        new DefaultConverter<decimal> { Culture = () => culture }.ConvertBack("1.5").ShouldBe(15m);
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("12.50")]
    [InlineData("1234.56")]
    [InlineData("0.500")]
    [InlineData("0.125")]
    [InlineData("00.123")]
    [InlineData("012.345")]
    public void ConvertBack_frenchDecimalPoint_isRefusedAsNoNumber(string typed)
    {
        using var french = InLanguage.French();

        Should.Throw<ConversionException>(() => Converter<decimal>().ConvertBack(typed)).ErrorMessageKey.ShouldBe("Converter_InvalidNumber");
    }

    [Theory]
    [InlineData("1.234,56", "1234.56")]
    [InlineData("1234,56", "1234.56")]
    [InlineData("-1,5", "-1.5")]
    [InlineData("0,5", "0.5")]
    [InlineData("0", "0")]
    [InlineData("007", "7")]
    public void ConvertBack_frenchNumber_isItsValue(string typed, string expected)
    {
        using var french = InLanguage.French();

        Converter<decimal>().ConvertBack(typed).ShouldBe(decimal.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("1,5")]
    [InlineData("0,500")]
    [InlineData("012,345")]
    public void ConvertBack_englishCommaThatGroupsNoThousands_isRefused(string typed)
    {
        Should.Throw<ConversionException>(() => Converter<decimal>().ConvertBack(typed));
    }

    [Fact]
    public void ConvertBack_englishNumber_isItsValue()
    {
        Converter<decimal>().ConvertBack("1,234.56").ShouldBe(1_234.56m);
    }

    // A whole number, such as the meal-voucher days: thousands are grouped, a fraction is no number.
    [Fact]
    public void ConvertBack_wholeNumberInFrench_isItsValue()
    {
        using var french = InLanguage.French();

        Converter<int>().ConvertBack("1.234").ShouldBe(1_234);
    }

    [Fact]
    public void ConvertBack_fractionForAWholeNumber_isRefused()
    {
        using var french = InLanguage.French();

        Should.Throw<ConversionException>(() => Converter<int>().ConvertBack("1,5"));
    }

    // An emptied field reads as MudBlazor reads it: zero.
    [Fact]
    public void ConvertBack_empty_isZero()
    {
        Converter<decimal>().ConvertBack("").ShouldBe(0m);
    }

    [Fact]
    public void Convert_valueInFrench_isShownInTheFieldsCultureAndFormat()
    {
        using var french = InLanguage.French();

        Converter<decimal>("N2").Convert(1_234.5m).ShouldBe("1.234,50");
    }

    [Fact]
    public void Convert_valueInEnglish_isShownInTheFieldsCultureAndFormat()
    {
        Converter<decimal>("N2").Convert(1_234.5m).ShouldBe("1,234.50");
    }

    // The culture and format a field hands its converter, read at each conversion, as MudBlazor's fields do.
    private static TypedNumberConverter<T> Converter<T>(string? format = null)
    {
        var culture = Formats.Culture;
        return new TypedNumberConverter<T> { Culture = () => culture, Format = () => format };
    }
}
