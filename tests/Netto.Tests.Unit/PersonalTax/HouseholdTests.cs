using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// What the user declares about their household: a joint assessment for a married or legally cohabiting
// couple (CIR 92 art. 126), except in the year it starts or ends, when each is taxed alone; a single
// person, a de facto cohabitant among them, is always taxed alone.
public class HouseholdTests
{
    [Fact]
    public void None_isASinglePersonWithNoPartnerIncome()
    {
        (Household.None.Status, Household.None.PartnerIncome, Household.None.BothAged66, Household.None.SeparateYear, Household.None.TaxedJointly)
            .ShouldBe((HouseholdStatus.Alone, 0m, false, SeparateYear.None, false));
    }

    [Theory]
    [InlineData(HouseholdStatus.Alone, SeparateYear.None, false)]
    [InlineData(HouseholdStatus.Alone, SeparateYear.Start, false)]
    [InlineData(HouseholdStatus.MarriedOrLegalCohabitant, SeparateYear.None, true)]
    [InlineData(HouseholdStatus.MarriedOrLegalCohabitant, SeparateYear.Start, false)]
    [InlineData(HouseholdStatus.MarriedOrLegalCohabitant, SeparateYear.End, false)]
    public void TaxedJointly_statusAndYear_isACoupleOutsideTheYearItStartsOrEnds(HouseholdStatus status, SeparateYear year, bool joint)
    {
        var household = new Household(status, 10_000m, SeparateYear: year);

        household.TaxedJointly.ShouldBe(joint);
    }
}
