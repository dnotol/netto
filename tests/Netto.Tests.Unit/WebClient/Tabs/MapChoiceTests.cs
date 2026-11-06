using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the map's choice is: a module or a bucket, never both, named as a link to the plan names it, a module by
// its card's fold name, a bucket by its key (plan?card=<name>).
public class MapChoiceTests
{
    [Fact]
    public void Of_aModule_isThatModuleNamedByItsFold()
    {
        var choice = MapChoice.Of(RiverModule.DirectorPay);

        (choice.Module, choice.Bucket, choice.Name, choice.End).ShouldBe((RiverModule.DirectorPay, (string?)null, "director-pay", MapEnd.Of(RiverModule.DirectorPay)));
    }

    [Theory]
    [InlineData("pocket")]
    [InlineData("taxes_on_pay")]
    [InlineData("taxes_on_profit")]
    [InlineData("kept_in_company")]
    [InlineData("expenses")]
    public void InBucket_aBucketOfTheBar_isThatBucketNamedByItsKey(string key)
    {
        var choice = MapChoice.InBucket(key);

        (choice.Module, choice.Bucket, choice.Name, choice.End).ShouldBe(((RiverModule?)null, key, key, MapEnd.InBucket(key)));
    }

    [Fact]
    public void InBucket_aKeyTheBarHasNot_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapChoice.InBucket("savings"));
    }

    // Two choices of the same module or bucket are the same choice, whatever made them.
    [Fact]
    public void Equals_theSameModuleOrBucket_isTheSameChoice()
    {
        (MapChoice.Of(RiverModule.Car) == MapChoice.Of(RiverModule.Car), MapChoice.InBucket("pocket") == MapChoice.InBucket("pocket"),
            MapChoice.Of(RiverModule.Car) == MapChoice.InBucket("pocket")).ShouldBe((true, true, false));
    }

    // A link names a module while the map draws it, as it always has.
    [Fact]
    public void Named_aModuleOnTheMap_isIt()
    {
        MapChoice.Named("split", MapView.ModulesOf(RiverInputs.Example)).ShouldBe(MapChoice.Of(RiverModule.Split));
    }

    [Fact]
    public void Named_aModuleOffTheMap_isNone()
    {
        MapChoice.Named("car", MapView.ModulesOf(RiverInputs.Example)).ShouldBeNull();
    }

    // The five buckets are on the map whatever the plan holds, so a link to one always arrives.
    [Theory]
    [InlineData("pocket")]
    [InlineData("expenses")]
    public void Named_aBucket_isItWhateverThePlan(string key)
    {
        MapChoice.Named(key, MapView.ModulesOf(RiverInputs.Example.SkipCompany())).ShouldBe(MapChoice.InBucket(key));
    }

    [Fact]
    public void Named_aNameNothingHas_isNone()
    {
        MapChoice.Named("nowhere", MapView.ModulesOf(RiverInputs.Example)).ShouldBeNull();
    }

    // A link's name says one thing: no bucket's key is a module's fold name.
    [Fact]
    public void Name_ofEveryModuleAndBucket_isItsOwn()
    {
        var names = MapModules.Order.Select(module => MapChoice.Of(module).Name).Concat(BucketsView.Keys.Select(key => MapChoice.InBucket(key).Name)).ToList();

        names.ShouldBeUnique();
    }
}
