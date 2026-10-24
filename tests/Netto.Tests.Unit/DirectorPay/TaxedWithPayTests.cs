using Netto.Core.Copyright;
using Netto.Core.DirectorPay;
using Netto.Core.MovableIncome;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// What the levers hand the pay to be taxed with it, added up lever by lever as the year folds them: a
// lever that hands nothing leaves the other's figures as they were, to the bit.
public class TaxedWithPayTests
{
    private static readonly MovableIncomePaid Royalty = new(10_000m, new(10_000m, CopyrightRules.IncomeYear2026.TaxedApartRate), 0m, 1_500m);

    [Fact]
    public void Plus_nothingAndALeversShare_isThatShare()
    {
        var rent = new TaxedWithPay { OtherIncome = 828.00m };

        (TaxedWithPay.None.Plus(rent), rent.Plus(TaxedWithPay.None)).ShouldBe((rent, rent));
    }

    // A negative zero is nothing: it hands the other lever's share on as it is.
    [Fact]
    public void Plus_aNegativeZero_handsTheOtherShareOnAsItIs()
    {
        var car = new TaxedWithPay { BenefitInKind = 1_714.29m };

        car.Plus(new TaxedWithPay { OtherIncome = decimal.Negate(0m) }).ShouldBeSameAs(car);
    }

    [Fact]
    public void Plus_twoLevers_addsTheirOtherIncomesAndTheirBenefits()
    {
        var first = new TaxedWithPay { OtherIncome = 828.00m, BenefitInKind = 100m };
        var second = new TaxedWithPay { OtherIncome = 100m, BenefitInKind = 1_714.29m };

        var both = first.Plus(second);

        (both.OtherIncome, both.BenefitInKind, both.Movable).ShouldBe((928.00m, 1_814.29m, MovableIncomePaid.None));
    }

    [Fact]
    public void Plus_aMovableIncomeAndABenefit_keepsTheMovableIncome()
    {
        var both = new TaxedWithPay { Movable = Royalty }.Plus(new TaxedWithPay { BenefitInKind = 1_714.29m });

        (both.BenefitInKind, both.Movable).ShouldBe((1_714.29m, Royalty));
    }

    // The pay takes one movable income, with one part taxed apart at its rate: two are a bug until it
    // takes more.
    [Fact]
    public void Plus_twoMovableIncomes_isABug()
    {
        var royalty = new TaxedWithPay { Movable = Royalty };

        Should.Throw<InvalidOperationException>(() => royalty.Plus(royalty));
    }
}
