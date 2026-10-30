namespace Netto.Core;

internal static class Money
{
    // Half away from zero, the rounding the administration and INASTI publish their amounts with.
    // decimal's default (half to even) would round 0.125 down to 0.12.
    public static decimal RoundToCent(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    // The amount less the other, a plain zero when they are equal: decimal subtraction can return a
    // negative zero (0m - 0.00m), which a step or a drain reads as money coming back.
    public static decimal Minus(decimal amount, decimal less) => amount == less ? 0m : amount - less;

    // The cent at or below the amount, as a cap that must never exceed the law's is taken.
    public static decimal DownToCent(decimal amount) => PlainZero(Math.Floor(amount * 100m) / 100m);

    // The cent at or above the amount, as a payment that must reach a figure is taken. Math.Ceiling(-0.4m)
    // is a negative zero, returned as a plain one.
    public static decimal UpToCent(decimal amount) => PlainZero(Math.Ceiling(amount * 100m) / 100m);

    // The smallest payment in cents whose credit at the rate, rounded half away from zero, covers the
    // amount left, itself in cents: Tax-Calc's « VA nécessaires pour éviter la majoration ». A credit of
    // left less half a cent already rounds up to it.
    public static decimal SmallestCovering(decimal left, decimal rate) => UpToCent((left - 0.005m) / rate);

    // The amount as is, a plain zero where it is a negative one: a chain of subtractions and additions can end on
    // 0 - 0.00, which a step or a drain reads as money coming back.
    public static decimal PlainZero(decimal amount) => amount == 0m ? 0m : amount;
}
