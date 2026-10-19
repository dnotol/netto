using System.Globalization;
using Netto.Core.Rules;

namespace Netto.Web.Client;

// How amounts read on an English page: €50,000.00. Program.cs makes this the app's culture, so the
// MudBlazor fields parse and print numbers the same way whatever the browser's language.
internal static class Formats
{
    public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("en-IE");

    public static string Euros(decimal amount) => amount.ToString("C2", Culture);

    public static string WholeEuros(decimal amount) => amount.ToString("C0", Culture);

    // U+2212, the minus sign, which lines up with the digits where a hyphen would not.
    public static string Deduction(decimal amount) => $"−{Euros(amount)}";

    public static string Share(decimal fraction) => fraction.ToString("0.0%", Culture);

    // The rule sets hold rates as fractions (20.50% is 0.205), under keys ending in _rate or _factor.
    public static string RuleAmount(RuleAmount amount) =>
        amount.Key.EndsWith("_rate", StringComparison.Ordinal) || amount.Key.EndsWith("_factor", StringComparison.Ordinal)
            ? amount.Value.ToString("0.###%", Culture)
            : Euros(amount.Value);
}
