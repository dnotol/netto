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
