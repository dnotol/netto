namespace Netto.Core.PersonalTax;

// A joint assessment's two columns, as the administration's calculation detail lays them out: each
// partner is taxed on the scale on their own taxable income with their own tax-free amount (CIR 92 art.
// 134 §4), and each line is rounded per partner.
public sealed record JointAssessment(TaxpayerColumn Director, TaxpayerColumn Partner);

// One partner's lines. ProfessionalIncome is their own net professional income, before the marital
// quotient: the pay's for the director, what the user typed for the partner. Quotient is what the
// marital quotient attributes to them (art. 87 and 88), positive, or takes from them for the other,
// negative. OtherIncome is income of another category taxed with theirs, the rent of a home office,
// the director's. TaxableIncome is their income on the scale: their own, the quotient and the other
// income. TaxFreeAmount is their tax-free amount once what one of them cannot use has passed to the
// other (§4): the partner who keeps less keeps what their income uses. TaxedApart is the tax on an
// income taxed at its own rate, a royalty within its limits, the director's, which joins their State
// tax before the autonomy factor. The credit for low earned income is computed per taxpayer, on their
// own income before the quotient (art. 289ter §1, al. 4). Supplements are the dependants' supplements on the
// partner they go to, the one with the higher taxable income (§4, 2°), none on the other; ChildTaxCredit
// is the refundable credit for children computed in their column, within the cap both share (§4, 6°).
public sealed record TaxpayerColumn(
    decimal ProfessionalIncome,
    decimal Quotient,
    decimal OtherIncome,
    decimal TaxableIncome,
    decimal TaxFreeAmount,
    decimal BasicTax,
    decimal TaxFreeAmountReduction,
    decimal StateTax,
    decimal TaxedApart,
    decimal ReducedStateTax,
    decimal RegionalAdditionalTax,
    decimal IncomeTax,
    decimal MunicipalSurcharge,
    decimal LowEarnedIncomeCredit)
{
    public TaxFreeAmountSupplements Supplements { get; init; } = TaxFreeAmountSupplements.None;

    public decimal ChildTaxCredit { get; init; }
}

// A taxpayer of the household: the director, or their partner in a joint return.
public enum Taxpayer
{
    Director,

    Partner,
}
