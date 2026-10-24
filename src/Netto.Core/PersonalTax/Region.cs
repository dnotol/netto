namespace Netto.Core.PersonalTax;

// The region the director lives in on 1 January of the tax year, which the regional additional tax and
// the municipality list hang off (special law of 16.01.1989, art. 5/1). Netto models a resident of
// Wallonia: the other regions have no rules yet, so a rule set asked for one is none, never a guess.
public enum Region
{
    Wallonia,

    Flanders,

    Brussels,
}
