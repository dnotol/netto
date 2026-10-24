using System.Globalization;
using Netto.Core.Rules;

namespace Netto.Web.Client;

// How amounts read on an English page, whatever the browser's language: €50,000.00. The culture is
// the invariant one with the euro sign rather than a named one such as en-IE: the client is published
// with InvariantGlobalization, so no ICU data is downloaded and a culture created by name throws,
// while the invariant one needs none, so the browser and the tests format alike. MudBlazor inputs
// take it through their Culture parameter (a RiverNumericField sets it itself).
internal static class Formats
{
    public static CultureInfo Culture { get; } = EnglishWithEuro();

    public static string Euros(decimal amount) => amount.ToString("C2", Culture);

    public static string WholeEuros(decimal amount) => amount.ToString("C0", Culture);

    // A chart's axis, short enough to sit beside the plot on a phone: €25k, €2.5k.
    public static string ThousandsOfEuros(decimal amount)
    {
        var sign = amount < 0m ? Culture.NumberFormat.NegativeSign : "";
        var thousands = (Math.Abs(amount) / 1_000m).ToString("#,0.#", Culture);
        return amount == 0m ? $"{Culture.NumberFormat.CurrencySymbol}0" : $"{sign}{Culture.NumberFormat.CurrencySymbol}{thousands}k";
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

    // The invariant culture's month names are English: 1 January 2027.
    public static string Day(DateOnly date) => date.ToString("d MMMM yyyy", Culture);

    // A day short enough to sit after a tab's name: 21 Dec.
    public static string ShortDay(DateOnly date) => date.ToString("d MMM", Culture);

    // A month of the income year, 1 being January, by its English name.
    public static string Month(int year, int month) => new DateOnly(year, month, 1).ToString("MMMM", Culture);

    // A month on the timeline's axis, short enough to sit over a month of a narrow screen: Oct.
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

    // A percentage with as many decimals as it has, up to three: a car's CO2 percentage of 9.75%, a
    // deduction rate of 61.6%.
    public static string Percentage(decimal fraction) => fraction.ToString("0.###%", Culture);

    // An emission in grams of CO2 per kilometre: 120 g/km, a fake hybrid's 112.5 g/km.
    public static string Grams(decimal grams) => $"{grams.ToString("0.#", Culture)} g/km";

    // Percentage points between two shares, to a tenth: 2.5, as the Compare plans tab says them.
    public static string Points(decimal points) => points.ToString("0.0", Culture);

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
}
