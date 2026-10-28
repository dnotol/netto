using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.DirectorPay;

// The pay of a director taxed jointly with a spouse or legal cohabitant. What the pay, the river and the
// assessment's balance count is what the director's income adds to the household's tax: the joint
// tax with it less the joint tax on the partner's own income alone, which is the partner's and left
// out, as is what was withheld on it (decided by the operator, 2026-10-28). The household's two columns
// and what the quotient saves are beside it. Income year 2026 at a 7% municipal rate; the joint tax
// itself is pinned by PersonalTaxCalculatorJointTests, so these hold the pay to it.
public class GrossToNetHouseholdTests
{
    private static readonly PersonalTaxRules Rules = PersonalTaxRules.IncomeYear2026;

    private static readonly Household NonEarningPartner = new(HouseholdStatus.MarriedOrLegalCohabitant);

    // 40,000 is more than 30% of the two together, so no quotient applies with the pay; alone, the
    // partner would be taxed on it, the quotient passing part of it to the director's side.
    private static readonly Household EarningPartner = new(HouseholdStatus.MarriedOrLegalCohabitant, 40_000m);

    private static GrossToNetResult Compute(decimal grossPay, TaxPrepayments prepayments, Household? household = null)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), prepayments, household: household);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    // The joint tax on the pay's taxable income, the own contributions deducted as the pay deducts them.
    private static PersonalTaxResult JointTaxOn(GrossToNetResult pay, Household household) =>
        PersonalTaxCalculator.Compute(Rules, pay.TaxablePay, pay.SocialContributions + pay.FundFee, 0.07m, household: household);

    private static PersonalTaxResult PartnerAlone(Household household) => PersonalTaxCalculator.Compute(Rules, 0m, 0m, 0.07m, household: household);

    [Fact]
    public void Compute_partnerWithoutIncome_isTheHouseholdsJointTax()
    {
        var pay = Compute(60_000m, Withheld, NonEarningPartner);
        var joint = JointTaxOn(pay, NonEarningPartner);

        (pay.IncomeTax, pay.MunicipalSurcharge, pay.LowEarnedIncomeCredit).ShouldBe((joint.IncomeTax, joint.MunicipalSurcharge, joint.LowEarnedIncomeCredit));
        pay.IncomeTax.ShouldBeLessThan(Compute(60_000m, Withheld).IncomeTax);
    }

    [Fact]
    public void Compute_partnerWithOwnIncome_countsWhatThePayAddsToTheHouseholdsTax()
    {
        var pay = Compute(60_000m, Withheld, EarningPartner);
        var (joint, alone) = (JointTaxOn(pay, EarningPartner), PartnerAlone(EarningPartner));

        (pay.IncomeTax, pay.MunicipalSurcharge).ShouldBe((joint.IncomeTax - alone.IncomeTax, joint.MunicipalSurcharge - alone.MunicipalSurcharge));
        alone.IncomeTax.ShouldBeGreaterThan(0m);
    }

    // The household's columns are the whole year's, the partner's own income included, and beside them
    // what the partner's income alone costs the household, which the pay leaves out.
    [Fact]
    public void Compute_partnerWithOwnIncome_holdsTheHouseholdsColumnsAndThePartnersOwnTax()
    {
        var pay = Compute(60_000m, Withheld, EarningPartner);

        var household = pay.Household.ShouldNotBeNull();
        household.Year.ShouldBe(JointTaxOn(pay, EarningPartner).Joint);
        household.PartnerOwn.ShouldBe(PartnerAlone(EarningPartner));
    }

    // The balance is what the director's income adds, never the partner's own tax: the final tax is the
    // pay's lines, less what was withheld on the pay.
    [Fact]
    public void Compute_partnerWithOwnIncome_leavesThePartnersOwnTaxOutOfTheBalance()
    {
        var pay = Compute(60_000m, new TaxPrepayments(10_000m, AdvancePayments.None), EarningPartner);

        var finalTax = pay.IncomeTax + pay.MunicipalSurcharge - pay.LowEarnedIncomeCredit + pay.AdvancePayments.Surcharge - pay.AdvancePayments.Bonus;
        (pay.Assessment.FinalTax, pay.Assessment.Balance).ShouldBe((finalTax, finalTax - 10_000m));
    }

    // The surcharge is on the pay's share of the household's tax (art. 157 and 158), less its withholding.
    [Fact]
    public void Compute_nothingWithheldNorPrepaid_surchargesThePaysShareOfTheHouseholdsTax()
    {
        var pay = Compute(60_000m, TaxPrepayments.None, EarningPartner);

        var expected = IncomeTaxAdvancePaymentCalculator.Compute(Rules.AdvancePayments, pay.IncomeTax, pay.LowEarnedIncomeCredit, TaxPrepayments.None);
        pay.AdvancePayments.Surcharge.ShouldBe(expected.Surcharge);
    }

    // With a partner who earns nothing, the quotient lowers the pay's tax, and so its surcharge.
    [Fact]
    public void Compute_nothingWithheldNorPrepaidWithAPartnerWithoutIncome_surchargesLessThanAlone()
    {
        var pay = Compute(60_000m, TaxPrepayments.None, NonEarningPartner);

        pay.AdvancePayments.Surcharge.ShouldBeLessThan(Compute(60_000m, TaxPrepayments.None).AdvancePayments.Surcharge);
    }

    // What the quotient saves: the pay's share of the household's tax were the quotient not applied to
    // the year, the partner's own tax alone left as it is, less its share with it; so the household's tax
    // without the quotient less with it. A partner with 10,000 of their own is topped up by 1,780, the
    // cap less their own.
    [Theory]
    [InlineData(10_000)]
    [InlineData(0)]
    public void Compute_household_saysWhatTheQuotientSaves(int partnerIncome)
    {
        var household = new Household(HouseholdStatus.MarriedOrLegalCohabitant, partnerIncome);
        var pay = Compute(60_000m, Withheld, household);

        var with = JointTaxOn(pay, household);
        var without = PersonalTaxCalculator.WithoutQuotient(Rules, pay.TaxablePay, pay.SocialContributions + pay.FundFee, 0.07m, 0m, null, household);
        var saves = without.IncomeTax + without.MunicipalSurcharge - with.IncomeTax - with.MunicipalSurcharge;
        pay.Household.ShouldNotBeNull().QuotientSaves.ShouldBe(saves);
        saves.ShouldBeGreaterThan(0m);
    }

    // A partner earning 30% or more is attributed nothing: the quotient saves nothing.
    [Fact]
    public void Compute_partnerEarning30PercentOrMore_saysTheQuotientSavesNothing()
    {
        var pay = Compute(60_000m, Withheld, EarningPartner);

        pay.Household.ShouldNotBeNull().QuotientSaves.ShouldBe(0m);
    }

    // A year taxed separately, or a director alone, is the director's tax alone, with no household.
    [Theory]
    [InlineData(HouseholdStatus.MarriedOrLegalCohabitant, true)]
    [InlineData(HouseholdStatus.Alone, false)]
    public void Compute_notTaxedJointly_isTheDirectorAlone(HouseholdStatus status, bool taxedSeparately)
    {
        var pay = Compute(60_000m, Withheld, new Household(status, 18_000m, TaxedSeparately: taxedSeparately));
        var alone = Compute(60_000m, Withheld);

        (pay.IncomeTax, pay.MunicipalSurcharge, pay.NetInPocket, pay.Assessment, pay.Household).ShouldBe((alone.IncomeTax, alone.MunicipalSurcharge, alone.NetInPocket, alone.Assessment, null));
    }

    [Fact]
    public void Compute_jointWithNegativePartnerIncome_isRefused()
    {
        var (value, error) = GrossToNet.Compute(
            2026, 60_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, household: new Household(HouseholdStatus.MarriedOrLegalCohabitant, -1m));

        (value, error).ShouldBe((null, GrossToNet.PartnerIncomeNegative));
        GrossToNet.PartnerIncomeNegative.Input.ShouldBe(RefusedInput.PartnerIncome);
    }

    // A partner the household is not taxed with is not read.
    [Fact]
    public void Compute_notJointWithNegativePartnerIncome_isNotRefused()
    {
        var (_, error) = GrossToNet.Compute(
            2026, 60_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, household: new Household(HouseholdStatus.Alone, -1m));

        error.ShouldBeNull();
    }

    // A joint year's income tax cites what it used besides a single person's amounts: the rest of the
    // scale of the tax on the tax-free amount and the marital quotient, with art. 134 §4 and the quotient's text.
    [Fact]
    public void Compute_household_incomeTaxStepCitesTheQuotientAndTheSharedTaxFreeAmount()
    {
        var pay = Compute(60_000m, Withheld, NonEarningPartner);

        var step = pay.Steps.Single(step => step.RuleKey == "income_tax");
        step.AmountsUsed.ShouldBe(Rules.JointIncomeTaxAmounts(bothAged66: false));
        step.AlsoCites.ShouldBe([Rules.MaritalQuotient.Source, Rules.TaxFreeAmountSharing]);
    }

    [Fact]
    public void Compute_alone_incomeTaxStepCitesASinglePersonsAmounts()
    {
        var pay = Compute(60_000m, Withheld);

        var step = pay.Steps.Single(step => step.RuleKey == "income_tax");
        step.AmountsUsed.ShouldBe(Rules.IncomeTaxAmounts);
        step.AlsoCites.ShouldBeEmpty();
    }
}
