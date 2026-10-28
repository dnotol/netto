using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// The supplements to the tax-free amount for dependants (CIR 92 art. 132 and 133) and the cap of the
// refundable credit for children (art. 134 §3) for one income year. The children's supplement is one
// amount for one to four children, then EachChildBeyondFour more for each child beyond the fourth; each
// child under 3 adds ChildUnder3; a single parent taxed alone SingleParent; each dependant aged 66 or more
// in a situation of dependence DependantAged66InCare, and each other dependant OtherDependant. The credit is
// at most ChildCreditCapPerChild for each dependent child. Source is the text the supplements' amounts come
// from, the indexation notice while arts 132 and 133 are not filed, and CreditSource art. 134, which the
// credit's step cites as its rule.
public sealed record TaxFreeAmountSupplementRules(
    Source Source,
    Source CreditSource,
    RuleAmount OneChild,
    RuleAmount TwoChildren,
    RuleAmount ThreeChildren,
    RuleAmount FourChildren,
    RuleAmount EachChildBeyondFour,
    RuleAmount ChildUnder3,
    RuleAmount SingleParent,
    RuleAmount DependantAged66InCare,
    RuleAmount OtherDependant,
    RuleAmount ChildCreditCapPerChild)
{
    public IReadOnlyList<RuleAmount> Amounts =>
    [
        OneChild, TwoChildren, ThreeChildren, FourChildren, EachChildBeyondFour, ChildUnder3, SingleParent, DependantAged66InCare, OtherDependant,
        ChildCreditCapPerChild,
    ];

    // What the dependants add to the tax-free amount. A single parent's supplement is for a taxpayer taxed
    // alone with a dependent child (art. 133 al. 1, 1°), never in a joint return.
    public TaxFreeAmountSupplements Of(Dependants dependants, bool taxedJointly)
    {
        ArgumentNullException.ThrowIfNull(dependants);

        return new TaxFreeAmountSupplements(
            ForChildren(dependants.Children),
            dependants.ChildrenUnder3 * ChildUnder3.Value,
            SingleParentApplies(dependants, taxedJointly) ? SingleParent.Value : 0m,
            dependants.DependantsAged66InCare * DependantAged66InCare.Value,
            dependants.OtherDependants * OtherDependant.Value);
    }

    // The most the refundable credit for children can be, for both partners together in a joint return
    // (art. 134 §3, al. 2, and §4, 6°).
    public decimal CreditCap(Dependants dependants)
    {
        ArgumentNullException.ThrowIfNull(dependants);

        return dependants.Children * ChildCreditCapPerChild.Value;
    }

    // The amounts the dependants' supplements and credit used, in the record's order, for the steps to cite.
    public IReadOnlyList<RuleAmount> Used(Dependants dependants, bool taxedJointly)
    {
        ArgumentNullException.ThrowIfNull(dependants);

        var children = dependants.Children switch
        {
            <= 0 => [],
            1 => [OneChild],
            2 => [TwoChildren],
            3 => [ThreeChildren],
            4 => [FourChildren],
            _ => new[] { FourChildren, EachChildBeyondFour },
        };

        return
        [
            .. children,
            .. dependants.ChildrenUnder3 > 0 ? [ChildUnder3] : Array.Empty<RuleAmount>(),
            .. SingleParentApplies(dependants, taxedJointly) ? [SingleParent] : Array.Empty<RuleAmount>(),
            .. dependants.DependantsAged66InCare > 0 ? [DependantAged66InCare] : Array.Empty<RuleAmount>(),
            .. dependants.OtherDependants > 0 ? [OtherDependant] : Array.Empty<RuleAmount>(),
            .. dependants.Children > 0 ? [ChildCreditCapPerChild] : Array.Empty<RuleAmount>(),
        ];
    }

    private static bool SingleParentApplies(Dependants dependants, bool taxedJointly) => dependants.SingleParent && !taxedJointly && dependants.Children > 0;

    private decimal ForChildren(int children) => children switch
    {
        <= 0 => 0m,
        1 => OneChild.Value,
        2 => TwoChildren.Value,
        3 => ThreeChildren.Value,
        4 => FourChildren.Value,
        _ => FourChildren.Value + (children - 4) * EachChildBeyondFour.Value,
    };
}
