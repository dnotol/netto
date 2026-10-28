using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.DirectorPay;

// What declaring the household saves the river, the figures the site's menu and the Household card read:
// DependantsSave, the river's share of the household's tax (income tax and municipal surcharge, less the
// credit for children) without the dependants less with them, everything else as declared; HouseholdSaves,
// the director's tax as a single person with no dependants less the river's share as declared. With
// enough withheld to leave no advance-payment surcharge, each is what the household adds to the pocket.
public class GrossToNetHouseholdSavesTests
{
    private static GrossToNetResult Compute(decimal grossPay, Household household, TaxedWithPay? withPay = null)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, withPay: withPay, household: household);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    private static readonly Dependants TwoChildren = new(Children: 2, ChildrenUnder3: 1);

    public static TheoryData<string, Household> Households => new()
    {
        { "alone with two children", Household.None with { Dependants = TwoChildren } },
        { "a single parent on a low pay", Household.None with { Dependants = TwoChildren with { SingleParent = true } } },
        { "a partner earning nothing", NonEarningPartner },
        { "a partner earning nothing, two children", NonEarningPartner with { Dependants = TwoChildren } },
        { "a partner earning 40,000, two children", EarningPartner with { Dependants = TwoChildren } },
    };

    [Theory]
    [MemberData(nameof(Households))]
    public void Compute_household_savesWhatItAddsToThePocket(string household, Household facts)
    {
        _ = household;
        foreach (var grossPay in new[] { 20_000m, 60_000m })
        {
            var pay = Compute(grossPay, facts, new TaxedWithPay { OtherIncome = 828m });
            var single = Compute(grossPay, Household.None, new TaxedWithPay { OtherIncome = 828m });

            Math.Abs(pay.HouseholdSaves - (pay.YearNetInPocket - single.YearNetInPocket)).ShouldBeLessThanOrEqualTo(0.01m);
        }
    }

    // Alone, the household is the dependants: what they save is all it saves, the tax without them less
    // with them.
    [Fact]
    public void Compute_aloneWithChildren_savesWhatTheDependantsSave()
    {
        var pay = Compute(60_000m, Household.None with { Dependants = TwoChildren });

        var without = Compute(60_000m, Household.None);
        var expected = without.IncomeTax + without.MunicipalSurcharge - pay.IncomeTax - pay.MunicipalSurcharge + pay.ChildTaxCredit;
        (pay.DependantsSave, pay.HouseholdSaves).ShouldBe((expected, expected));
        expected.ShouldBeGreaterThan(0m);
    }

    // With a partner, the dependants save the household's tax with them against without them, the partner
    // as declared both times; the household saves that and what joint taxation saves besides.
    [Fact]
    public void Compute_partnerAndChildren_savesTheDependantsAndTheJointTaxation()
    {
        var withChildren = Compute(60_000m, NonEarningPartner with { Dependants = TwoChildren });
        var partnerOnly = Compute(60_000m, NonEarningPartner);

        withChildren.DependantsSave.ShouldBe(partnerOnly.IncomeTax + partnerOnly.MunicipalSurcharge - withChildren.IncomeTax - withChildren.MunicipalSurcharge
            + withChildren.ChildTaxCredit);
        withChildren.HouseholdSaves.ShouldBe(partnerOnly.HouseholdSaves + withChildren.DependantsSave);
        (partnerOnly.DependantsSave, partnerOnly.HouseholdSaves > 0m).ShouldBe((0m, true));
    }

    [Fact]
    public void Compute_directorAloneWithoutDependants_savesNothing()
    {
        var pay = Compute(60_000m, Household.None);

        (pay.DependantsSave, pay.HouseholdSaves).ShouldBe((0m, 0m));
    }
}
