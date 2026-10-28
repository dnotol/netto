using Netto.Core.DirectorPay;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// The reductions for dependants of annex III to the AR/CIR 92 (key formula ESS-SR/2025-0928, n° 13 to
// 15 and its annexes 3 to 5) on made-up monthly pays of income year 2026, taken off the basic tax of
// either method: n° 38 for dependent children, a disabled child counted for two (n° 8); n° 39, 2, for
// each dependant aged 66 or over in a situation of dependency; n° 39, 3, for each other dependant, a
// disabled one counted for two; n° 40, 1, for a single parent with a dependent child, under the single
// method only (n° 40 applies to section 1 of part 2, chapter 2). All the reductions add up, never
// beyond the basic tax (key formula n° 13), and the year's tax is divided by 12 (n° 16).
//
// Expected values are derived from the official amounts by the SPF simulator's chain « Précompte
// professionnel 2026 », sheet « Dirigeants d'entreprise »: N = max(0, I − L − M), O = round(N ÷ 12),
// whose L (children) and M (other family charges) are typed amounts. Its own example, 2,500 a month
// with one child (624), withholds 261.18. Tax-Calc for income year 2026 is not online, and it takes the
// withholding as an input (code 1407) anyway. The November amounts have no simulator of their own:
// they apply the same chain with the coordinated annex's tables.
public class WithholdingCalculatorDependantsTests
{
    private static readonly WithholdingRules January2026 = WithholdingRules.IncomeYear2026;

    private static readonly WithholdingRules November2026 = WithholdingRules.IncomeYear2026.InMonth(11);

    private static readonly WithholdingDependants OneChild = new(1);

    private static readonly WithholdingDependants TwoChildren = new(2);

    // Under the single method: one child on 2,500 is the simulator's row 22; two children on 4,166.67,
    // 1,656 off 10,691.27 to October and 1,644 off 10,589.62 from November; ten children on 10,000,
    // 21,996 + 2 × 3,864 to October and 21,492 + 2 × 3,780 from November.
    public static TheoryData<int, decimal, int, decimal> SingleWithholdingByChildren => new()
    {
        { 1, 2_500m, 1, 261.18m },
        { 11, 2_500m, 1, 247.71m },
        { 1, 4_166.67m, 2, 752.94m },
        { 11, 4_166.67m, 2, 745.47m },
        { 1, 10_000m, 9, 1_238.27m },
        { 11, 10_000m, 9, 1_278.80m },
        { 1, 10_000m, 10, 916.27m },
        { 11, 10_000m, 10, 963.80m },
    };

    [Theory]
    [MemberData(nameof(SingleWithholdingByChildren))]
    public void Compute_2026AsSingleWithChildren_takesNumber38sReduction(int month, decimal monthlyPay, int children, decimal expected)
    {
        var result = Compute(WithholdingRules.IncomeYear2026.InMonth(month), monthlyPay, WithholdingHousehold.AsSingle, new WithholdingDependants(children));

        result.MonthlyWithholding.ShouldBe(expected);
    }

    // The simulator's own example: 3,758.20 less 624 is 3,134.20, a twelfth of it 261.18.
    [Fact]
    public void Compute_2026Pay2500OneChild_isTheSimulatorsExample()
    {
        var result = Compute(January2026, 2_500m, WithholdingHousehold.AsSingle, OneChild);

        (result.BasicTax - result.TaxFreeAmountReduction, result.ChildrenReduction, result.AnnualTax, result.MonthlyWithholding)
            .ShouldBe((3_758.20m, 624m, 3_134.20m, 261.18m));
    }

    // Under the marital quotient: 1,656 off 5,494.33 to October, 1,644 off 5,291.03 from November.
    [Theory]
    [InlineData(1, 319.86)]
    [InlineData(11, 303.92)]
    public void Compute_2026PartnerWithoutIncomeTwoChildren_takesNumber38sReductionOffTheQuotientsTax(int month, decimal expected)
    {
        var result = Compute(WithholdingRules.IncomeYear2026.InMonth(month), 4_166.67m, WithholdingHousehold.PartnerWithoutIncome, TwoChildren);

        result.MonthlyWithholding.ShouldBe(expected);
    }

    // A disabled child counts for two (n° 8): one disabled child is two children's reduction.
    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void Compute_2026OneDisabledChild_isTwoChildrensReduction(int month)
    {
        var rules = WithholdingRules.IncomeYear2026.InMonth(month);

        var disabled = Compute(rules, 4_166.67m, WithholdingHousehold.AsSingle, new WithholdingDependants(1, DisabledChildren: 1));

        disabled.ChildrenReduction.ShouldBe(Compute(rules, 4_166.67m, WithholdingHousehold.AsSingle, TwoChildren).ChildrenReduction);
    }

    // A single parent of two: 1,656 + 624 off 10,691.27 to October, 1,644 + 636 off 10,589.62 from
    // November.
    [Theory]
    [InlineData(1, 624, 700.94)]
    [InlineData(11, 636, 692.47)]
    public void Compute_2026SingleParentOfTwo_takesNumber40Point1sReductionToo(int month, decimal singleParentReduction, decimal expected)
    {
        var result = Compute(WithholdingRules.IncomeYear2026.InMonth(month), 4_166.67m, WithholdingHousehold.AsSingle, TwoChildren with { SingleParent = true });

        (result.SingleParentReduction, result.MonthlyWithholding).ShouldBe((singleParentReduction, expected));
    }

    // N° 40 applies under the single method only: under the quotient a single parent's reduction is none.
    [Fact]
    public void Compute_2026SingleParentPartnerWithoutIncome_takesNoSingleParentReduction()
    {
        var result = Compute(January2026, 4_166.67m, WithholdingHousehold.PartnerWithoutIncome, TwoChildren with { SingleParent = true });

        (result.SingleParentReduction, result.MonthlyWithholding).ShouldBe((0m, 319.86m));
    }

    // N° 40, 1 is for a parent « with one or more dependent children »: without one it gives nothing.
    [Fact]
    public void Compute_2026SingleParentWithoutChildren_takesNoReduction()
    {
        var result = Compute(January2026, 4_166.67m, WithholdingHousehold.AsSingle, new WithholdingDependants(SingleParent: true));

        (result.SingleParentReduction, result.MonthlyWithholding).ShouldBe((0m, 890.94m));
    }

    // One dependant aged 66 or over in a situation of dependency (n° 39, 2), never counted for two (n° 8
    // excepts the person of n° 9), and two other dependants, one disabled, counted for three (n° 39, 3):
    // 1,992 + 3 × 624 to October, 1,992 + 3 × 636 from November.
    [Theory]
    [InlineData(1, WithholdingHousehold.AsSingle, 3_864, 568.94)]
    [InlineData(11, WithholdingHousehold.AsSingle, 3_900, 557.47)]
    [InlineData(1, WithholdingHousehold.PartnerWithoutIncome, 3_864, 135.86)]
    [InlineData(11, WithholdingHousehold.PartnerWithoutIncome, 3_900, 115.92)]
    public void Compute_2026OtherDependants_takesNumber39sReductions(int month, WithholdingHousehold household, decimal dependantsReduction, decimal expected)
    {
        var dependants = new WithholdingDependants(DependantsAged66InCare: 1, OtherDependants: 2, DisabledOtherDependants: 1);

        var result = Compute(WithholdingRules.IncomeYear2026.InMonth(month), 4_166.67m, household, dependants);

        (result.DependantsReduction, result.MonthlyWithholding).ShouldBe((dependantsReduction, expected));
    }

    // The reductions never take the tax below zero (key formula n° 13): 624 off 460.35 on 1,500 a month;
    // under the quotient, 2,500's tax is nothing already.
    [Theory]
    [InlineData(1_500, WithholdingHousehold.AsSingle)]
    [InlineData(2_500, WithholdingHousehold.PartnerWithoutIncome)]
    public void Compute_2026ReductionsBeyondTheBasicTax_withholdNothing(decimal monthlyPay, WithholdingHousehold household)
    {
        var result = Compute(January2026, monthlyPay, household, OneChild);

        (result.AnnualTax, decimal.IsNegative(result.AnnualTax), result.MonthlyWithholding).ShouldBe((0m, false, 0m));
    }

    // Each reduction line is what it took off the tax, never more than was left of it (n° 13), taken in
    // the annex's order: the children's first, then the other dependants', then the single parent's.
    // On 1,500 a month the 624 of one child meets 460.35 of tax; on 2,500, two children's 1,656 and the
    // 1,992 of a dependant aged 66 or over in care leave 110.20 of 3,758.20 for the single parent's 624.
    public static TheoryData<decimal, WithholdingDependants, decimal, decimal, decimal> CappedReductions => new()
    {
        { 1_500m, OneChild, 460.35m, 0m, 0m },
        { 1_500m, new WithholdingDependants(1, OtherDependants: 1, SingleParent: true), 460.35m, 0m, 0m },
        { 2_500m, new WithholdingDependants(2, DependantsAged66InCare: 1, SingleParent: true), 1_656m, 1_992m, 110.20m },
    };

    [Theory]
    [MemberData(nameof(CappedReductions))]
    public void Compute_2026ReductionsBeyondTheTax_takeWhatIsLeftInTheAnnexsOrder(
        decimal monthlyPay, WithholdingDependants dependants, decimal children, decimal others, decimal singleParent)
    {
        var result = Compute(January2026, monthlyPay, WithholdingHousehold.AsSingle, dependants);

        (result.ChildrenReduction, result.DependantsReduction, result.SingleParentReduction, result.AnnualTax).ShouldBe((children, others, singleParent, 0m));
    }

    // Under the quotient, 2,500's tax is nothing already: no reduction takes anything.
    [Fact]
    public void Compute_2026NoTaxLeft_takesNoReduction()
    {
        var result = Compute(January2026, 2_500m, WithholdingHousehold.PartnerWithoutIncome, new WithholdingDependants(1, OtherDependants: 1));

        (result.ChildrenReduction, decimal.IsNegative(result.ChildrenReduction), result.DependantsReduction).ShouldBe((0m, false, 0m));
    }

    // Each reduction is its own line and the result says the dependants it was taken for.
    [Fact]
    public void Compute_2026_listsEachReductionAndEchoesTheDependants()
    {
        var dependants = new WithholdingDependants(2, DependantsAged66InCare: 1, SingleParent: true);

        var result = Compute(January2026, 4_166.67m, WithholdingHousehold.AsSingle, dependants);

        (result.Dependants, result.ChildrenReduction, result.DependantsReduction, result.SingleParentReduction, result.AnnualTax)
            .ShouldBe((dependants, 1_656m, 1_992m, 624m, 6_419.27m));
    }

    // Not saying the dependants is having none, as before the reductions.
    [Theory]
    [InlineData(WithholdingHousehold.AsSingle)]
    [InlineData(WithholdingHousehold.PartnerWithoutIncome)]
    public void Compute_dependantsNotSaid_isNone(WithholdingHousehold household)
    {
        var (value, _) = WithholdingCalculator.Compute(January2026, 4_166.67m, household);

        value.ShouldBe(Compute(January2026, 4_166.67m, household, WithholdingDependants.None));
    }

    [Fact]
    public void Compute_noDependants_takesNoReduction()
    {
        var result = Compute(November2026, 4_166.67m, WithholdingHousehold.AsSingle, WithholdingDependants.None);

        (result.ChildrenReduction, result.DependantsReduction, result.SingleParentReduction, result.MonthlyWithholding).ShouldBe((0m, 0m, 0m, 882.47m));
    }

    // A period of the pay statement cites the amounts its withholding applied, the reductions with them.
    [Fact]
    public void AmountsUsed_periodWithDependants_citeTheReductionsApplied()
    {
        var dependants = TwoChildren with { SingleParent = true };
        var withholding = Compute(November2026, 4_166.67m, WithholdingHousehold.AsSingle, dependants);

        var period = new PayStatementPeriod(November2026, 11, 12, withholding);

        period.AmountsUsed.ShouldBe(November2026.AmountsFor(WithholdingHousehold.AsSingle, dependants));
    }

    // The engine refuses a count below zero before the withholding sees it: here it is a bug.
    public static TheoryData<WithholdingDependants> Impossible => new()
    {
        new WithholdingDependants(-1),
        new WithholdingDependants(1, DisabledChildren: -1),
        new WithholdingDependants(1, DisabledChildren: 2),
        new WithholdingDependants(DependantsAged66InCare: -1),
        new WithholdingDependants(OtherDependants: -1),
        new WithholdingDependants(OtherDependants: 1, DisabledOtherDependants: -1),
        new WithholdingDependants(OtherDependants: 1, DisabledOtherDependants: 2),
    };

    [Theory]
    [MemberData(nameof(Impossible))]
    public void Compute_impossibleDependants_throws(WithholdingDependants dependants)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => WithholdingCalculator.Compute(January2026, 4_166.67m, WithholdingHousehold.AsSingle, dependants));
    }

    private static WithholdingResult Compute(WithholdingRules rules, decimal monthlyPay, WithholdingHousehold household, WithholdingDependants dependants)
    {
        var (value, error) = WithholdingCalculator.Compute(rules, monthlyPay, household, dependants);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
