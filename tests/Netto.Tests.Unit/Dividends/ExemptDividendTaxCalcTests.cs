using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.Dividends;

// Tax-Calc « exercice 2026 » runs of 2026-10-19 (calculation version IPP_EX_2026 V_1.8.0): the made-up
// T4 and T1 cases of PersonalTaxCalculatorTests, plus a 40,000 VVPRbis dividend from the third
// distribution, whose withholding on the exempt 833 (15% for income 2025) goes in code 1437. Tax-Calc
// lists it under « Eléments remboursables », after the municipal surcharge, which it leaves alone.
// Its balance also holds lines Netto does not model, removed here: the advance-payment surcharge
// (746.12 in T4), the special social security contribution (459.98 in T4, its default « Oui ») and
// the low earned-income tax credit (880.00 in T1).
public class ExemptDividendTaxCalcTests
{
    private static readonly PersonalTaxRules Rules2025 = PersonalTaxRules.IncomeYear2025;

    // Tax-Calc's balance: 13,018.78 less the 746.12 advance-payment surcharge.
    [Fact]
    public void Balance_2025TaxCalcT4WithExemptVvprbisDividend_matchesTaxCalc()
    {
        var (incomeTax, surcharge, refund, balance) = Balance(50_000.00m, 8_300.00m);

        (incomeTax, surcharge, refund, balance).ShouldBe((11_586.55m, 811.06m, 124.95m, 12_272.66m));
    }

    // Tax-Calc's balance: 1,004.95 in the taxpayer's favour, of which 880.00 is the low earned-income
    // tax credit: the withholding comes back even with no tax to set it against.
    [Fact]
    public void Balance_2025TaxCalcT1NoTaxWithExemptVvprbisDividend_refundsTheWithholding()
    {
        var (incomeTax, surcharge, refund, balance) = Balance(14_000.00m, 3_500.00m);

        (incomeTax, surcharge, refund, balance).ShouldBe((0.00m, 0.00m, 124.95m, -124.95m));
    }

    private static (decimal IncomeTax, decimal Surcharge, decimal Refund, decimal Balance) Balance(decimal directorPay, decimal ownContributions)
    {
        var tax = PersonalTaxCalculator.Compute(Rules2025, directorPay, ownContributions, 0.07m);
        var dividend = DividendCalculator.Compute(DividendRules.IncomeYear2025, 40_000m, WithholdingCategory.VvprbisThirdYearOnwards);

        return (tax.IncomeTax, tax.MunicipalSurcharge, dividend.ExemptionRefund, tax.IncomeTax + tax.MunicipalSurcharge - dividend.ExemptionRefund);
    }
}
