using System.Globalization;
using System.Text.RegularExpressions;
using MudBlazor;
using MudBlazor.Utilities.Exceptions;

namespace Netto.Web.Client.River;

// What a viewer types in a number field, read strictly. MudBlazor reads a number with NumberStyles.Any, which
// takes a thousands separator anywhere: in French, where it is a dot, « 1.5 » would read 15 and « 12.50 » 1250.
// So a thousands separator is taken only between groups of exactly three digits, after one to three, and the
// decimal separator, once, ends the whole part; a minus, typed (-) or as Formats writes it (U+2212), may lead.
// « 1.234,56 », « 1234,56 », « -1,5 » are numbers in French, « 1.5 » and « 1234.56 » are not; in English the
// same rule holds with the comma and the dot swapped.
internal static class TypedNumber
{
    // The text in the invariant form MudBlazor reads without doubt (-1234.56), or null when it is no number in
    // the culture's own form.
    public static string? Invariant(string text, NumberFormatInfo numbers)
    {
        var group = Regex.Escape(numbers.NumberGroupSeparator);
        var point = Regex.Escape(numbers.NumberDecimalSeparator);
        var minus = Regex.Escape(numbers.NegativeSign);
        var match = Regex.Match(text.Trim(),
            $@"^(?<minus>-|{minus})?(?<whole>\d{{1,3}}(?:{group}\d{{3}})+|\d+)?(?:{point}(?<fraction>\d*))?$",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        if (!match.Success)
            return null;

        var whole = match.Groups["whole"].Value.Replace(numbers.NumberGroupSeparator, "", StringComparison.Ordinal);
        var fraction = match.Groups["fraction"].Value;
        if (whole.Length == 0 && fraction.Length == 0)
            return null;

        var sign = match.Groups["minus"].Success ? "-" : "";
        return fraction.Length == 0 ? $"{sign}{(whole.Length == 0 ? "0" : whole)}" : $"{sign}{(whole.Length == 0 ? "0" : whole)}.{fraction}";
    }
}

// How every river number field reads and shows its text (RiverNumericField): what is typed is read strictly
// (TypedNumber), then by MudBlazor's own converter in the invariant culture, and a text that is no number is
// refused with MudBlazor's key, which the field words as its error (NettoMudLocalizer); a value is shown by
// MudBlazor's own converter in the field's culture and format, which the field hands it (ICultureAwareConverter).
internal sealed class TypedNumberConverter<T> : IReversibleConverter<T?, string?>, ICultureAwareConverter
{
    // MudBlazor's own key for a text that is no number (its LanguageResource, internal), which its localizer words.
    internal const string InvalidNumber = "Converter_InvalidNumber";

    private readonly DefaultConverter<T> _shown;
    private readonly DefaultConverter<T> _read = new() { Culture = () => CultureInfo.InvariantCulture, Format = () => null };

    public TypedNumberConverter() => _shown = new DefaultConverter<T> { Culture = () => Culture(), Format = () => Format() };

    public Func<CultureInfo> Culture { get; set; } = () => Formats.Culture;

    public Func<string?> Format { get; set; } = () => null;

    public string? Convert(T? input) => _shown.Convert(input);

    // An emptied field reads as MudBlazor reads it.
    public T? ConvertBack(string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? _read.ConvertBack(input)
            : TypedNumber.Invariant(input, Culture().NumberFormat) is { } invariant
                ? _read.ConvertBack(invariant)
                : throw new ConversionException(InvalidNumber);
}
