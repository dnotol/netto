using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.DirectorPay;

// The pay of a director with dependants: the personal tax takes their supplements and the credit for
// children (pinned by PersonalTaxCalculatorDependantsTests), and the pay, its pocket, its next 1,000, its
// steps and the assessment's balance follow. The credit comes off after the municipal surcharge, as the
// credit for low earned income, is refunded beyond the tax (art. 304 §2) and is a federal credit the
// advance-payment surcharge's base subtracts (art. 175, 1°). Income year 2026 at a 7% municipal rate; the
// credits below are derived by hand from the official amounts (art. 130, 131, 134 §2 and §3, the notice for
// tax year 2027), on the taxable income the engine's contributions leave: two children, one under 3, and a
// single parent raise the tax-free amount to 11,550 + 5,130 + 740 + 1,980 = 19,400, taxed 5,500.50 on §2's
// scale.
public class GrossToNetDependantsTests
{
    private static readonly PersonalTaxRules Rules = PersonalTaxRules.IncomeYear2026;

    // A single parent with two children, one under 3.
    private static readonly Household SingleParentOfTwo = Household.None with { Dependants = new Dependants(Children: 2, ChildrenUnder3: 1, SingleParent: true) };

    private static GrossToNetResult Compute(decimal grossPay, Household household, TaxPrepayments? prepayments = null, TaxedWithPay? withPay = null)
    {
        var (value, error) = GrossToNet.Compute(
            2026, grossPay, MunicipalRates.IncomeYear2026.Entered(0.07m), prepayments ?? Withheld, withPay: withPay, household: household);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    private static PersonalTaxResult TaxOn(GrossToNetResult pay, Household household, decimal otherIncome = 0m) =>
        PersonalTaxCalculator.Compute(Rules, pay.TaxablePay, pay.SocialContributions + pay.FundFee, 0.07m, otherIncome, household: household);

    // 20,000 leaves 15,945.17 of taxable income, under the 19,400 of tax-free amount: no tax, and 5,500.50
    // less the basic tax of 3,986.29 leaves 1,514.21 unabsorbed, all of it the children's, capped at 1,100.
    [Fact]
    public void Compute_singleParentOnALowPay_takesTheTaxAndTheCreditOfTheHousehold()
    {
        var pay = Compute(20_000m, SingleParentOfTwo);

        var tax = TaxOn(pay, SingleParentOfTwo);
        (pay.IncomeTax, pay.MunicipalSurcharge, pay.LowEarnedIncomeCredit, pay.ChildTaxCredit).ShouldBe((tax.IncomeTax, tax.MunicipalSurcharge, tax.LowEarnedIncomeCredit, 1_100m));
        (pay.Supplements, pay.SupplementsTo).ShouldBe((tax.Supplements, Taxpayer.Director));
    }

    // The credit is refunded, so it all reaches the pocket, after the taxes.
    [Theory]
    [InlineData(20_000)]
    [InlineData(60_000)]
    public void Compute_singleParent_pocketsTheCredit(int grossPay)
    {
        var pay = Compute(grossPay, SingleParentOfTwo);

        pay.NetInPocket.ShouldBe(pay.GrossPay - pay.SocialContributions - pay.IncomeTax - pay.MunicipalSurcharge + pay.LowEarnedIncomeCredit + pay.ChildTaxCredit
            - pay.AdvancePayments.Surcharge + pay.AdvancePayments.Bonus);
        pay.NetInPocket.ShouldBeGreaterThan(Compute(grossPay, Household.None).NetInPocket);
    }

    // The assessment's final tax takes the credit off, and what it leaves beyond the tax is refunded.
    [Fact]
    public void Compute_singleParent_takesTheCreditOffTheAssessment()
    {
        var pay = Compute(20_000m, SingleParentOfTwo, new TaxPrepayments(500m, AdvancePayments.None));

        var finalTax = pay.IncomeTax + pay.MunicipalSurcharge - pay.LowEarnedIncomeCredit - pay.ChildTaxCredit + pay.AdvancePayments.Surcharge - pay.AdvancePayments.Bonus;
        (pay.Assessment.FinalTax, pay.Assessment.Balance).ShouldBe((finalTax, finalTax - 500m));
        pay.Assessment.Balance.ShouldBeLessThan(-1_100m);
    }

    // 24,000 leaves 19,418.62, just above the tax-free amount: its basic tax is 4,180 + 2,698.62 at 40% =
    // 5,259.45, and the 241.05 that 5,500.50 exceeds it by is the credit. The next 1,000 raises the basic tax
    // beyond 5,500.50, so it takes all of it away. Its parts still add up to it.
    [Fact]
    public void Compute_creditTheNextThousandTakesAway_isAPartOfTheNextThousand()
    {
        var pay = Compute(24_000m, SingleParentOfTwo);

        var next = pay.NextThousand;
        (pay.ChildTaxCredit, next.ChildTaxCredit).ShouldBe((241.05m, 241.05m));
        (next.SocialContributions + next.IncomeTax + next.MunicipalSurcharge + next.LowEarnedIncomeCredit + next.ChildTaxCredit + next.AdvancePayments + next.NetInPocket)
            .ShouldBe(1_000m);
    }

    // A home office's rent taxed with the pay absorbs the tax-free amount the credit stood on: its 828 raise
    // the taxable income to 20,246.62, whose basic tax of 5,590.65 exceeds 5,500.50, so it takes the 241.05
    // away. What it takes away of the credit is its own, as the credit for low earned income it takes away
    // is.
    [Fact]
    public void Compute_rentTakingTheCreditAway_countsTheCreditItLoses()
    {
        var pay = Compute(24_000m, SingleParentOfTwo, withPay: new TaxedWithPay { OtherIncome = 828m });

        var withRent = TaxOn(pay, SingleParentOfTwo, 828m);
        (pay.ChildTaxCredit, withRent.ChildTaxCredit, pay.OtherIncomeTax.ChildTaxCreditLost).ShouldBe((241.05m, 0m, 241.05m));
        pay.YearNetInPocket.ShouldBe(pay.NetInPocket - pay.OtherIncomeTax.Total);
        pay.OtherIncomeTax.Total.ShouldBe(pay.OtherIncomeTax.IncomeTax + pay.OtherIncomeTax.MunicipalSurcharge + pay.OtherIncomeTax.LowEarnedIncomeCreditLost
            + pay.OtherIncomeTax.ChildTaxCreditLost + pay.OtherIncomeTax.AdvancePayments);
    }

    // The credit is a step of its own, art. 134 §3 its rule and the cap per child its amount, money coming
    // back; the income tax cites the supplements it used, the scale of the tax on the tax-free amount they
    // climb, and the notice they come from.
    [Fact]
    public void Compute_children_stepsCiteTheCreditAndTheSupplements()
    {
        var pay = Compute(20_000m, SingleParentOfTwo);

        var credit = pay.Steps.Single(step => step.RuleKey == "child_tax_credit");
        (credit.Layer, credit.Event, credit.Track, credit.Amount, credit.Source).ShouldBe((Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, -1_100m, Rules.Supplements.CreditSource));
        credit.AmountsUsed.ShouldBe([Rules.Supplements.ChildCreditCapPerChild]);
        var incomeTax = pay.Steps.Single(step => step.RuleKey == "income_tax");
        incomeTax.AmountsUsed.ShouldBe([
            .. Rules.IncomeTaxAmounts, .. Rules.TaxFreeAmountAmounts.Except(Rules.IncomeTaxAmounts),
            Rules.Supplements.TwoChildren, Rules.Supplements.ChildUnder3, Rules.Supplements.SingleParent]);
        incomeTax.AlsoCites.ShouldBe([Rules.Supplements.Source]);
    }

    // Without children there is no credit to show: other dependants raise the tax-free amount only.
    [Fact]
    public void Compute_otherDependantsOnly_hasNoCreditStep()
    {
        var household = Household.None with { Dependants = new Dependants(OtherDependants: 1) };

        var pay = Compute(60_000m, household);

        pay.Steps.ShouldNotContain(step => step.RuleKey == "child_tax_credit");
        pay.Steps.Single(step => step.RuleKey == "income_tax").AmountsUsed.ShouldContain(Rules.Supplements.OtherDependant);
    }

    // A joint year with a partner who earns nothing: the river's share is the household's tax, the
    // dependants' saving and credit included, and the supplements go to the director.
    [Fact]
    public void Compute_jointWithChildren_takesTheHouseholdsCreditAndSaysWhoGetsTheSupplements()
    {
        var household = NonEarningPartner with { Dependants = new Dependants(Children: 3) };

        var pay = Compute(20_000m, household);

        var tax = TaxOn(pay, household);
        (pay.IncomeTax, pay.ChildTaxCredit, pay.SupplementsTo).ShouldBe((tax.IncomeTax, tax.ChildTaxCredit, Taxpayer.Director));
        pay.ChildTaxCredit.ShouldBeGreaterThan(0m);
        var steps = pay.Steps.Single(step => step.RuleKey == "income_tax");
        steps.AlsoCites.ShouldBe([Rules.MaritalQuotient.Source, Rules.TaxFreeAmountSharing, Rules.Supplements.Source]);
    }

    // A partner earning more takes the supplements.
    [Fact]
    public void Compute_jointWithAPartnerEarningMore_givesThePartnerTheSupplements()
    {
        var pay = Compute(20_000m, EarningPartner with { Dependants = new Dependants(Children: 2) });

        pay.SupplementsTo.ShouldBe(Taxpayer.Partner);
    }

    public static TheoryData<Dependants, string, RefusedInput> RefusedDependants => new()
    {
        { new Dependants(Children: -1), "children_negative", RefusedInput.Children },
        { new Dependants(Children: 1, ChildrenUnder3: -1), "children_under_3_out_of_range", RefusedInput.ChildrenUnder3 },
        { new Dependants(Children: 1, ChildrenUnder3: 2), "children_under_3_out_of_range", RefusedInput.ChildrenUnder3 },
        { new Dependants(DependantsAged66InCare: -1), "dependants_in_care_negative", RefusedInput.DependantsInCare },
        { new Dependants(OtherDependants: -1), "other_dependants_negative", RefusedInput.OtherDependants },
    };

    // Counts below zero, and more children under 3 than children, are refused on the field that holds them,
    // with or without a partner.
    [Theory]
    [MemberData(nameof(RefusedDependants))]
    public void Compute_dependantsOutOfRange_isRefused(Dependants dependants, string key, RefusedInput input)
    {
        foreach (var household in new[] { Household.None, NonEarningPartner })
        {
            var (value, error) = GrossToNet.Compute(2026, 60_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, household: household with { Dependants = dependants });

            (value, error?.Key, error?.Input).ShouldBe((null, key, input));
        }
    }
}
