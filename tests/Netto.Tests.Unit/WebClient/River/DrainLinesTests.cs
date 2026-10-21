using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A drain list shows only the lines that move money; a line of nothing stays a step of the ledger and
// of the card's sources, but leaves the list.
public class DrainLinesTests
{
    // Each reads €0.00: zero, the negative zero the engine gives for a credit of nothing, and less than
    // half a cent.
    public static TheoryData<decimal> Nothing => new() { 0m, decimal.Negate(0.00m), 0.004m, -0.004m };

    public static TheoryData<decimal> Cents => new() { 0.01m, 0.005m, -0.01m };

    [Fact]
    public void Nothing_negativeZero_isANegativeZero()
    {
        decimal.IsNegative(decimal.Negate(0.00m)).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(Nothing))]
    public void Shown_lineOfNothing_isLeftOut(decimal amount)
    {
        var drains = new[] { new Drain("income_tax_advance_payment_surcharge", DrainKind.PaidOut, amount, "taxes_on_pay") };

        DrainLines.Shown(drains).ShouldBeEmpty();
    }

    // What is kept in a loss year is negative, and moves money all the same.
    [Theory]
    [MemberData(nameof(Cents))]
    public void Shown_lineOfACentOrMore_isKept(decimal amount)
    {
        var drain = new Drain("kept_in_company", DrainKind.PassedOn, amount, "kept_in_company");

        DrainLines.Shown([drain]).ShouldBe([drain]);
    }

    // On the example, nothing comes back as the credit for low earned income, and the withholding
    // leaves neither a surcharge nor a bonus on the tax (ModuleViewsTests).
    [Fact]
    public void Shown_directorPayOfTheExample_keepsWhatMovesInItsOrder()
    {
        var view = DirectorPayView.From(RiverInputs.Example, RiverInputs.Example.Run().Pay.ShouldNotBeNull());

        DrainLines.Shown(view.Drains).Select(drain => drain.Key)
            .ShouldBe(["social_contributions", "income_tax", "municipal_surcharge", "net_pay"]);
    }
}
