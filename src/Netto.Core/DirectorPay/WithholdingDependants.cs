using Netto.Core.PersonalTax;

namespace Netto.Core.DirectorPay;

// The director's dependants as the monthly withholding takes them (annex III to the AR/CIR 92, part 3,
// chapter 2): Children, the dependent children, of whom DisabledChildren are disabled (n° 3.1) and each
// counted for two (n° 8), for n° 38; DependantsAged66InCare, the dependants of art. 136, 2° and 3°, CIR 92
// aged 66 or over in a situation of dependency (n° 9), never counted for two, for n° 39, 2; OtherDependants,
// the other dependants of art. 136, 2° to 4°, of whom DisabledOtherDependants are disabled (n° 3.2) and
// each counted for two, for n° 39, 3; SingleParent, a widow or widower not remarried, an unmarried,
// divorced or separated parent, whose reduction of n° 40, 1 needs a dependent child.
//
// The annex takes the family situation on 1 January of the year the pay is paid (n° 5.1), the tax on
// 1 January of the tax year: Netto withholds by the same dependants the user declares for the tax, as
// n° 5.2 a lets the payer take a change into account once told of it.
public sealed record WithholdingDependants(
    int Children = 0,
    int DisabledChildren = 0,
    int DependantsAged66InCare = 0,
    int OtherDependants = 0,
    int DisabledOtherDependants = 0,
    bool SingleParent = false)
{
    public static WithholdingDependants None { get; } = new();

    // The dependants the director's withholding takes from those declared for the tax: all of them when
    // the assessment gives their supplements to the director's column (CIR 92 art. 134 §4, the higher
    // income in a joint return), none when it gives them to the partner, who claims them on their own
    // pay (annex III n° 6 lets the couple choose). The children under 3 raise only the tax; a single
    // parent counts only when not taxed jointly, as for the tax. No disability is declared yet, so
    // nobody counts for two.
    public static WithholdingDependants Of(Household household, Taxpayer supplementsTo)
    {
        var declared = household.Dependants;

        return supplementsTo == Taxpayer.Partner
            ? None
            : new WithholdingDependants(
                declared.Children,
                DependantsAged66InCare: declared.DependantsAged66InCare,
                OtherDependants: declared.OtherDependants,
                SingleParent: declared.SingleParent && !household.TaxedJointly);
    }

    // The children n° 38's table is read at, a disabled child counted for two.
    public int CountedChildren => Children + DisabledChildren;

    // The other dependants n° 39, 3 is multiplied by, a disabled one counted for two.
    public int CountedOtherDependants => OtherDependants + DisabledOtherDependants;

    // Whether n° 40, 1 applies: a single parent with a dependent child, under the single method, the
    // only one n° 40 applies to.
    public bool SingleParentUnder(WithholdingHousehold household) =>
        SingleParent && Children > 0 && household == WithholdingHousehold.AsSingle;

    // The counts the engine refuses before the withholding sees them: a bug here.
    internal void ThrowIfImpossible()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Children);
        ArgumentOutOfRangeException.ThrowIfNegative(DisabledChildren);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(DisabledChildren, Children);
        ArgumentOutOfRangeException.ThrowIfNegative(DependantsAged66InCare);
        ArgumentOutOfRangeException.ThrowIfNegative(OtherDependants);
        ArgumentOutOfRangeException.ThrowIfNegative(DisabledOtherDependants);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(DisabledOtherDependants, OtherDependants);
    }
}
