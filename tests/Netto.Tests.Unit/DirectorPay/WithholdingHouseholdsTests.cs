using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// The household the withholding takes from the one the user declares (annex III to the AR/CIR 92):
// the marital quotient of n° 34 for a spouse or legal cohabitant (n° 2) taxed jointly whose partner
// has no professional income, its 66 variant when both have reached 66; the single method of n° 32
// otherwise: a director alone or a de facto cohabitant, a partner with professional income of their
// own, or a year the couple is taxed separately, which n° 4.2 b treats as single (the year of the
// marriage or the declaration, of the divorce or the end of the cohabitation, and from the year after
// a de facto separation). The year of a partner's death, which n° 4.2 does not list, is taxed
// separately too and so taken as single: a miss accepted (decided 2026-10-28).
public class WithholdingHouseholdsTests
{
    public static TheoryData<Household, WithholdingHousehold> Households => new()
    {
        { Household.None, WithholdingHousehold.AsSingle },
        { new Household(HouseholdStatus.Alone, BothAged66: true), WithholdingHousehold.AsSingle },
        { new Household(HouseholdStatus.MarriedOrLegalCohabitant), WithholdingHousehold.PartnerWithoutIncome },
        { new Household(HouseholdStatus.MarriedOrLegalCohabitant, BothAged66: true), WithholdingHousehold.PartnerWithoutIncomeBothAged66 },
        { new Household(HouseholdStatus.MarriedOrLegalCohabitant, 0.01m), WithholdingHousehold.AsSingle },
        { new Household(HouseholdStatus.MarriedOrLegalCohabitant, 40_000m, BothAged66: true), WithholdingHousehold.AsSingle },
        { new Household(HouseholdStatus.MarriedOrLegalCohabitant, TaxedSeparately: true), WithholdingHousehold.AsSingle },
        { new Household(HouseholdStatus.MarriedOrLegalCohabitant, BothAged66: true, TaxedSeparately: true), WithholdingHousehold.AsSingle },
    };

    [Theory]
    [MemberData(nameof(Households))]
    public void Of_household_isTheWithholdingsMethod(Household household, WithholdingHousehold expected)
    {
        WithholdingHouseholds.Of(household).ShouldBe(expected);
    }
}
