using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A card's glance in the outline carries the icon of the bucket its money lands in, where it lands in
// one: what reaches the pocket, a car's costs in the muddy pocket. The rest is money still on its way
// (the revenue, the pay, what the company has left after its tax), a difference (the moves' gain) or a
// date, and lands in no bucket.
public class CardGlanceTests
{
    [Theory]
    [InlineData(GlanceKind.ToYou, "pocket")]
    [InlineData(GlanceKind.MuddyPocket, "muddy")]
    [InlineData(GlanceKind.In, null)]
    [InlineData(GlanceKind.ToYourPay, null)]
    [InlineData(GlanceKind.TaxedAsPay, null)]
    [InlineData(GlanceKind.LeftAfterTax, null)]
    [InlineData(GlanceKind.MoreThanToday, null)]
    [InlineData(GlanceKind.NextBy, null)]
    public void Bucket_eachKind_isWhereItsMoneyLands(GlanceKind kind, string? bucket)
    {
        new CardGlance(kind, "€1,000.00").Bucket.ShouldBe(bucket);
    }

    // Every kind is in the theory above, and every bucket a glance names has its icon.
    [Fact]
    public void Bucket_everyKind_isABucketOfTheBarOrNone()
    {
        Enum.GetValues<GlanceKind>()
            .Select(kind => new CardGlance(kind, "").Bucket)
            .OfType<string>()
            .ShouldAllBe(bucket => BucketsView.Keys.Contains(bucket) || bucket == BucketsView.Muddy);
    }
}
