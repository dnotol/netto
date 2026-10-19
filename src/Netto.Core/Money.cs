namespace Netto.Core;

internal static class Money
{
    // Half away from zero, the rounding the administration and INASTI publish their amounts with.
    // decimal's default (half to even) would round 0.125 down to 0.12.
    public static decimal RoundToCent(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
