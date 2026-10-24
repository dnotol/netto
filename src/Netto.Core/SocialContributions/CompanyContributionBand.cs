namespace Netto.Core.SocialContributions;

// Which of INASTI's yearly company contributions the company owes, as the user declares it, since Netto
// cannot see its accounts: by the balance-sheet total of its penultimate closed financial year as it stood on
// 1 January of the contribution year (law of 30.12.1992, art. 91), 2024's for 2026 with a calendar accounting
// year, at most the year's threshold (Lower) or above it (Upper); or none, for a company the law exempts
// (Exempt): a société de personnes in its first three years whose managers and most active partners were
// self-employed for at most 12 of the 40 quarters before it was formed (art. 94, 9°), or a company with no
// activity in a whole calendar year, on an attestation of the SPF Finances (art. 92bis). Saved plans store
// it by name: renaming a value changes their format.
public enum CompanyContributionBand
{
    Lower,
    Upper,
    Exempt,
}
