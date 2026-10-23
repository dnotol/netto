using Netto.Core.MovableIncome;
using Netto.Core.PersonalTax;

namespace Netto.Core.Copyright;

// The royalty as the rules take it. RelativeLimit is 30% of the royalty and the gross pay on the
// payslip, Cap the year's absolute cap; WithinLimits the part that stays movable income and is taxed
// apart, the smaller of the three, nothing when the four previous years averaged more than the cap
// (AverageWithinCap); FlatRateCosts what the year takes off that part before taxing it apart, nothing
// when it takes none; TaxedAsPay the rest, taxed as director pay, costs never coming off it. Withheld is
// what the company withholds on each part: 15% on the part within the limits less its costs, 30% on the
// part beyond them. Bound says which limit set the part within, none when the whole royalty is within
// them. Without a royalty, all nothing.
public sealed record CopyrightResult(
    decimal Royalty,
    bool AverageWithinCap,
    decimal RelativeLimit,
    decimal Cap,
    decimal WithinLimits,
    decimal FlatRateCosts,
    decimal TaxedAsPay,
    decimal WithheldWithinLimits,
    decimal WithheldBeyondLimits,
    CopyrightBound Bound,
    IncomeTaxedApart? TaxedApart)
{
    public static CopyrightResult None { get; } = new(0m, true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, CopyrightBound.None, null);

    public decimal Withheld => WithheldWithinLimits + WithheldBeyondLimits;

    // How a royalty that follows the pay was set: at what an advance ruling accepts at the pay, or none,
    // and why. Null for a royalty the user fixed.
    public CopyrightFollowing? Following { get; init; }

    // What the royalty does to the year, as YearSimulation composes it beside the other levers: what the
    // company pays and deducts, the part taxed apart, net of its costs, the part taxed as pay, and what
    // it withheld.
    public MovableIncomePaid MovableIncome => new(Royalty, TaxedApart, TaxedAsPay, Withheld);
}

// What set the part of the royalty within the limits.
public enum CopyrightBound
{
    // The whole royalty is within them.
    None,

    // 30% of the royalty and the pay.
    Relative,

    // The year's cap.
    Cap,

    // The four previous years averaged more than the cap: nothing is within them.
    Average,
}

// How a royalty that follows the pay is set: what an advance ruling accepts for the creative share at
// the pay, or none, and why, so that a card can say it without testing the rules itself.
public enum CopyrightFollowing
{
    // What an advance ruling accepts at the pay, all of it within the limits: nothing without pay or
    // without time spent creating.
    AtCeiling,

    // The share of the working time spent creating is not given yet.
    NoCreativeShare,

    // The four previous years averaged more than the cap, so every royalty would be taxed as pay: a
    // share would not change it.
    AverageAboveCap,

    // The income year has no envelope rate to measure what a ruling accepts.
    NoEnvelopeRate,
}
