using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What a copyright royalty does to the smallest pay that keeps the reduced corporate rate, as the split's
// card says it: that pay with the royalty as the plan holds it (With) and for the same year without it
// (Without), each none where no pay the split gives keeps the rate. The engine's own condition
// (RoyaltyLowersPayKeepingReducedRate), selected as it is: whether the royalty lowers the pay is read by
// setting the two beside each other. None while the plan has no such condition: no royalty, a rate the
// pay does not decide, or no year.
internal sealed record RoyaltyRateView(decimal? With, decimal? Without)
{
    public static RoyaltyRateView Of(RoyaltyLowersPayKeepingReducedRate condition) => new(condition.With, condition.Without);

    // The condition is worked out for the year it is read from, which runs the search for the smallest pay
    // keeping the rate twice: only the split's card, with a royalty in the plan, asks.
    public static RoyaltyRateView? From(YearResult? year) =>
        year is null ? null : YearConditions.Of(year).OfType<RoyaltyLowersPayKeepingReducedRate>().SingleOrDefault() is { } condition ? Of(condition) : null;
}
