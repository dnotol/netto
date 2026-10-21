using System.Globalization;
using System.Resources;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

public class BucketBarTextTests
{
    // The name IStringLocalizer<BucketBar> reads River/BucketBar.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.BucketBar", typeof(BucketBar).Assembly);

    // Each bucket, with and without a rent among the taxes on pay.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BucketBarResx_everyBucket_hasAText(bool taxesOnPayIncludeRent)
    {
        BucketsView.Keys.Select(key => BucketBarText.Bucket(key, taxesOnPayIncludeRent))
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Bucket_taxesOnPayWithRent_hasItsOwnText()
    {
        (BucketBarText.Bucket("taxes_on_pay", true), BucketBarText.Bucket("pocket", true)).ShouldBe(("Bucket_taxes_on_pay_and_rent", "Bucket_pocket"));
    }

    // What each bucket holds, in the bubble its name opens, with and without a rent among the taxes on pay.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BucketBarResx_everyBucket_saysWhatItHolds(bool taxesOnPayIncludeRent)
    {
        BucketsView.Keys.Select(key => BucketBarText.Holds(key, taxesOnPayIncludeRent))
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Holds_taxesOnPayWithRent_saysTheRentToo()
    {
        (BucketBarText.Holds("taxes_on_pay", true), BucketBarText.Holds("pocket", true)).ShouldBe(("Holds_taxes_on_pay_and_rent", "Holds_pocket"));
    }

    // Once the river settles, the live region says what reaches the pocket, and that alone: the
    // example's 61,975.74 (BucketsViewTests).
    [Fact]
    public void PocketAnnounced_example_saysThePocketAlone()
    {
        var pocket = BucketBarAnnouncement.Pocket(BucketsView.From(RiverInputs.Example.Run()));

        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(BucketBarText.PocketAnnounced, CultureInfo.InvariantCulture) ?? "", pocket);

        text.ShouldBe("In your pocket: €61,975.74.");
    }

    // While the engine refuses an amount there is no figure: the region empties, and the card holding
    // the refused amount says why.
    [Fact]
    public void Pocket_noFigure_isNothing()
    {
        var pocket = BucketBarAnnouncement.Pocket(null);

        pocket.ShouldBeNull();
    }
}
