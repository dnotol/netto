namespace Netto.Core.SocialContributions;

// Who pays the director's INASTI contributions, as the user declares it: the director personally, or
// the company on the director's behalf. What the company pays for the director is pay too (SPF return
// explanations, part 2, cadre XVI, 1), deducted as the director's own contributions (cadre XVI, 10;
// CIR 92 art. 52, 7°).
public enum ContributionsPaidBy
{
    Director,
    Company,
}
