using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// What the partner would owe filing alone, the river's baseline in a joint year: a single taxpayer on their
// own net professional income, typed net so no flat-rate costs come off it, with their own tax-free amount,
// no quotient and no dependants, and no credit for low earned income, as the joint assessment counts none
// for them. Income year 2026 at a 7% municipal rate, derived by hand from the official amounts: the scale
// (art. 130), the tax-free amount of 11,550 and the 25% of its tax (art. 131 and 134 §2), the autonomy factor
// and Wallonia's additional rate.
public class PersonalTaxCalculatorPartnerAloneTests
{
    private static readonly PersonalTaxRules Rules = PersonalTaxRules.IncomeYear2026;

    // 30,000: 4,180 + 12,790 at 40% + 490 at 45% is 9,516.50 on the scale, less 2,887.50 on the tax-free
    // amount: 6,629.00, 4,974.60 of it federal and 1,654.40 regional, and 464.03 of municipal surcharge.
    [Fact]
    public void PartnerAlone_ownIncome_isASingleTaxpayersTaxOnIt()
    {
        var alone = PersonalTaxCalculator.PartnerAlone(Rules, 30_000m, 0.07m);

        alone.ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 0m, TaxableIncome: 30_000m,
            BasicTax: 9_516.50m, TaxFreeAmountReduction: 2_887.50m, StateTax: 6_629.00m,
            ReducedStateTax: 4_974.60m, RegionalAdditionalTax: 1_654.40m, IncomeTax: 6_629.00m, MunicipalSurcharge: 464.03m,
            LowEarnedIncomeCredit: 0m));
    }

    // The household with the director earning nothing gives the partner a quotient towards the director
    // (9,000 of the 30,000), which filing alone never gives.
    [Fact]
    public void PartnerAlone_ownIncome_owesMoreThanTheHouseholdWithTheDirectorEarningNothing()
    {
        var household = new Household(HouseholdStatus.MarriedOrLegalCohabitant, 30_000m);

        var alone = PersonalTaxCalculator.PartnerAlone(Rules, 30_000m, 0.07m);

        alone.IncomeTax.ShouldBeGreaterThan(PersonalTaxCalculator.Compute(Rules, 0m, 0m, 0.07m, household: household).IncomeTax);
    }

    [Fact]
    public void PartnerAlone_noIncome_owesNothing()
    {
        var alone = PersonalTaxCalculator.PartnerAlone(Rules, 0m, 0.07m);

        (alone.IncomeTax, alone.MunicipalSurcharge, alone.LowEarnedIncomeCredit).ShouldBe((0m, 0m, 0m));
    }
}
