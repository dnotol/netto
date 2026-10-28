using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// A joint assessment with dependants, resident in Wallonia at a 7% municipal rate (CIR 92 art. 134 §4): each
// partner has the base tax-free amount, the supplements go to the one with the higher taxable income, after
// the marital quotient (2°), what the other cannot use passes to them in full (4°), and what they cannot use
// passes to the other up to what the other's income leaves of their own tax-free amount (3°), counted as the
// base amount and the other supplements first (al. 2). Each partner's credit is computed in their column, the
// cap of 550 per child for both together, shared in proportion to their credits (6°, al. 2, 2°). The
// derogation giving the supplements to the lower income when that lowers the couple's State tax (al. 3) is
// not modelled (PersonalTaxCalculator). Expected values for income 2026 are derived by hand from the official
// amounts (Tax-Calc does not cover it); the 2025 case from the notice for tax year 2026, for a Tax-Calc
// « exercice 2026 » check.
public class PersonalTaxCalculatorJointDependantsTests
{
    private static readonly PersonalTaxRules Rules2026 = PersonalTaxRules.IncomeYear2026;

    private static Household Partner(decimal income, Dependants dependants) =>
        new Household(HouseholdStatus.MarriedOrLegalCohabitant, income) with { Dependants = dependants };

    // 48,500 of the director's, 11,780 of it attributed to a partner who earns nothing: the director's 36,720
    // is higher, so the two children's 5,130 join their 11,550. Their tax-free amount's tax is 2,937.50 +
    // 4,930 at 30% = 4,416.50 against 2,887.50 without: 8,124.00 of State tax, 6,096.49 federal and 2,027.51
    // regional. The partner's column is as without children (PersonalTaxCalculatorJointTests).
    [Fact]
    public void Compute_2026DirectorHigher_givesTheDirectorTheSupplements()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Partner(0m, new Dependants(Children: 2)));

        var joint = result.Joint.ShouldNotBeNull();
        (joint.Director.TaxFreeAmount, joint.Director.TaxFreeAmountReduction, joint.Director.StateTax, joint.Director.ReducedStateTax,
                joint.Director.RegionalAdditionalTax, joint.Director.Supplements)
            .ShouldBe((16_680m, 4_416.50m, 8_124.00m, 6_096.49m, 2_027.51m, new TaxFreeAmountSupplements(5_130m, 0m, 0m, 0m, 0m)));
        (joint.Partner.TaxFreeAmount, joint.Partner.IncomeTax, joint.Partner.Supplements).ShouldBe((11_550m, 57.50m, TaxFreeAmountSupplements.None));
        (result.SupplementsTo, result.IncomeTax, result.MunicipalSurcharge, result.ChildTaxCredit).ShouldBe((Taxpayer.Director, 8_181.50m, 572.71m, 0m));
    }

    // 19,400 of the director's against a partner's 40,000, neither attributed anything: the partner's is
    // higher and takes the supplements.
    [Fact]
    public void Compute_2026PartnerHigher_givesThePartnerTheSupplements()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 20_000m, 0m, 0.07m, household: Partner(40_000m, new Dependants(Children: 2)));

        var joint = result.Joint.ShouldNotBeNull();
        (joint.Director.TaxFreeAmount, joint.Partner.TaxFreeAmount, result.SupplementsTo).ShouldBe((11_550m, 16_680m, Taxpayer.Partner));
        (joint.Director.Supplements, joint.Partner.Supplements.Total).ShouldBe((TaxFreeAmountSupplements.None, 5_130m));
    }

    // A single parent's supplement is for a taxpayer taxed alone: a joint return leaves it out.
    [Fact]
    public void Compute_2026SingleParentDeclaredInAJointReturn_addsNoSingleParentSupplement()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Partner(0m, new Dependants(Children: 2, SingleParent: true)));

        result.Joint.ShouldNotBeNull().Director.TaxFreeAmount.ShouldBe(16_680m);
    }

    // 14,550 of the director's (15,000 less 3%) against the partner's 13,000, no quotient either way: the
    // director takes three children's 11,440, 22,990 of tax-free amount. They cannot use 8,440 of it, which
    // passes to the partner up to the 1,450 their income leaves beyond their own 11,550, taken from the base
    // amount: 21,540 and 13,000. The director's children's layer sits on the 10,100 left of the base: 21,540 is
    // taxed 6,356.50, less the basic tax of 3,637.50, 2,719.00 unabsorbed, capped at 3 × 550. The partner's
    // 13,000 carries no child supplement and no credit.
    [Fact]
    public void Compute_2026SupplementsTheDirectorCannotUse_passToThePartnerUpToTheirIncome()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 15_000m, 0m, 0.07m, household: Partner(13_000m, new Dependants(Children: 3)));

        var joint = result.Joint.ShouldNotBeNull();
        (joint.Director.TaxableIncome, joint.Director.TaxFreeAmount, joint.Partner.TaxFreeAmount).ShouldBe((14_550.00m, 21_540.00m, 13_000.00m));
        (joint.Director.ChildTaxCredit, joint.Partner.ChildTaxCredit, result.ChildTaxCredit, result.IncomeTax).ShouldBe((1_650.00m, 0m, 1_650.00m, 0m));
    }

    // Both partners at 24,000 (24,742.27 less 742.27), five children: the director, at a tie, takes their
    // 25,580, 37,130 in all. They cannot use 13,130, which passes up to the partner's 12,450 beyond their own:
    // the 11,550 of base first, then 900 of the children's. The director's 24,680 is all children's, taxed
    // 7,612.50, 520.50 beyond the basic tax of 7,092.00; the partner's children's 900 sit on the 23,100 counted
    // first, 360.00 of tax, of which the 248.50 their basic tax cannot absorb is theirs. 769.00 in all, under
    // the cap of 2,750.
    [Fact]
    public void Compute_2026PassingBeyondTheBaseAmount_givesThePartnerACreditToo()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 24_742.27m, 0m, 0.07m, household: Partner(24_000m, new Dependants(Children: 5)));

        var joint = result.Joint.ShouldNotBeNull();
        (joint.Director.TaxFreeAmount, joint.Partner.TaxFreeAmount).ShouldBe((24_680.00m, 24_000.00m));
        (joint.Director.ChildTaxCredit, joint.Partner.ChildTaxCredit, result.ChildTaxCredit).ShouldBe((520.50m, 248.50m, 769.00m));
    }

    // Without dependants nothing moves: the columns are the joint assessment's as before.
    [Fact]
    public void Compute_2026NoDependants_leavesTheJointColumnsAsTheyWere()
    {
        var household = new Household(HouseholdStatus.MarriedOrLegalCohabitant, 13_000m);

        var result = PersonalTaxCalculator.Compute(Rules2026, 15_000m, 0m, 0.07m, household: household);

        (result.Joint.ShouldNotBeNull().Director.TaxFreeAmount, result.Joint.Partner.TaxFreeAmount, result.ChildTaxCredit).ShouldBe((11_550m, 11_550m, 0m));
    }

    // The cap above both partners' credits is shared in proportion to them, rounded to the cent on the
    // director's side, the partner's the rest.
    [Theory]
    [InlineData(600, 400, 500, 300, 200)]
    [InlineData(520.50, 248.50, 550, 372.27, 177.73)]
    [InlineData(520.50, 248.50, 2_750, 520.50, 248.50)]
    [InlineData(0, 0, 0, 0, 0)]
    public void ShareCap_creditsAboveIt_sharesItInProportion(decimal director, decimal partner, decimal cap, decimal expectedDirector, decimal expectedPartner)
    {
        PersonalTaxCalculator.ShareCap(director, partner, cap).ShouldBe((expectedDirector, expectedPartner));
    }

    // For a Tax-Calc « exercice 2026 » check (income 2025), derived from the notice for tax year 2026: a couple
    // (1002), two children (1030); the director's 1400 15,463.92 (less 463.92, 15,000.00), the partner's own
    // net professional income 14,000.00 (as 14,432.99 of pay less 432.99), no quotient. The director takes the
    // children's 5,110, 16,020, and passes the 1,020 they cannot use to the partner, whose 14,000 leaves 3,090
    // beyond their 10,910: 15,000 and 11,930. The director's 15,000 is taxed 3,927.00 on §2's scale, 177.00
    // beyond the basic tax of 3,750.00: the credit. The partner's 3,500.00 less 3,006.00 is 494.00 of State tax,
    // 370.71 federal and 123.29 regional, 34.58 of surcharge. The credit for low earned income is the
    // director's 880 (Tax-Calc may grant the partner one too, which Netto does not count).
    [Fact]
    public void Compute_2025SupplementsPassedOn_leaveTheDirectorACreditAndThePartnerATax()
    {
        var result = PersonalTaxCalculator.Compute(PersonalTaxRules.IncomeYear2025, 15_463.92m, 0m, 0.07m, household: Partner(14_000m, new Dependants(Children: 2)));

        var joint = result.Joint.ShouldNotBeNull();
        (joint.Director.TaxableIncome, joint.Director.TaxFreeAmount, joint.Director.StateTax, joint.Director.ChildTaxCredit)
            .ShouldBe((15_000.00m, 15_000.00m, 0m, 177.00m));
        (joint.Partner.TaxFreeAmount, joint.Partner.StateTax, joint.Partner.ReducedStateTax, joint.Partner.RegionalAdditionalTax)
            .ShouldBe((11_930.00m, 494.00m, 370.71m, 123.29m));
        (result.IncomeTax, result.MunicipalSurcharge, result.ChildTaxCredit, result.LowEarnedIncomeCredit).ShouldBe((494.00m, 34.58m, 177.00m, 880m));
    }
}
