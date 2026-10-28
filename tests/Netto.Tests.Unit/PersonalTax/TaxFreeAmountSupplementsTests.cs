using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// The supplements to the tax-free amount for the director's dependants (CIR 92 art. 132 and 133), as the
// indexation notices give them: for income year 2026 the notice for tax year 2027, table II, the child
// supplements frozen at the coefficient 2.2793 (art. 178 §2/1) on the bases the law of 15.07.2026 raised for
// one and two children; for income year 2025 the notice for tax year 2026, table I. Arts 132 and 133 are
// not filed.
public class TaxFreeAmountSupplementsTests
{
    // One child 2,130, two 5,130, three 11,440, four 18,510, then 7,070 for each child beyond the fourth.
    [Theory]
    [InlineData(2026, 0, 0)]
    [InlineData(2026, 1, 2_130)]
    [InlineData(2026, 2, 5_130)]
    [InlineData(2026, 3, 11_440)]
    [InlineData(2026, 4, 18_510)]
    [InlineData(2026, 5, 25_580)]
    [InlineData(2026, 6, 32_650)]
    [InlineData(2025, 1, 1_980)]
    [InlineData(2025, 2, 5_110)]
    [InlineData(2025, 3, 11_440)]
    [InlineData(2025, 4, 18_510)]
    [InlineData(2025, 5, 25_580)]
    public void Of_children_isTheSupplementForTheirNumber(int incomeYear, int children, int expected)
    {
        var rules = Rules(incomeYear);

        var supplements = rules.Of(new Dependants(Children: children), taxedJointly: false);

        (supplements.Children, supplements.ForChildren, supplements.Total).ShouldBe((expected, expected, expected));
    }

    // 740 for each child under 3 for whom no childcare costs are claimed, counted among the children too.
    [Theory]
    [InlineData(2026)]
    [InlineData(2025)]
    public void Of_childrenUnder3_addsTheirSupplementToTheChildren(int incomeYear)
    {
        var rules = Rules(incomeYear);

        var supplements = rules.Of(new Dependants(Children: 2, ChildrenUnder3: 2), taxedJointly: false);

        (supplements.ChildrenUnder3, supplements.ForChildren).ShouldBe((1_480m, rules.TwoChildren.Value + 1_480m));
    }

    // A single parent taxed alone with dependent children: 1,980, not a child supplement.
    [Theory]
    [InlineData(2026)]
    [InlineData(2025)]
    public void Of_singleParentTaxedAlone_adds1980BesidesTheChildren(int incomeYear)
    {
        var rules = Rules(incomeYear);

        var supplements = rules.Of(new Dependants(Children: 1, SingleParent: true), taxedJointly: false);

        (supplements.SingleParent, supplements.ForChildren, supplements.Others).ShouldBe((1_980m, rules.OneChild.Value, 1_980m));
    }

    // Art. 133 al. 1, 1° is for a taxpayer taxed alone who has a dependent child: a joint return, or no
    // child, gives nothing for it.
    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void Of_singleParentJointOrWithoutChildren_addsNothingForIt(int children, bool taxedJointly)
    {
        var supplements = Rules(2026).Of(new Dependants(Children: children, SingleParent: true), taxedJointly);

        supplements.SingleParent.ShouldBe(0m);
    }

    // Art. 132 al. 1, 7°: 5,950 for each dependant aged 66 or more in a situation of dependence; 8°: 1,980
    // for each other dependant. Neither is a child supplement.
    [Theory]
    [InlineData(2026)]
    [InlineData(2025)]
    public void Of_otherDependants_addsTheirSupplementsBesidesTheChildren(int incomeYear)
    {
        var supplements = Rules(incomeYear).Of(new Dependants(DependantsAged66InCare: 1, OtherDependants: 2), taxedJointly: true);

        (supplements.DependantsAged66InCare, supplements.OtherDependants, supplements.ForChildren, supplements.Others, supplements.Total)
            .ShouldBe((5_950m, 3_960m, 0m, 9_910m, 9_910m));
    }

    [Fact]
    public void Of_noDependants_isNone()
    {
        Rules(2026).Of(Dependants.None, taxedJointly: false).ShouldBe(TaxFreeAmountSupplements.None);
    }

    // 550 per dependent child, not indexed (art. 134 §3, al. 2, as the law of 18.12.2025 set it from tax year
    // 2026), whatever their age.
    [Theory]
    [InlineData(2026, 3, 1_650)]
    [InlineData(2025, 2, 1_100)]
    [InlineData(2026, 0, 0)]
    public void CreditCap_children_is550PerChild(int incomeYear, int children, int expected)
    {
        Rules(incomeYear).CreditCap(new Dependants(Children: children, ChildrenUnder3: Math.Min(children, 1), OtherDependants: 1)).ShouldBe(expected);
    }

    // The amounts each year's step cites: the notice of its tax year for the supplements, art. 134 of its
    // income year for the credit's cap.
    public static TheoryData<int, string, decimal, string> AmountsAndDocuments => new()
    {
        { 2026, "supplement_one_child", 2_130m, "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html" },
        { 2026, "supplement_two_children", 5_130m, "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html" },
        { 2026, "supplement_three_children", 11_440m, "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html" },
        { 2026, "supplement_four_children", 18_510m, "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html" },
        { 2026, "supplement_each_child_beyond_four", 7_070m, "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html" },
        { 2026, "supplement_child_under_3", 740m, "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html" },
        { 2026, "supplement_single_parent", 1_980m, "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html" },
        { 2026, "supplement_dependant_aged_66_in_care", 5_950m, "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html" },
        { 2026, "supplement_other_dependant", 1_980m, "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html" },
        { 2026, "child_tax_credit_cap_per_child", 550m, "personal-tax/spf-finances_cir92-art-134-revenus-2026_2026-08-05.html" },
        { 2025, "supplement_one_child", 1_980m, "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html" },
        { 2025, "supplement_two_children", 5_110m, "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html" },
        { 2025, "supplement_three_children", 11_440m, "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html" },
        { 2025, "supplement_four_children", 18_510m, "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html" },
        { 2025, "supplement_each_child_beyond_four", 7_070m, "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html" },
        { 2025, "supplement_child_under_3", 740m, "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html" },
        { 2025, "supplement_single_parent", 1_980m, "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html" },
        { 2025, "supplement_dependant_aged_66_in_care", 5_950m, "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html" },
        { 2025, "supplement_other_dependant", 1_980m, "personal-tax/spf-finances_avis-indexation-exercice-2026_2026-01-19.html" },
        { 2025, "child_tax_credit_cap_per_child", 550m, "personal-tax/spf-finances_cir92-art-134-revenus-2025_2026-01-14.html" },
    };

    [Theory]
    [MemberData(nameof(AmountsAndDocuments))]
    public void Amounts_eachAmount_isItsYearsFromItsDocument(int incomeYear, string key, decimal value, string document)
    {
        var amount = Rules(incomeYear).Amounts.Single(amount => amount.Key == key);

        (amount.Value, amount.Source.Document).ShouldBe((value, document));
    }

    // The amounts a step cites are the ones the dependants used: two children of whom one under 3 cite the
    // two children's supplement and the one for a child under 3, and the credit's cap.
    [Fact]
    public void Used_dependants_areTheAmountsTheirSupplementsAndCreditTook()
    {
        var rules = Rules(2026);

        var used = rules.Used(new Dependants(Children: 2, ChildrenUnder3: 1, OtherDependants: 1), taxedJointly: false);

        used.ShouldBe([rules.TwoChildren, rules.ChildUnder3, rules.OtherDependant, rules.ChildCreditCapPerChild]);
    }

    // More than four children use the four children's supplement and the one for each child beyond.
    [Fact]
    public void Used_fiveChildren_citesTheFourAndTheOneBeyond()
    {
        var rules = Rules(2026);

        var used = rules.Used(new Dependants(Children: 5), taxedJointly: false);

        used.ShouldBe([rules.FourChildren, rules.EachChildBeyondFour, rules.ChildCreditCapPerChild]);
    }

    private static TaxFreeAmountSupplementRules Rules(int incomeYear) => PersonalTaxRules.For(incomeYear, Region.Wallonia).ShouldNotBeNull().Supplements;
}
