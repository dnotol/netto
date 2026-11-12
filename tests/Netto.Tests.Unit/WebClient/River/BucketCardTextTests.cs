using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

public class BucketCardTextTests
{
    [Fact]
    public void BucketCardResx_everyKey_hasAnEnglishAndAFrenchText()
    {
        string[] keys = [BucketCardText.Nothing, BucketCardText.OnlyFrom];

        keys.Where(key => ResxText.For<BucketCard>().Get(key) is null).ToList().ShouldBeEmpty();
        keys.Where(key => ResxText.French<BucketCard>().Get(key) is null).ToList().ShouldBeEmpty();
    }
}
