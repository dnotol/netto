using Netto.Core.PersonalTax;

namespace Netto.Core.DirectorPay;

// The director's family situation as the monthly withholding takes it (annex III to the AR/CIR 92,
// n° 2, 4 and 5.1): which basic tax of its part 2, chapter 2 applies. AsSingle is section 1, n° 32: a
// person alone, a de facto cohabitant, a spouse or legal cohabitant whose partner has professional
// income of their own, or a couple the annex treats as single for the year (n° 4.2). The others are
// section 2, n° 34, the marital quotient: a spouse or legal cohabitant whose partner has no
// professional income, BothAged66 when both partners have reached 66 on the 1 January the rules
// name, which from 1 November 2026 raises the income imputed to the partner.
public enum WithholdingHousehold
{
    AsSingle,
    PartnerWithoutIncome,
    PartnerWithoutIncomeBothAged66,
}

public static class WithholdingHouseholds
{
    // The withholding's household from the one the user declares: the quotient for a couple taxed
    // jointly whose partner has no professional income, the single method otherwise. A year the couple
    // is taxed separately is one n° 4.2 b treats as single, but for the year of a partner's death, which
    // it does not list and which Netto takes as single too (decided 2026-10-28).
    public static WithholdingHousehold Of(Household household) => household switch
    {
        { TaxedJointly: true, PartnerIncome: 0m, BothAged66: true } => WithholdingHousehold.PartnerWithoutIncomeBothAged66,
        { TaxedJointly: true, PartnerIncome: 0m } => WithholdingHousehold.PartnerWithoutIncome,
        _ => WithholdingHousehold.AsSingle,
    };
}
