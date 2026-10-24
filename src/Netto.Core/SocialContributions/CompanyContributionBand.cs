namespace Netto.Core.SocialContributions;

// Which of INASTI's two yearly company contributions the company owes, as the user declares it, since Netto
// cannot see its accounts: by the balance-sheet total of its penultimate closed financial year as it stood on
// 1 January of the contribution year (law of 30.12.1992, art. 91), 2024's for 2026 with a calendar accounting
// year, at most the year's threshold (Lower) or above it (Upper). Saved plans store it by name: renaming a
// value changes their format.
public enum CompanyContributionBand
{
    Lower,
    Upper,
}
