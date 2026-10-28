using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// What the user declares about their household: a joint assessment for a married or legally cohabiting
// couple (CIR 92 art. 126), except in a year they are taxed separately, the year the couple starts or
// ends; a director alone, a de facto cohabitant among them, is always taxed alone.
public class HouseholdTests
{
    [Fact]
    public void None_isTheDirectorAloneWithNoPartnerIncome()
    {
        (Household.None.Status, Household.None.PartnerIncome, Household.None.BothAged66, Household.None.TaxedSeparately, Household.None.TaxedJointly)
            .ShouldBe((HouseholdStatus.Alone, 0m, false, false, false));
    }

    // Dependants live with or without a partner; none unless given.
    [Fact]
    public void None_hasNoDependants()
    {
        (Household.None.Dependants, new Household(HouseholdStatus.MarriedOrLegalCohabitant).Dependants).ShouldBe((Dependants.None, Dependants.None));
    }

    // A dependant is a person counted: a child, a dependant aged 66 or more in care, any other. Raising the
    // children alone, ticked with none, declares nobody; the children under 3 are among the children.
    [Theory]
    [InlineData(0, 0, false, 0, 0, false)]
    [InlineData(0, 0, true, 0, 0, false)]
    [InlineData(1, 0, false, 0, 0, true)]
    [InlineData(0, 0, false, 1, 0, true)]
    [InlineData(0, 0, false, 0, 1, true)]
    [InlineData(2, 1, true, 0, 0, true)]
    public void AnyDeclared_dependants_isWhetherAnyoneIsCounted(int children, int under3, bool singleParent, int inCare, int others, bool declared)
    {
        new Dependants(children, under3, singleParent, inCare, others).AnyDeclared.ShouldBe(declared);
    }

    [Theory]
    [InlineData(HouseholdStatus.Alone, false, false)]
    [InlineData(HouseholdStatus.Alone, true, false)]
    [InlineData(HouseholdStatus.MarriedOrLegalCohabitant, false, true)]
    [InlineData(HouseholdStatus.MarriedOrLegalCohabitant, true, false)]
    public void TaxedJointly_statusAndYear_isACoupleOutsideAYearTaxedSeparately(HouseholdStatus status, bool taxedSeparately, bool joint)
    {
        var household = new Household(status, 10_000m, TaxedSeparately: taxedSeparately);

        household.TaxedJointly.ShouldBe(joint);
    }
}
