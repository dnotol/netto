using System.Globalization;
using Netto.Core.Rules;

namespace Netto.Web.Client;

// How amounts and dates read, in the language the page runs in (Languages). In English, whatever the browser's
// language: €50,000.00, 1 January 2027. In French, like the administration's: 1.234,56 €, 20,5 %, 1er janvier
// 2027, the unit after the number with a no-break space (U+00A0). Both cultures are built by hand on the
// invariant one rather than taken by name (en-IE, fr-BE): the client is published with InvariantGlobalization,
// so no ICU data is downloaded and a named culture has none of its data, while a hand-built one needs none, so
// the browser and the tests format alike. MudBlazor inputs take Culture through their Culture parameter (a
// RiverNumericField sets it itself, and reads what is typed strictly, TypedNumber). The page's own culture of
// numbers is never set: only its UI culture, which picks the culture here.
internal static class Formats
{
    // U+00A0, between a number and its unit, so a line never breaks between them.
    private const string NoBreak = "\u00A0";

    private static readonly CultureInfo English = EnglishWithEuro();

    private static readonly CultureInfo French = FrenchWithEuro();

    // The culture of the language the page runs in.
    public static CultureInfo Culture => InFrench ? French : English;

    private static bool InFrench => Languages.Current == Language.French;

    public static string Euros(decimal amount) => amount.ToString("C2", Culture);

    public static string WholeEuros(decimal amount) => amount.ToString("C0", Culture);

    // A chart's axis, short enough to sit beside the plot on a phone: €25k, €2.5k; in French 25 k€, 2,5 k€.
    public static string ThousandsOfEuros(decimal amount)
    {
        var culture = Culture;
        var sign = amount < 0m ? culture.NumberFormat.NegativeSign : "";
        var thousands = (Math.Abs(amount) / 1_000m).ToString("#,0.#", culture);
        if (InFrench)
            return amount == 0m ? $"0{NoBreak}€" : $"{sign}{thousands}{NoBreak}k€";

        return amount == 0m ? $"{culture.NumberFormat.CurrencySymbol}0" : $"{sign}{culture.NumberFormat.CurrencySymbol}{thousands}k";
    }

    // Prefixed even at zero, so every part paid out reads as a subtraction.
    public static string Deduction(decimal amount) => $"{Culture.NumberFormat.NegativeSign}{Euros(amount)}";

    // Money that comes back, such as a refund.
    public static string Addition(decimal amount) => $"{Culture.NumberFormat.PositiveSign}{Euros(amount)}";

    public static string Share(decimal fraction) => fraction.ToString("0.0%", Culture);

    // A share at a glance, in the bar of buckets and a card's summary: 62%.
    public static string WholeShare(decimal fraction) => fraction.ToString("0%", Culture);

    // One decimal, as municipalities set their rates (CIR 92 art. 468): 8% reads 8.0%.
    public static string MunicipalRate(decimal fraction) => fraction.ToString("0.0%", Culture);

    // 1 January 2027; in French 1er janvier 2027, the first of a month being « 1er ».
    public static string Day(DateOnly date) => $"{DayOfMonth(date)} {date.ToString("MMMM yyyy", Culture)}";

    // A day short enough to sit after a tab's name: 21 Dec; in French 21 déc., 1er avr.
    public static string ShortDay(DateOnly date) => $"{DayOfMonth(date)} {date.ToString("MMM", Culture)}";

    // A month of the income year, 1 being January, by its name: January; in French janvier.
    public static string Month(int year, int month) => new DateOnly(year, month, 1).ToString("MMMM", Culture);

    // A month on the timeline's axis, short enough to sit over a month of a narrow screen: Oct; in French oct.
    public static string ShortMonth(int year, int month) => new DateOnly(year, month, 1).ToString("MMM", Culture);

    // The rule sets hold rates as fractions (20.50% is 0.205), under keys ending in _rate or _factor,
    // multiples of a rate under keys ending in _multiplier: 2.25 reads 2.25×, coefficients, bare
    // numbers published to four decimals, under keys ending in _coefficient, and emissions in grams of
    // CO2 per kilometre under keys ending in _grams: 70 reads 70 g/km.
    public static string RuleAmount(RuleAmount amount) =>
        amount.Key.EndsWith("_rate", StringComparison.Ordinal) || amount.Key.EndsWith("_factor", StringComparison.Ordinal)
            ? amount.Value.ToString("0.###%", Culture)
            : amount.Key.EndsWith("_multiplier", StringComparison.Ordinal)
                ? $"{amount.Value.ToString("0.##", Culture)}×"
                : amount.Key.EndsWith("_coefficient", StringComparison.Ordinal)
                    ? amount.Value.ToString("0.####", Culture)
                    : amount.Key.EndsWith("_grams", StringComparison.Ordinal)
                        ? Grams(amount.Value)
                        : Euros(amount.Value);

    // A multiplier the law writes as a fraction, 2/3 or 5/3, read as one (RuleAmount would round it to
    // 0.67× or 1.67×): the fraction of the smallest denominator up to 12 that the value is, to four
    // decimals; any other amount reads as RuleAmount reads it.
    public static string Fraction(RuleAmount amount)
    {
        for (var denominator = 2; denominator <= 12 && amount.Value != Math.Truncate(amount.Value); denominator++)
        {
            var numerator = Math.Round(amount.Value * denominator);
            if (Math.Abs(numerator / denominator - amount.Value) < 0.0001m)
                return $"{numerator.ToString("0", Culture)}/{denominator.ToString(Culture)}";
        }

        return RuleAmount(amount);
    }

    // A percentage with as many decimals as it has, up to three: a car's CO2 percentage of 9.75%, a
    // deduction rate of 61.6%.
    public static string Percentage(decimal fraction) => fraction.ToString("0.###%", Culture);

    // An emission in grams of CO2 per kilometre: 120 g/km, a fake hybrid's 112.5 g/km; in French 112,5 g/km, after a
    // no-break space.
    public static string Grams(decimal grams) => $"{grams.ToString("0.#", Culture)}{(InFrench ? NoBreak : " ")}g/km";

    // Percentage points between two shares, to a tenth: 2.5, as the Plans panel's comparison says them.
    public static string Points(decimal points) => points.ToString("0.0", Culture);

    private static string DayOfMonth(DateOnly date) =>
        InFrench && date.Day == 1 ? "1er" : date.Day.ToString(CultureInfo.InvariantCulture);

    private static CultureInfo EnglishWithEuro()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.CurrencySymbol = "€";
        culture.NumberFormat.CurrencyPositivePattern = 0; // €n
        culture.NumberFormat.CurrencyNegativePattern = 1; // -€n

        // U+2212, the minus sign, which is as wide as a digit where a hyphen is not. Parsing still
        // accepts a typed hyphen.
        culture.NumberFormat.NegativeSign = char.ConvertFromUtf32(0x2212);
        return CultureInfo.ReadOnly(culture);
    }

    // Like the administration's: a dot between thousands, a comma before the decimals, the euro and the percent
    // after the number, a no-break space before them: 1.234,56 €, −1.234,56 €, 20,5 %. The space is part of the
    // symbol, since a format's pattern would put an ordinary one there, where a line can break. Months in lower
    // case, the genitive ones too, which a day before the month reads.
    private static CultureInfo FrenchWithEuro()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        var numbers = culture.NumberFormat;
        numbers.NumberDecimalSeparator = ",";
        numbers.NumberGroupSeparator = ".";
        numbers.CurrencyDecimalSeparator = ",";
        numbers.CurrencyGroupSeparator = ".";
        numbers.PercentDecimalSeparator = ",";
        numbers.PercentGroupSeparator = ".";
        numbers.CurrencySymbol = NoBreak + "€";
        numbers.CurrencyPositivePattern = 1; // n€, the symbol carrying its space
        numbers.CurrencyNegativePattern = 5; // -n€
        numbers.PercentSymbol = NoBreak + "%";
        numbers.PercentPositivePattern = 1; // n%
        numbers.PercentNegativePattern = 1; // -n%
        numbers.NegativeSign = char.ConvertFromUtf32(0x2212);

        string[] months = ["janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre", ""];
        string[] shortMonths = ["janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc.", ""];
        var dates = culture.DateTimeFormat;
        dates.MonthNames = months;
        dates.MonthGenitiveNames = months;
        dates.AbbreviatedMonthNames = shortMonths;
        dates.AbbreviatedMonthGenitiveNames = shortMonths;
        return CultureInfo.ReadOnly(culture);
    }
}
