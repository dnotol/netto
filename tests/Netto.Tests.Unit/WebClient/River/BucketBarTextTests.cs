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

    // Under the bar, what reaches the pocket out of the example's 100,000, nothing being kept
    // (BucketsViewTests), the share being the term its bubble explains.
    [Fact]
    public void Reach_example_saysWhatReachesThePocketAndWhatGoesToTaxes()
    {
        var view = BucketsView.From(RiverInputs.Example.Run()).ShouldNotBeNull();

        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(BucketBarText.Reach(view), CultureInfo.InvariantCulture) ?? "", "62%", "38%");

        text.ShouldBe("[62% reaches you], 38% goes to taxes.");
    }

    // What the company keeps is still the director's: the sentence says the share is of the rest.
    [Fact]
    public void Reach_reserve_saysItIsOfWhatTheCompanyDoesNotKeep()
    {
        var view = BucketsView.From(RiverInputs.Example.WithReserve(15_000m).Run()).ShouldNotBeNull();

        TermText.Split(Resources.GetString(BucketBarText.Reach(view), CultureInfo.InvariantCulture) ?? "")
            .ShouldBe(("Of what the company does not keep, ", "{0} reaches you", ", {1} goes to taxes."));
    }

    [Fact]
    public void BucketBarResx_reach_saysWhatItMeansAndWhereTheRestOfTheRiverIs()
    {
        new[] { BucketBarText.ReachMeaning, BucketBarText.EnterCompany }
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    // While the engine refuses an amount there is no figure to announce.
    [Fact]
    public void Pocket_noFigure_isNothing()
    {
        var pocket = BucketBarAnnouncement.Pocket(null);

        pocket.ShouldBeNull();
    }

    // The bar, and its live region once the river settles, say instead that there is no figure until
    // the amount the card marks is corrected.
    [Fact]
    public void BucketBarResx_noFigure_saysWhy()
    {
        var text = Resources.GetString(BucketBarText.Refused, CultureInfo.InvariantCulture);

        text.ShouldBe("No figures until the amount marked below is corrected.");
    }
}
