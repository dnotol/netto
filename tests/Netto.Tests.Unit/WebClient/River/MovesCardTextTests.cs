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

    public static TheoryData<Move, string> Payments => new()
    {
        { new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 29_700.45m), "Pay €29,700.45 by 21 December 2026 on the company's corporate tax" },
        { new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 14_317.50m), "Pay €14,317.50 by 21 December 2026 on your income tax" },
    };

    [Theory]
    [MemberData(nameof(Payments))]
    public void Move_payment_saysTheAmountTheDateAndTheTax(Move move, string expected)
    {
        var (amount, date) = move switch
        {
            CompanyAdvancePaymentMove payment => (payment.Amount, payment.Date),
            DirectorAdvancePaymentMove payment => (payment.Amount, payment.Date),
            _ => throw new ArgumentOutOfRangeException(nameof(move)),
        };

        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(MovesCardText.Move(move), CultureInfo.InvariantCulture) ?? "",
            Netto.Web.Client.Formats.Euros(amount), Netto.Web.Client.Formats.Day(date));

        text.ShouldBe(expected);
    }

    public static TheoryData<Move, string, string> Levers => new()
    {
        { new EcoVouchersMove(250m), "€250.00", "Grant €250.00 of eco vouchers a year" },
        { new SportCultureVouchersMove(100m), "€100.00", "Grant €100.00 of sport and culture vouchers a year" },
        { new RentMove(1_380m), "€1,380.00", "Rent the office in your home to the company for €1,380.00 a year" },
    };

    [Theory]
    [MemberData(nameof(Levers))]
    public void Move_lever_saysWhatTheCompanyPays(Move move, string amount, string expected)
    {
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(MovesCardText.Move(move), CultureInfo.InvariantCulture) ?? "", amount);

        text.ShouldBe(expected);
    }
}
