using Netto.Core.CompanyCar;
using Netto.Tests.WebClient.River;
using Shouldly;

namespace Netto.Tests.Rules;

// What the engine refuses is a typed value: a stable key, which the page words as Error_<key>, and the
// input it refuses, which says where the page shows it. The keys are the ones the engine always gave.
public class RefusalTests
{
    // One text per key: two refusals sharing a key would show one's words for the other.
    [Fact]
    public void Refusals_everyOneTheEngineDeclares_hasAKeyOfItsOwn()
    {
        var keys = EngineRefusals.All.Select(refusal => refusal.Key).ToList();

        keys.Count.ShouldBeGreaterThan(40);
        keys.ShouldBeUnique();
    }

    // So a refusal written into a string, a resource key or a test's name, reads as its key.
    [Fact]
    public void ToString_refusal_isItsKey()
    {
        var text = $"{CarCalculator.Co2Missing}";

        text.ShouldBe("car_co2_missing");
    }
}
