using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// The dependants the director's withholding takes from those the user declares for the tax (annex III to
// the AR/CIR 92, n° 6 and 38 to 40): all of them when the assessment gives the dependants' supplements to
// the director's column (CIR 92 art. 134 §4, the higher income in a joint return; always the director
// alone), none when it gives them to the partner, who claims them on their own pay. The children under 3
// raise only the tax; no disability is declared yet, so nobody counts for two; a single parent counts
// only when not taxed jointly, as for the tax.
public class WithholdingDependantsTests
{
    private static readonly Dependants Declared = new(Children: 2, ChildrenUnder3: 1, SingleParent: true, DependantsAged66InCare: 1, OtherDependants: 3);

    [Fact]
    public void Of_noDependants_isNone()
    {
        WithholdingDependants.Of(Household.None, Taxpayer.Director).ShouldBe(WithholdingDependants.None);
    }

    [Fact]
    public void Of_directorAlone_isTheDeclaredDependantsWithoutTheChildrenUnder3()
    {
        var household = Household.None with { Dependants = Declared };

        WithholdingDependants.Of(household, Taxpayer.Director).ShouldBe(new WithholdingDependants(2, 0, 1, 3, 0, SingleParent: true));
    }

    [Fact]
    public void Of_coupleTaxedJointly_isNoSingleParent()
    {
        var household = new Household(HouseholdStatus.MarriedOrLegalCohabitant, 10_000m) { Dependants = Declared };

        WithholdingDependants.Of(household, Taxpayer.Director).ShouldBe(new WithholdingDependants(2, 0, 1, 3, 0, SingleParent: false));
    }

    // A year taxed separately, the single parent counts as for the tax.
    [Fact]
    public void Of_coupleTaxedSeparately_keepsTheSingleParent()
    {
        var household = new Household(HouseholdStatus.MarriedOrLegalCohabitant, TaxedSeparately: true) { Dependants = Declared };

        WithholdingDependants.Of(household, Taxpayer.Director).SingleParent.ShouldBeTrue();
    }

    [Fact]
    public void Of_supplementsToThePartner_isNone()
    {
        var household = new Household(HouseholdStatus.MarriedOrLegalCohabitant, 80_000m) { Dependants = Declared };

        WithholdingDependants.Of(household, Taxpayer.Partner).ShouldBe(WithholdingDependants.None);
    }
}
