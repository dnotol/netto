using System.Globalization;
using System.Resources;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

public class OptimisationTabTextTests
{
    // The name IStringLocalizer<OptimisationTab> reads Tabs/OptimisationTab.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Tabs.OptimisationTab", typeof(OptimisationTab).Assembly);

    [Fact]
    public void OptimisationTabResx_everyState_hasAText()
    {
        Enum.GetValues<MovesState>().Select(OptimisationTabText.State)
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    [Fact]
    public void OptimisationTabResx_everyReason_hasAText()
    {
        Enum.GetValues<MoveReasonKind>().Select(OptimisationTabText.Why)
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    // Why each move helps, as the tab writes it under the move, with its amounts in euros (MoveReasonsTests).
    public static TheoryData<string, string[], string> Reasons => new()
    {
        { nameof(MoveReasonKind.Pay), ["€50,000.00", "€20,799.00", "€39,325.00", "€58,064.23"],
            "Your pay goes from €50,000.00 to €20,799.00, and the dividend paid out from €39,325.00 to €58,064.23." },
        { nameof(MoveReasonKind.PayWithRoyalty), ["€50,000.00", "€21,473.00", "€11,538.46", "€4,955.30", "€30,250.00", "€53,937.25"],
            "Your pay goes from €50,000.00 to €21,473.00, your copyright royalty from €11,538.46 to €4,955.30, and the dividend paid out from €30,250.00 to €53,937.25." },
        { nameof(MoveReasonKind.ReducedRateGivenUp), [], "With it, the company gives up the reduced rate of corporate tax." },
        { nameof(MoveReasonKind.ReducedRateGained), [], "With it, the company qualifies for the reduced rate of corporate tax." },
        { nameof(MoveReasonKind.CompanySurcharge), ["€675.00", "€200.00"], "The surcharge on the company's corporate tax goes from €675.00 to €200.00." },
        { nameof(MoveReasonKind.CompanySurchargeCancelled), ["€1,336.52"], "It cancels the surcharge of €1,336.52 on the company's corporate tax." },
        { nameof(MoveReasonKind.DirectorSurcharge), ["€486.57", "€120.00"], "The surcharge on your income tax goes from €486.57 to €120.00." },
        { nameof(MoveReasonKind.DirectorSurchargeCancelled), ["€486.57"], "It cancels the surcharge of €486.57 on your income tax." },
        { nameof(MoveReasonKind.Vouchers), ["€250.00", "€800.00"], "€250.00 more reaches you tax-free; the company deducts €800.00 of it." },
        { nameof(MoveReasonKind.VouchersNotDeducted), ["€250.00"], "€250.00 more reaches you tax-free; the company deducts none of it." },
        { nameof(MoveReasonKind.Rent), ["€0.00", "€1,380.00", "€0.00", "€1,025.62"],
            "The rent the company pays you goes from €0.00 to €1,380.00, and what reaches you of it after its tax from €0.00 to €1,025.62." },
        { nameof(MoveReasonKind.CopyrightFollowsPay), ["€0.00", "€11,538.46", "€0.00", "€925.94"],
            "Your copyright royalty goes from €0.00 to €11,538.46, and the tax on it apart from your pay from €0.00 to €925.94." },
    };

    [Theory]
    [MemberData(nameof(Reasons))]
    public void Why_reason_saysWhatChangesWithTheMove(string kind, string[] amounts, string expected)
    {
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(OptimisationTabText.Why(Enum.Parse<MoveReasonKind>(kind)), CultureInfo.InvariantCulture) ?? "", amounts);

        text.ShouldBe(expected);
    }

    // A link's name starts with what it shows, then says which move it is about.
    [Fact]
    public void LinkLabel_startsWithTheLinksTextThenNamesTheMove()
    {
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(OptimisationTabText.LinkLabel, CultureInfo.InvariantCulture) ?? "",
            "See it on The split", "Pay yourself €20,799.00 instead of €50,000.00");

        text.ShouldBe("See it on The split: Pay yourself €20,799.00 instead of €50,000.00");
    }

    // The example's move (MovesViewTests).
    [Fact]
    public void Move_pay_saysThePayFromAndTo()
    {
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(OptimisationTabText.Move(new PayMove(20_799m)), CultureInfo.InvariantCulture) ?? "", "€20,799.00", "€50,000.00");

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

        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(OptimisationTabText.Move(move), CultureInfo.InvariantCulture) ?? "",
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
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(OptimisationTabText.Move(move), CultureInfo.InvariantCulture) ?? "", amount);

        text.ShouldBe(expected);
    }

    // Letting the royalty follow the pay, with the share it reads, and the pay it may hold down so the
    // company pays both.
    [Fact]
    public void Move_royaltyFollowingThePay_saysTheShareAndThatThePayStaysWithinWhatTheCompanyPays()
    {
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(OptimisationTabText.Move(new CopyrightFollowsPayMove()), CultureInfo.InvariantCulture) ?? "",
            "75%");

        text.ShouldBe("Let your copyright royalty follow your pay, at what a ruling accepts for 75% of your time spent creating, your pay kept within what the company can pay beside it");
    }
}
