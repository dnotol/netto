using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// A joint assessment, resident in Wallonia at a 7% municipal rate, the director's pay in cadre XVI with
// its flat-rate costs. Tax-Calc does not cover income year 2026 yet (taxcalc2027 answered 403 on
// 2026-10-28), so every expected value is derived by hand from the official amounts: the marital
// quotient's 30% and caps (law of 15.07.2026 art. 27 and 30, indexation notice for tax year 2027,
// the cap counting the partner's own professional income as DOC 56 1243/001 reads it), the scale (art.
// 130), the tax-free amount of 11,550 per partner and the scale of the tax on it (art. 131 and 134 §2),
// the unused part passing to the other partner (art. 134 §4), the credit per taxpayer before the
// quotient (art. 289ter §1), the autonomy factor and Wallonia's additional rate, each partner's column
// rounded to the cent where a single person's lines are. The 2025 case is derived the same way from
// the amounts for tax year 2026, for a Tax-Calc « exercice 2026 » run to check the method against.
public class PersonalTaxCalculatorJointTests
{
    private static readonly PersonalTaxRules Rules2026 = PersonalTaxRules.IncomeYear2026;

    private static Household Partner(decimal income = 0m, bool bothAged66 = false) =>
        new(HouseholdStatus.MarriedOrLegalCohabitant, income, bothAged66);

    // 60,000 less 10,000 of contributions, less 3% of 50,000: 48,500 of net professional income. 30% of
    // it is 14,550, more than the cap: the partner is attributed 11,780, and taxed on it on the scale
    // with their own tax-free amount, which it exceeds, so nothing passes on.
    [Fact]
    public void Compute_2026OneEarnerAboveTheCap_attributesTheCapToThePartner()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Partner());

        result.Joint.ShouldBe(new JointAssessment(
            new TaxpayerColumn(
                ProfessionalIncome: 48_500.00m, Quotient: -11_780.00m, OtherIncome: 0m, TaxableIncome: 36_720.00m, TaxFreeAmount: 11_550m,
                BasicTax: 12_540.50m, TaxFreeAmountReduction: 2_887.50m, StateTax: 9_653.00m, TaxedApart: 0m,
                ReducedStateTax: 7_243.90m, RegionalAdditionalTax: 2_409.10m, IncomeTax: 9_653.00m, MunicipalSurcharge: 675.71m, LowEarnedIncomeCredit: 0m),
            new TaxpayerColumn(
                ProfessionalIncome: 0m, Quotient: 11_780.00m, OtherIncome: 0m, TaxableIncome: 11_780.00m, TaxFreeAmount: 11_550m,
                BasicTax: 2_945.00m, TaxFreeAmountReduction: 2_887.50m, StateTax: 57.50m, TaxedApart: 0m,
                ReducedStateTax: 43.15m, RegionalAdditionalTax: 14.35m, IncomeTax: 57.50m, MunicipalSurcharge: 4.03m, LowEarnedIncomeCredit: 0m)));
    }

    // The household's lines are the two columns' sums; the taxable income stays the director's own net
    // professional income, before the quotient, as the pay's steps show it.
    [Fact]
    public void Compute_2026OneEarnerAboveTheCap_sumsTheColumnsIntoTheHouseholdsLines()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Partner());

        (result with { Joint = null }).ShouldBe(new PersonalTaxResult(
            FlatRateCosts: 1_500.00m, TaxableIncome: 48_500.00m,
            BasicTax: 15_485.50m, TaxFreeAmountReduction: 5_775.00m, StateTax: 9_710.50m,
            ReducedStateTax: 7_287.05m, RegionalAdditionalTax: 2_423.45m, IncomeTax: 9_710.50m, MunicipalSurcharge: 679.74m,
            LowEarnedIncomeCredit: 0m));
    }

    // Both 66 on 1 January 2027: the cap is 12,790.
    [Fact]
    public void Compute_2026BothAged66_attributesTheHigherCap()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Partner(bothAged66: true));

        var partner = result.Joint.ShouldNotBeNull().Partner;
        (partner.Quotient, partner.StateTax, result.StateTax, result.IncomeTax, result.MunicipalSurcharge)
            .ShouldBe((12_790.00m, 310.00m, 9_508.50m, 9_508.50m, 665.60m));
    }

    // 31,000 less 3%: 30,070. The partner is attributed 30% of it, 9,021, below their tax-free amount of
    // 11,550: the 2,529 they cannot use passes to the director, whose 14,079 is taxed 25% up to 11,750
    // and 30% beyond (art. 134 §2 and §4).
    [Fact]
    public void Compute_2026PartnerBelowTheTaxFreeAmount_passesTheUnusedPartOnAtTheScalesRates()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 31_000m, 0m, 0.07m, household: Partner());

        var (director, partner) = (result.Joint.ShouldNotBeNull().Director, result.Joint.Partner);
        (director.TaxableIncome, director.TaxFreeAmount, director.BasicTax, director.TaxFreeAmountReduction, director.StateTax)
            .ShouldBe((21_049.00m, 14_079.00m, 5_911.60m, 3_636.20m, 2_275.40m));
        (partner.TaxableIncome, partner.TaxFreeAmount, partner.BasicTax, partner.TaxFreeAmountReduction, partner.StateTax)
            .ShouldBe((9_021.00m, 9_021.00m, 2_255.25m, 2_255.25m, 0m));
        (result.IncomeTax, result.MunicipalSurcharge).ShouldBe((2_275.40m, 159.28m));
    }

    // The partner's own 5,000 of professional income is under 30% of the 53,500 together. 30% is 16,050,
    // 11,050 more than their own, but the cap counts their own income too: 11,780 less 5,000, 6,780.
    [Fact]
    public void Compute_2026PartnerWithOwnIncomeUnder30Percent_topsThemUpWithinTheCapTheirOwnIncomeIncluded()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Partner(5_000m));

        var (director, partner) = (result.Joint.ShouldNotBeNull().Director, result.Joint.Partner);
        (director.Quotient, director.TaxableIncome, partner.ProfessionalIncome, partner.Quotient, partner.TaxableIncome)
            .ShouldBe((-6_780.00m, 41_720.00m, 5_000m, 6_780.00m, 11_780.00m));
        (director.StateTax, partner.StateTax, result.IncomeTax, result.MunicipalSurcharge).ShouldBe((11_903.00m, 57.50m, 11_960.51m, 837.24m));
    }

    // 20,000 of 50,070 is more than 30%: no quotient, and each partner's income exceeds their tax-free
    // amount, so each is taxed as alone.
    [Fact]
    public void Compute_2026PartnerAt30PercentOrMore_attributesNothing()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 31_000m, 0m, 0.07m, household: Partner(20_000m));

        var (director, partner) = (result.Joint.ShouldNotBeNull().Director, result.Joint.Partner);
        (director.Quotient, director.TaxFreeAmount, director.StateTax, partner.Quotient, partner.TaxFreeAmount, partner.StateTax)
            .ShouldBe((0m, 11_550m, 6_660.50m, 0m, 11_550m, 2_604.50m));
        (result.IncomeTax, result.MunicipalSurcharge).ShouldBe((9_264.99m, 648.55m));
    }

    // The director earns 1,940 and the partner 40,000: the director is the one topped up, 30% of 41,940
    // less their own 1,940 being 10,642, held to the cap less their own, 9,840.
    [Fact]
    public void Compute_2026PartnerEarnsMore_attributesTheQuotientToTheDirector()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 2_000m, 0m, 0.07m, household: Partner(40_000m));

        var (director, partner) = (result.Joint.ShouldNotBeNull().Director, result.Joint.Partner);
        (director.ProfessionalIncome, director.Quotient, director.TaxableIncome, partner.Quotient, partner.TaxableIncome)
            .ShouldBe((1_940.00m, 9_840.00m, 11_780.00m, -9_840.00m, 30_160.00m));
        (director.StateTax, partner.StateTax, result.IncomeTax, result.MunicipalSurcharge).ShouldBe((57.50m, 6_701.00m, 6_758.50m, 473.10m));
    }

    // Each column's surcharge rounds on its own: 9,653.00 and 57.50 at 7%, 675.71 and 4.025 rounded up.
    [Fact]
    public void Compute_2026OneEarnerAboveTheCap_roundsEachPartnersLinesOnTheirOwn()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Partner());

        var (director, partner) = (result.Joint.ShouldNotBeNull().Director, result.Joint.Partner);
        (director.MunicipalSurcharge, partner.MunicipalSurcharge, result.MunicipalSurcharge).ShouldBe((675.71m, 4.03m, 679.74m));
    }

    // The credit is the director's, on their own 28,033 before the quotient moves 8,409.90 of it to the
    // partner: 910 × (29,120 − 28,033) / 6,720, 147.20, where the 19,623.10 left would give the full 910
    // (art. 289ter §1, al. 4). The partner, attributed income only, has none.
    [Fact]
    public void Compute_2026CreditForLowEarnedIncome_isTheDirectorsOnTheirIncomeBeforeTheQuotient()
    {
        var result = PersonalTaxCalculator.Compute(Rules2026, 28_900m, 0m, 0.07m, household: Partner());

        var (director, partner) = (result.Joint.ShouldNotBeNull().Director, result.Joint.Partner);
        (director.Quotient, director.TaxFreeAmount, director.TaxFreeAmountReduction, director.StateTax).ShouldBe((-8_409.90m, 14_690.10m, 3_819.53m, 1_521.71m));
        (director.LowEarnedIncomeCredit, partner.LowEarnedIncomeCredit, result.LowEarnedIncomeCredit).ShouldBe((147.20m, 0m, 147.20m));
    }

    // The rent of a home office (828) and a royalty within its limits (10,000 at 15%) are the director's
    // and no professional income: the quotient is 30% of the pay's 30,070 alone, the rent joins the
    // director's income on the scale, the 1,500 taxed apart the director's State tax.
    [Fact]
    public void Compute_2026RentAndRoyaltyTaxedApart_stayInTheDirectorsColumnOutsideTheQuotient()
    {
        var result = PersonalTaxCalculator.Compute(
            Rules2026, 31_000m, 0m, 0.07m, 828m, new IncomeTaxedApart(10_000m, new Netto.Core.Rules.RuleAmount("copyright_rate", 0.15m, Rules2026.IncomeTaxScale)), Partner());

        var (director, partner) = (result.Joint.ShouldNotBeNull().Director, result.Joint.Partner);
        (partner.Quotient, director.OtherIncome, director.TaxableIncome, director.StateTax, director.TaxedApart, director.IncomeTax)
            .ShouldBe((9_021.00m, 828m, 21_877.00m, 2_606.60m, 1_500.00m, 4_106.61m));
        (result.TaxedApart, result.IncomeTax, result.MunicipalSurcharge).ShouldBe((1_500.00m, 4_106.61m, 287.46m));
    }

    // In the year a couple starts or ends, each is taxed alone: the director's tax is a single person's.
    [Fact]
    public void Compute_2026YearTaxedSeparately_isTheDirectorTaxedAlone()
    {
        var household = new Household(HouseholdStatus.MarriedOrLegalCohabitant, 5_000m, TaxedSeparately: true);

        var result = PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: household);

        result.ShouldBe(PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m));
    }

    // Income year 2025, for the Tax-Calc « exercice 2026 » check: the cap is 13,460, the tax-free amount
    // 10,910 each, the scale 25% to 16,320, 40% to 28,800, 45% to 49,840.
    [Fact]
    public void Compute_2025OneEarnerAboveTheCap_attributesThatYearsCap()
    {
        var result = PersonalTaxCalculator.Compute(PersonalTaxRules.IncomeYear2025, 60_000m, 10_000m, 0.07m, household: Partner());

        var (director, partner) = (result.Joint.ShouldNotBeNull().Director, result.Joint.Partner);
        (director.TaxableIncome, director.BasicTax, director.TaxFreeAmountReduction, director.StateTax, director.ReducedStateTax, director.RegionalAdditionalTax,
                director.MunicipalSurcharge)
            .ShouldBe((35_040.00m, 11_880.00m, 2_727.50m, 9_152.50m, 6_868.31m, 2_284.19m, 640.68m));
        (partner.Quotient, partner.BasicTax, partner.TaxFreeAmountReduction, partner.StateTax, partner.ReducedStateTax, partner.RegionalAdditionalTax,
                partner.MunicipalSurcharge)
            .ShouldBe((13_460.00m, 3_365.00m, 2_727.50m, 637.50m, 478.40m, 159.10m, 44.63m));
        (result.IncomeTax, result.MunicipalSurcharge).ShouldBe((9_790.00m, 685.31m));
    }

    // Without the quotient the partner's whole 11,550 passes to the director, taxed 25%, 30% and 40%:
    // 6,980.50 off 17,841.50, a State tax of 10,861.00, which the quotient brings down to 9,710.50.
    [Fact]
    public void WithoutQuotient_2026OneEarner_passesThePartnersWholeTaxFreeAmountOn()
    {
        var result = PersonalTaxCalculator.WithoutQuotient(Rules2026, 60_000m, 10_000m, 0.07m, 0m, null, Partner());

        var director = result.Joint.ShouldNotBeNull().Director;
        (director.Quotient, director.TaxFreeAmount, director.BasicTax, director.TaxFreeAmountReduction, result.StateTax, result.IncomeTax, result.MunicipalSurcharge)
            .ShouldBe((0m, 23_100m, 17_841.50m, 6_980.50m, 10_861.00m, 10_861.01m, 760.27m));
    }

    [Fact]
    public void Compute_negativePartnerIncome_throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PersonalTaxCalculator.Compute(Rules2026, 60_000m, 10_000m, 0.07m, household: Partner(-1m)));
    }
}
