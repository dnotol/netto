using Netto.Core.PersonalTax;

namespace Netto.Core.Copyright;

public static class CopyrightCalculator
{
    public const string RoyaltyNegative = "copyright_royalty_negative";

    public const string CreativeShareOutOfRange = "copyright_creative_share_out_of_range";

    // A royalty the user fixes above what the company has before pay and royalty (YearSimulation).
    public const string RoyaltyAboveProfit = "copyright_royalty_above_profit";

    // The royalty as the rules take it beside the gross pay on the payslip, or the key of what they
    // refuse. The limits are on the gross royalty, as the 281.45 fiche reports the gross within and
    // beyond them, and 30% is measured on the royalty and the gross pay alone, without the benefits in
    // kind or the contributions the company pays on the director's behalf (decided unattended, the
    // smaller total, as the SDA's rulings for directors measure it), taken down to the cent so that it
    // never exceeds the law's, as the rent's cap is. A pay below nothing, which the pay's own rules
    // refuse, counts as none here. The flat-rate costs come off the part within the limits alone, the
    // limits staying on the gross: art. 37 measures the royalties, and art. 22 nets the movable income
    // they leave; each band is rounded to the cent, and the 15% withheld on the part within is on its
    // net (art. 269 takes the net income). The programme law of 30.05.2026 holds the withholding of a
    // royalty paid before 11.06.2026 to the old rule, on the net for every author: Netto takes the year's
    // royalty as paid at once under the new one. A royalty that follows the pay is taken at what it comes
    // to at that pay (AtPay), the amount given unread, and says how it was set (Following): none while
    // there is nothing a ruling accepts, which is never refused, so the river runs on until the share is
    // given.
    public static (CopyrightResult? Value, string? Error) Compute(CopyrightRules rules, CopyrightRoyalty royalty, decimal grossPay)
    {
        if (!royalty.FollowsPay && royalty.Royalty < 0m)
            return (null, RoyaltyNegative);
        if (royalty.CreativeShare is < 0m or > 1m)
            return (null, CreativeShareOutOfRange);

        var following = royalty.FollowsPay ? Following(rules, royalty) : (FollowingRoyalty?)null;
        royalty = AtPay(rules, royalty, grossPay);
        if (!royalty.Any)
            return (CopyrightResult.None with { Following = following }, null);

        var relativeLimit = Math.Floor(rules.RelativeLimit.Value * (royalty.Royalty + Math.Max(0m, grossPay)) * 100m) / 100m;
        var cap = rules.Cap.Value;
        var (within, bound) = royalty switch
        {
            { AverageWithinCap: false } => (0m, CopyrightBound.Average),
            _ when royalty.Royalty <= Math.Min(relativeLimit, cap) => (royalty.Royalty, CopyrightBound.None),
            _ when relativeLimit <= cap => (relativeLimit, CopyrightBound.Relative),
            _ => (cap, CopyrightBound.Cap),
        };
        var asPay = royalty.Royalty - within;
        var costs = royalty.HoldsAttestation || !rules.FlatRateCosts.NeedsAttestation ? FlatRateCosts(rules.FlatRateCosts, within) : 0m;
        var net = within - costs;

        return (new CopyrightResult(
            royalty.Royalty,
            royalty.AverageWithinCap,
            relativeLimit,
            cap,
            within,
            costs,
            asPay == 0m ? 0m : asPay,
            Money.RoundToCent(net * rules.WithholdingRate.Value),
            Money.RoundToCent(asPay * rules.BeyondLimitsWithholdingRate.Value),
            bound,
            within > 0m ? new IncomeTaxedApart(net, rules.TaxedApartRate) : null) { Following = following }, null);
    }

    // What an advance ruling accepts as a royalty beside the gross pay on the payslip, which Netto
    // proposes as a move and a royalty that follows the pay comes to: the envelope rate times the creative
    // share of the envelope, pay plus royalty, as ruling 2026.0484 measured it for one director, and what
    // keeps the whole royalty within the limits, the relative limit's share of that envelope and the cap.
    // A share r of the envelope is r / (1 − r) of the pay, taken down to the cent so that the royalty is
    // never above it. The pay is the payslip's gross alone, as the relative limit's is (decided unattended,
    // the smaller envelope). None until the share is given, when the four previous years averaged above
    // the cap, which taxes every royalty as pay, or for a year without an envelope rate (Following).
    public static decimal? Ceiling(CopyrightRules rules, CopyrightRoyalty royalty, decimal grossPay)
    {
        if (royalty.CreativeShare is not { } share || !royalty.AverageWithinCap || rules.EnvelopeRate is not { } envelope)
            return null;

        var rate = Math.Min(envelope.Value * share, rules.RelativeLimit.Value);
        var largest = Math.Floor(rate * Math.Max(0m, grossPay) / (1m - rate) * 100m) / 100m;
        return Math.Min(largest, rules.Cap.Value);
    }

    // The royalty as the company pays it beside that gross pay: the one given, or, when it follows the
    // pay, what a ruling accepts there (Ceiling), none where a ruling accepts nothing.
    public static CopyrightRoyalty AtPay(CopyrightRules rules, CopyrightRoyalty royalty, decimal grossPay) =>
        royalty.FollowsPay ? royalty with { Royalty = Ceiling(rules, royalty, grossPay) ?? 0m } : royalty;

    // How a royalty that follows the pay is set, whatever the pay: at what a ruling accepts, or none, and
    // why. The four previous years first, since no share would change them, then the year's rules.
    public static FollowingRoyalty Following(CopyrightRules rules, CopyrightRoyalty royalty) => royalty switch
    {
        { AverageWithinCap: false } => FollowingRoyalty.AverageAboveCap,
        _ when rules.EnvelopeRate is null => FollowingRoyalty.NoEnvelopeRate,
        { CreativeShare: null } => FollowingRoyalty.NoCreativeShare,
        _ => FollowingRoyalty.AtCeiling,
    };

    // The first band's rate on the income up to its ceiling, the second's on the part from there up to
    // its own, nothing beyond.
    private static decimal FlatRateCosts(CopyrightFlatRateCosts costs, decimal within)
    {
        var first = Math.Min(within, costs.FirstBandCeiling.Value);
        var second = Math.Clamp(within - costs.FirstBandCeiling.Value, 0m, costs.SecondBandCeiling.Value - costs.FirstBandCeiling.Value);
        return Money.RoundToCent(first * costs.FirstBandRate.Value) + Money.RoundToCent(second * costs.SecondBandRate.Value);
    }
}
