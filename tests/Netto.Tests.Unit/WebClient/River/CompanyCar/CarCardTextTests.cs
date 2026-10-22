using System.Globalization;
using System.Resources;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;

namespace Netto.Tests.WebClient.River.CompanyCar;

// The car card builds resource keys from what it compares. A key without a text in its .resx would
// show on screen as the key itself.
public class CarCardTextTests
{
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.CompanyCar.CarCard", typeof(CarCard).Assembly);

    // What the car takes in cash against the muddy pocket, each way it can fall.
    [Fact]
    public void CarCardResx_everyGap_hasAText()
    {
        var comparisons = new[] { new CarComparison(40_000m, 43_000m, 8_000m, 1_000m), new CarComparison(30_000m, 40_000m, 8_000m, 1_000m), new CarComparison(40_000m, 40_000m, 8_000m, 0m) };

        comparisons.Select(CarCardText.Gap).ShouldBe(["Gap_More", "Gap_Less", "Gap_NoCash"]);
        comparisons.Select(CarCardText.Gap).Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null).ShouldBeEmpty();
    }
}
