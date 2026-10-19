using System.Globalization;
using System.Resources;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

public class BucketBarTextTests
{
    // The name IStringLocalizer<BucketBar> reads River/BucketBar.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.BucketBar", typeof(BucketBar).Assembly);

    [Fact]
    public void BucketBarResx_everyBucket_hasAText()
    {
        BucketsView.Keys.Select(BucketBarText.Bucket)
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }
}
