using System.Globalization;
using System.Resources;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

public class MovesCardTextTests
{
    // The name IStringLocalizer<MovesCard> reads River/MovesCard.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.MovesCard", typeof(MovesCard).Assembly);

    [Fact]
    public void MovesCardResx_everyState_hasAText()
    {
        Enum.GetValues<MovesState>().Select(MovesCardText.State)
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    // The example's move (MovesViewTests).
    [Fact]
    public void Move_pay_saysThePayFromAndTo()
    {
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(MovesCardText.Move(new PayMove(20_799m)), CultureInfo.InvariantCulture) ?? "", "€20,799.00", "€50,000.00");

        text.ShouldBe("Pay yourself €20,799.00 instead of €50,000.00");
    }
}
