using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// A director taxed alone with dependants, resident in Wallonia at a 7% municipal rate, the pay in cadre XVI
// with its flat-rate costs and no own contributions unless given. The supplements raise the tax-free amount,
// whose tax climbs art. 134 §2's scale (25% to 11,750, 30% to 16,720, 40% to 27,860 for income 2026). The
// part of that tax the basic tax cannot absorb, insofar as it is the child supplements, counted after the
// base amount and the other supplements and with the base and the other supplements beyond the taxable
// income left out, becomes the refundable credit for children, at most 550 per child (§3). §2's scale is
// above art. 130's from 11,750 on (30% against 25%), so a credit arises even with an income at or above the
// tax-free amount. Tax-Calc does not cover income year 2026, so every expected value is derived by hand from
// the official amounts (indexation notice for tax year 2027, art. 130, 131 and 134); the 2025 cases are
// derived the same way from the notice for tax year 2026, for a Tax-Calc « exercice 2026 » check.
public class PersonalTaxCalculatorDependantsTests
{
    private static readonly PersonalTaxRules Rules2026 = PersonalTaxRules.IncomeYear2026;
    private static readonly PersonalTaxRules Rules2025 = PersonalTaxRules.IncomeYear2025;

    // A single parent with two children, one under 3: 5,130 + 740 + 1,980 = 7,850 of supplements.
    private static readonly Household SingleParentOfTwo = Alone(new Dependants(Children: 2, ChildrenUnder3: 1, SingleParent: true));

    private static Household Alone(Dependants dependants) => Household.None with { Dependants = dependants };

    // 20,000 less 3% is 19,400, the tax-free amount 11,550 + 7,850 = 19,400 too. The basic tax is 4,180 +
    // 2,680 at 40% = 5,252.00, the tax on the tax-free amount 2,937.50 + 4,970 at 30% + 2,680 at 40% =
    // 5,500.50: no State tax, and 248.50 the basic tax cannot absorb, all of it the children's (their layer,
    // from 13,530 to 19,400, is taxed 2,029.00), under the cap of 1,100: the credit. The credit for low
    // earned income is the full 910.
    [Fact]
    public void Compute_2026SingleParentAtTheTaxFreeAmount_turnsWhatTheTaxCannotAbsorbIntoTheCredit()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 20_000m, 0m, 0.07m, household: SingleParentOfTwo);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 600.00m, TaxableIncome: 19_400.00m,
            BasicTax: 5_252.00m, TaxFreeAmountReduction: 5_500.50m, StateTax: 0m,
            ReducedStateTax: 0m, RegionalAdditionalTax: 0m, IncomeTax: 0m, MunicipalSurcharge: 0m,
            LowEarnedIncomeCredit: 910m)
        {
            Supplements = new TaxFreeAmountSupplements(5_130m, 740m, 1_980m, 0m, 0m),
            ChildTaxCredit = 248.50m,
        });
    }

    // 12,000 less 3% is 11,640, all of it at 25%: 2,910.00, absorbed by the tax on as much tax-free amount.
    // Counting the base and the single parent's supplement only up to the income, the children's 5,870 sit
    // on top of 11,640: 4,744.50 on §2's scale, 1,834.50 the basic tax cannot absorb, capped at 1,100.
    [Fact]
    public void Compute_2026SingleParentWithALowIncome_capsTheCreditAt550PerChild()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 12_000m, 0m, 0.07m, household: SingleParentOfTwo);

        (result.TaxableIncome, result.BasicTax, result.TaxFreeAmountReduction, result.IncomeTax, result.ChildTaxCredit, result.LowEarnedIncomeCredit)
            .ShouldBe((11_640.00m, 2_910.00m, 2_910.00m, 0m, 1_100.00m, 910m));
    }

    // 60,000 less 10,000 of contributions and 1,500 of flat-rate costs: 48,500. Two children raise the
    // tax-free amount to 16,680, whose tax is 2,937.50 + 4,930 at 30% = 4,416.50, all of it absorbed: 17,841.50
    // less it is 13,425.00 of State tax, 10,074.52 federal and 3,350.48 regional, 939.75 of surcharge. No
    // credit.
    [Fact]
    public void Compute_2026TwoChildrenAndAHigherIncome_lowersTheTaxWithNoCredit()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Alone(new Dependants(Children: 2)));

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_500.00m, TaxableIncome: 48_500.00m,
            BasicTax: 17_841.50m, TaxFreeAmountReduction: 4_416.50m, StateTax: 13_425.00m,
            ReducedStateTax: 10_074.52m, RegionalAdditionalTax: 3_350.48m, IncomeTax: 13_425.00m, MunicipalSurcharge: 939.75m,
            LowEarnedIncomeCredit: 0m)
        {
            Supplements = new TaxFreeAmountSupplements(5_130m, 0m, 0m, 0m, 0m),
        });
    }

    // No income at all: the base amount beyond it is left out, so the three children's 11,440 sit on
    // nothing, 2,860.00 at 25%, all of it unabsorbed, capped at 3 × 550.
    [Fact]
    public void Compute_2026NoIncomeAndThreeChildren_refundsTheCap()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 0m, 0m, 0.07m, household: Alone(new Dependants(Children: 3)));

        (result.IncomeTax, result.ChildTaxCredit).ShouldBe((0m, 1_650.00m));
    }

    // The supplements for other dependants are never refunded: only the children's are (art. 132 al. 1,
    // 1° to 6°).
    [Fact]
    public void Compute_2026NoIncomeAndAnotherDependant_refundsNothing()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 0m, 0m, 0.07m, household: Alone(new Dependants(OtherDependants: 1, DependantsAged66InCare: 1)));

        (result.Supplements.Total, result.ChildTaxCredit).ShouldBe((7_930m, 0m));
    }

    // 5,000 of taxable income (5,154.64 less 154.64), a dependant aged 66 in care and a child. The base and
    // the 5,950 count first and only up to the income, so the child's 2,130 sit on 5,000: 7,130 at 25% is
    // 1,782.50, less the basic tax of 1,250.00, 532.50, under the cap of 550. Counting the 66-year-old's
    // supplement beyond the income would have reached the cap.
    [Fact]
    public void Compute_2026OtherSupplementsBeyondTheIncome_areLeftOutOfTheCredit()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 5_154.64m, 0m, 0.07m, household: Alone(new Dependants(Children: 1, DependantsAged66InCare: 1)));

        (result.TaxableIncome, result.BasicTax, result.ChildTaxCredit).ShouldBe((5_000.00m, 1_250.00m, 532.50m));
    }

    // A single parent's supplement and the 66-year-old's are part of the tax-free amount the reduction is
    // on, besides the children's.
    [Fact]
    public void Compute_2026Dependants_raiseTheTaxFreeAmountByEverySupplement()
    {
        var household = Alone(new Dependants(Children: 1, SingleParent: true, DependantsAged66InCare: 1, OtherDependants: 1));

        var result = PersonalTaxCalculator.Compute(Rules2026, 100_000m, 15_000m, 0.07m, household: household);

        // 11,550 + 2,130 + 1,980 + 5,950 + 1,980 = 23,590: 2,937.50 + 1,491.00 + 6,870 at 40%.
        result.TaxFreeAmountReduction.ShouldBe(7_176.50m);
    }

    // Without dependants nothing moves.
    [Fact]
    public void Compute_2026NoDependants_isTheSinglePersonsTax()
    {
        var with = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Household.None);

        (with.Supplements, with.ChildTaxCredit, with.TaxFreeAmountReduction).ShouldBe((TaxFreeAmountSupplements.None, 0m, 2_887.50m));
    }

    // For a Tax-Calc « exercice 2026 » check (income 2025), derived from the notice for tax year 2026: a single
    // parent (code 1001 and rubrique A 5's « Non »), two children (1030), one under 3 (rubrique B 1 c); 1400
    // 21,000.00 and 1405 1,500.00. 19,500 less 3% (585.00) is 18,915.00, above the tax-free amount of 10,910 +
    // 5,110 + 740 + 1,980 = 18,740. The basic tax is 4,080 + 2,595 at 40% = 5,118.00; the tax on the tax-free
    // amount 2,865 + 4,860 at 30% + 2,420 at 40% = 5,291.00: no State tax, and the 173.00 it cannot absorb is
    // the credit (the children's layer is taxed 1,997.00, the cap 1,100). The credit for low earned income is
    // the full 880. The final tax is 1,053.00 to refund, before the withholding.
    [Fact]
    public void Compute_2025SingleParentAboveTheTaxFreeAmount_refundsWhatTheTaxCannotAbsorb()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 21_000m, 1_500m, 0.07m, household: SingleParentOfTwo);

        result.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 585.00m, TaxableIncome: 18_915.00m,
            BasicTax: 5_118.00m, TaxFreeAmountReduction: 5_291.00m, StateTax: 0m,
            ReducedStateTax: 0m, RegionalAdditionalTax: 0m, IncomeTax: 0m, MunicipalSurcharge: 0m,
            LowEarnedIncomeCredit: 880m)
        {
            Supplements = new TaxFreeAmountSupplements(5_110m, 740m, 1_980m, 0m, 0m),
            ChildTaxCredit = 173.00m,
        });
    }

    // The same household on 1400 15,000.00 and 1405 1,000.00: 14,000 less 420.00 is 13,580.00, a basic tax of
    // 3,395.00 and a tax on as much tax-free amount of 2,865 + 2,120 at 30% = 3,501.00. The children's 5,850 on
    // top of the 12,890 counted first reach 18,740: 5,291.00, 1,896.00 unabsorbed, capped at 1,100. A final
    // tax of 1,980.00 to refund.
    [Fact]
    public void Compute_2025SingleParentWithALowIncome_capsTheCredit()
    {
        var result = PersonalTaxCalculator.Compute(Rules2025, 15_000m, 1_000m, 0.07m, household: SingleParentOfTwo);

        (result.TaxableIncome, result.BasicTax, result.TaxFreeAmountReduction, result.IncomeTax, result.ChildTaxCredit, result.LowEarnedIncomeCredit)
            .ShouldBe((13_580.00m, 3_395.00m, 3_501.00m, 0m, 1_100.00m, 880m));
    }
}
