namespace Netto.Core.PersonalTax;

// The director's household as the user declares it, for the income year. A married or legally
// cohabiting couple is taxed jointly (CIR 92 art. 126): one assessment in both names, each partner's
// net income still fixed apart, the marital quotient and the tax-free amounts shared between them.
// Except in a year the couple is taxed separately (TaxedSeparately): the year of the marriage or of the
// declaration of legal cohabitation, and the year of the divorce, of the end of the cohabitation, or
// from the year after a de facto separation, which the withholding treats alike (annex III, n° 4.2). A
// de facto cohabitant is taxed alone. PartnerIncome is the partner's net professional income, the
// user's to type, none unless given; BothAged66 whether both partners were 66 on 1 January of the tax
// year, which raises the quotient's cap. None is the director alone. Eligibility is the user's to
// declare, never the engine's to decide.
public sealed record Household(
    HouseholdStatus Status,
    decimal PartnerIncome = 0m,
    bool BothAged66 = false,
    bool TaxedSeparately = false)
{
    public static Household None { get; } = new(HouseholdStatus.Alone);

    public bool TaxedJointly => Status == HouseholdStatus.MarriedOrLegalCohabitant && !TaxedSeparately;
}

// Whether the director is taxed alone, with no spouse nor legal cohabitant (a de facto cohabitant has
// neither), or has one. (Alone, not Single: the analysers keep a type's name out of an identifier.)
public enum HouseholdStatus
{
    Alone,

    MarriedOrLegalCohabitant,
}
