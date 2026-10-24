using Netto.Core;
using Shouldly;

namespace Netto.Tests;

// The engine's arithmetic of money: a difference that moves nothing is a plain zero, never a negative one
// (decimal subtraction can return one, which a step or a drain reads as money coming back), and an amount
// taken to the cent below or above, as a cap or a payment that has to cover something is.
public class MoneyTests
{
    public static TheoryData<decimal, decimal> EqualAmounts => new()
    {
        { 0m, 0.00m },
        { 0.00m, 0m },
        { 0m, 0m },
        { 1_200.5m, 1_200.50m },
        { -3.10m, -3.1m },
    };

    public static TheoryData<decimal, decimal, decimal> DifferentAmounts => new()
    {
        { 5m, 3.25m, 1.75m },
        { 3m, 5m, -2m },
        { 0m, 0.01m, -0.01m },
    };

    public static TheoryData<decimal, decimal> DownToCents => new()
    {
        { 1.23m, 1.23m },
        { 1.234m, 1.23m },
        { 1.239m, 1.23m },
        { 1.2399999m, 1.23m },
        { -1.231m, -1.24m },
        { 0.004m, 0m },
    };

    public static TheoryData<decimal, decimal> UpToCents => new()
    {
        { 1.23m, 1.23m },
        { 1.231m, 1.24m },
        { 1.2300001m, 1.24m },
        { -1.239m, -1.23m },
        { 0.001m, 0.01m },
    };

    // The credit rates of the advance payments' four deadlines.
    public static TheoryData<decimal> CreditRates => new() { 0.09m, 0.075m, 0.06m, 0.045m };

    [Theory]
    [MemberData(nameof(EqualAmounts))]
    public void Minus_equalAmounts_isAPlainZero(decimal amount, decimal same)
    {
        var difference = Money.Minus(amount, same);

        decimal.IsNegative(difference).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(DifferentAmounts))]
    public void Minus_differentAmounts_isTheirDifference(decimal amount, decimal less, decimal expected)
    {
        var difference = Money.Minus(amount, less);

        difference.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(DownToCents))]
    public void DownToCent_amount_isTheCentAtOrBelowIt(decimal amount, decimal expected)
    {
        var cents = Money.DownToCent(amount);

        cents.ShouldBe(expected);
    }

    [Fact]
    public void DownToCent_negativeZero_isAPlainZero()
    {
        var cents = Money.DownToCent(0m - 0.00m);

        decimal.IsNegative(cents).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(UpToCents))]
    public void UpToCent_amount_isTheCentAtOrAboveIt(decimal amount, decimal expected)
    {
        var cents = Money.UpToCent(amount);

        cents.ShouldBe(expected);
    }

    // Math.Ceiling(-0.4m) is a negative zero.
    [Fact]
    public void UpToCent_tinyNegativeAmount_isAPlainZero()
    {
        var cents = Money.UpToCent(-0.004m);

        (cents, decimal.IsNegative(cents)).ShouldBe((0m, false));
    }

    // Every amount left to cover from a cent to 100.00, and a few larger ones: the payment's credit, rounded
    // half away from zero as the administration rounds it, covers it, and a cent less does not.
    [Theory]
    [MemberData(nameof(CreditRates))]
    public void SmallestCovering_anyAmountLeft_isTheSmallestPaymentInCentsWhoseRoundedCreditCoversIt(decimal rate)
    {
        IEnumerable<decimal> lefts = [.. Enumerable.Range(1, 10_000).Select(cents => cents / 100m), 1_234.56m, 9_999.99m, 45_678.91m];

        var wrong = lefts
            .Select(left => (Left: left, Payment: Money.SmallestCovering(left, rate)))
            .Where(covering => Money.RoundToCent(covering.Payment * rate) < covering.Left
                || Money.RoundToCent((covering.Payment - 0.01m) * rate) >= covering.Left)
            .ToList();

        wrong.ShouldBeEmpty();
    }
}
