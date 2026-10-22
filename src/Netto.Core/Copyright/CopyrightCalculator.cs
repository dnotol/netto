using Netto.Core.PersonalTax;

namespace Netto.Core.Copyright;

public static class CopyrightCalculator
{
    public const string RoyaltyNegative = "copyright_royalty_negative";

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
    // royalty as paid at once under the new one.
    public static (CopyrightResult? Value, string? Error) Compute(CopyrightRules rules, CopyrightRoyalty royalty, decimal grossPay)
    {
        if (royalty.Royalty < 0m)
            return (null, RoyaltyNegative);
        if (!royalty.Any)
            return (CopyrightResult.None, null);

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
            within > 0m ? new IncomeTaxedApart(net, rules.TaxedApartRate) : null), null);
    }

    // The first band's rate on the income up to its ceiling, the second's on the part from there up to
    // its own, nothing beyond.
    private static decimal FlatRateCosts(CopyrightFlatRateCosts costs, decimal within)
    {
        var first = Math.Min(within, costs.FirstBandCeiling.Value);
        var second = Math.Clamp(within - costs.FirstBandCeiling.Value, 0m, costs.SecondBandCeiling.Value - costs.FirstBandCeiling.Value);
        return Money.RoundToCent(first * costs.FirstBandRate.Value) + Money.RoundToCent(second * costs.SecondBandRate.Value);
    }
}
