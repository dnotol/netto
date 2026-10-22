using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// A benefit in kind is director pay (art. 31 al. 2, 2° and art. 32 CIR 92): in the taxable pay, in
// INASTI's base (royal decree n° 38, art. 11 §2) and in the pay the key formula withholds on (annex
// III, n° 14.1), but never paid in cash. So the pay with a benefit is taxed exactly as a cash pay of
// the gross plus the benefit, and leaves the benefit less in the pocket. Income year 2026 at a 7%
// municipal rate, a made-up benefit of 2,400.
public class GrossToNetBenefitInKindTests
{
    private const decimal Benefit = 2_400m;

    private static readonly TaxPrepayments Withheld = new(1_000_000m, AdvancePayments.None);

    public static TheoryData<decimal, ContributionsPaidBy> Pays => new()
    {
        { 0m, ContributionsPaidBy.Director },
        { 10_000m, ContributionsPaidBy.Director },
        { 30_000m, ContributionsPaidBy.Director },
        { 50_000m, ContributionsPaidBy.Director },
        { 160_000m, ContributionsPaidBy.Director },
        { 0m, ContributionsPaidBy.Company },
        { 20_000m, ContributionsPaidBy.Company },
        { 50_000m, ContributionsPaidBy.Company },
        { 160_000m, ContributionsPaidBy.Company },
    };

    [Theory]
    [MemberData(nameof(Pays))]
    public void Compute_withABenefit_taxesItAsCashPayOfTheSameAmount(decimal grossPay, ContributionsPaidBy paidBy)
    {
        var withBenefit = Compute(grossPay, Benefit, paidBy, Withheld);
        var asCash = Compute(grossPay + Benefit, 0m, paidBy, Withheld);

        (withBenefit.SocialContributions, withBenefit.TaxablePay, withBenefit.FlatRateCosts, withBenefit.TaxableIncome, withBenefit.IncomeTax,
                withBenefit.MunicipalSurcharge, withBenefit.LowEarnedIncomeCredit)
            .ShouldBe((asCash.SocialContributions, asCash.TaxablePay, asCash.FlatRateCosts, asCash.TaxableIncome, asCash.IncomeTax,
                asCash.MunicipalSurcharge, asCash.LowEarnedIncomeCredit));
    }

    [Theory]
    [MemberData(nameof(Pays))]
    public void Compute_withABenefit_leavesItOutOfTheCashInPocket(decimal grossPay, ContributionsPaidBy paidBy)
    {
        var withBenefit = Compute(grossPay, Benefit, paidBy, Withheld);
        var asCash = Compute(grossPay + Benefit, 0m, paidBy, Withheld);

        withBenefit.NetInPocket.ShouldBe(asCash.NetInPocket - Benefit);
    }

    // The company pays the car's costs, not the benefit: the pay costs it the gross and the
    // contributions it pays for the director.
    [Theory]
    [MemberData(nameof(Pays))]
    public void Compute_withABenefit_costsTheCompanyTheCashPayAlone(decimal grossPay, ContributionsPaidBy paidBy)
    {
        var withBenefit = Compute(grossPay, Benefit, paidBy, Withheld);
        var asCash = Compute(grossPay + Benefit, 0m, paidBy, Withheld);

        (withBenefit.GrossPay, withBenefit.BenefitInKind, withBenefit.CompanyCost).ShouldBe((grossPay, Benefit, asCash.CompanyCost - Benefit));
    }

    // The key formula withholds on the month's taxable pay, the benefit's twelfth in it, out of the
    // cash paid: 50,000 + 2,400 a year is 4,366.67 a month, of which 4,166.67 is paid.
    [Fact]
    public void Compute_withABenefit_withholdsOnItOutOfTheCashPaid()
    {
        var withBenefit = Compute(50_000m, Benefit, ContributionsPaidBy.Director, TaxPrepayments.AsWithheld);
        var asCash = Compute(52_400m, 0m, ContributionsPaidBy.Director, TaxPrepayments.AsWithheld);
        var month = withBenefit.Statement.Months[0];

        (month.GrossPay, month.BenefitInKind, month.TaxablePay, month.Withholding, month.NetPaid)
            .ShouldBe((4_166.67m, 200m, 4_366.67m, asCash.Statement.Months[0].Withholding, 4_166.67m - asCash.Statement.Months[0].Withholding));
    }

    // What is left to pay or refunded on the assessment is the same as on the cash pay: the same tax,
    // the same withholding.
    [Fact]
    public void Compute_withABenefit_leavesTheSameBalanceOnTheAssessment()
    {
        var withBenefit = Compute(50_000m, Benefit, ContributionsPaidBy.Director, TaxPrepayments.AsWithheld);
        var asCash = Compute(52_400m, 0m, ContributionsPaidBy.Director, TaxPrepayments.AsWithheld);

        withBenefit.Assessment.ShouldBe(asCash.Assessment);
    }

    // The next 1,000 is gross paid in cash, on top of the same benefit.
    [Fact]
    public void Compute_withABenefit_sendsTheNextThousandWhereTheCashPaysWouldGo()
    {
        var withBenefit = Compute(50_000m, Benefit, ContributionsPaidBy.Director, Withheld);
        var asCash = Compute(52_400m, 0m, ContributionsPaidBy.Director, Withheld);

        withBenefit.NextThousand.ShouldBe(asCash.NextThousand);
    }

    // The surcharge on too little withheld or prepaid is on the tax of the pay with its benefit.
    [Fact]
    public void Compute_withABenefitAndNothingWithheld_surchargesTheTaxOnIt()
    {
        var withBenefit = Compute(50_000m, Benefit, ContributionsPaidBy.Director, TaxPrepayments.None);
        var asCash = Compute(52_400m, 0m, ContributionsPaidBy.Director, TaxPrepayments.None);

        (withBenefit.AdvancePayments.Surcharge, withBenefit.AdvancePayments.LastDeadlineTopUp, withBenefit.NetInPocket)
            .ShouldBe((asCash.AdvancePayments.Surcharge, asCash.AdvancePayments.LastDeadlineTopUp, asCash.NetInPocket - Benefit));
    }

    [Fact]
    public void Compute_withoutABenefit_recordsNone()
    {
        Compute(50_000m, 0m, ContributionsPaidBy.Director, Withheld).BenefitInKind.ShouldBe(0m);
    }

    // The engine's own rules give the benefit, never below zero.
    [Fact]
    public void Compute_negativeBenefit_isABug()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m), Withheld, ContributionsPaidBy.Director, 0m, -0.01m));
    }

    private static GrossToNetResult Compute(decimal grossPay, decimal benefit, ContributionsPaidBy paidBy, TaxPrepayments prepayments)
    {
        var (value, error) = GrossToNet.Compute(2026, grossPay, MunicipalRates.Entered(0.07m), prepayments, paidBy, 0m, benefit);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}
