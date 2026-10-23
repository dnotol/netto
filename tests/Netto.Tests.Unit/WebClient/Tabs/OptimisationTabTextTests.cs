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

    // The pocket with every move, the pocket after each move, a link's name and the note on a river
    // changed since its plan.
    [Fact]
    public void OptimisationTabResx_everyFixedKey_hasAText()
    {
        new[] { OptimisationTabText.PlanInPocket, OptimisationTabText.After, OptimisationTabText.LinkLabel, OptimisationTabText.RiverChanged }
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    // The plan the moves are measured from, which the tab's words follow: Today, the plan in the river,
    // and the plan in the river under its name.
    private static readonly MovesBaseline FromToday = MovesBaseline.Today(RiverInputs.Example);

    private static readonly MovesBaseline FromThePlanInTheRiver = new(RiverInputs.Example, IsToday: false);

    private static readonly MovesBaseline FromANamedPlan = new(RiverInputs.Example, IsToday: false, "Plan 2");

    public static TheoryData<string, MovesBaseline> Baselines => new()
    {
        { "Today", FromToday },
        { "the plan in the river", FromThePlanInTheRiver },
        { "a named plan", FromANamedPlan },
    };

    // Its head, the figures it opens on and the total, for each plan the moves are from.
    [Theory]
    [MemberData(nameof(Baselines))]
    public void OptimisationTabResx_everyKeyFromEachPlan_hasAText(string _, MovesBaseline from)
    {
        new[] { OptimisationTabText.Eyebrow(from), OptimisationTabText.Lede(from), OptimisationTabText.FromInPocket(from), OptimisationTabText.MoreThanFrom(from), OptimisationTabText.Total(from) }
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Baselines))]
    public void OptimisationTabResx_everyStateFromEachPlan_hasAText(string _, MovesBaseline from)
    {
        Enum.GetValues<MovesState>().Select(state => OptimisationTabText.State(state, from))
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null)
            .ShouldBeEmpty();
    }

    // Today's star is an icon beside the words, not a character in them.
    [Fact]
    public void FromInPocket_today_readsTodayInYourPocket()
    {
        Resources.GetString(OptimisationTabText.FromInPocket(FromToday), CultureInfo.InvariantCulture).ShouldBe("Today, in your pocket");
    }

    // While no plan is starred Today, the tab says its moves are from the plan in the river as it was opened
    // or saved, under its name where it has one: each text's key, what the tab fills its {0} with (the
    // plan's name, « Plan 2 » here, or the moves' total), and the text.
    public static TheoryData<string, string, string> FromThePlan => new()
    {
        { OptimisationTabText.Eyebrow(FromThePlanInTheRiver), "", "Against the plan in your river" },
        { OptimisationTabText.Eyebrow(FromANamedPlan), "Plan 2", "Against « Plan 2 », the plan in your river" },
        { OptimisationTabText.Lede(FromANamedPlan), "", "What Netto would change in the plan in your river, as you opened or saved it, to leave more in your pocket. "
            + "Each move adds what it says on top of the moves above it, so together they add up to the gain of the whole list. "
            + "What you change in the river afterwards is not counted: save it and star it as Today, your current setup, to measure the moves from it." },
        { OptimisationTabText.State(MovesState.Moves, FromThePlanInTheRiver), "", "From the plan in your river, as you opened or saved it:" },
        { OptimisationTabText.State(MovesState.Moves, FromANamedPlan), "Plan 2", "From « Plan 2 », as you opened or saved it:" },
        { OptimisationTabText.State(MovesState.AlreadyBest, FromANamedPlan), "Plan 2", "The plan in your river is already the best Netto finds." },
        { OptimisationTabText.State(MovesState.StartsAtPay, FromThePlanInTheRiver), "",
            "The plan in your river starts at your pay: enter its company's year in the river and save it to see the moves." },
        { OptimisationTabText.State(MovesState.Refused, FromThePlanInTheRiver), "",
            "An amount of the plan in your river is refused: correct it in the river and save it to see the moves." },
        { OptimisationTabText.FromInPocket(FromANamedPlan), "", "This plan, in your pocket" },
        { OptimisationTabText.MoreThanFrom(FromANamedPlan), "", "More than this plan" },
        { OptimisationTabText.Total(FromThePlanInTheRiver), "€1.00", "Together: €1.00 more than this plan." },
        { OptimisationTabText.RiverChanged, "", "The river has changed since you opened or saved this plan: save it to measure the moves from it." },
    };

    [Theory]
    [MemberData(nameof(FromThePlan))]
    public void Text_fromThePlanInTheRiver_saysItIsThatPlan(string key, string argument, string expected)
    {
        var text = string.Format(CultureInfo.InvariantCulture, Resources.GetString(key, CultureInfo.InvariantCulture) ?? "", argument);

        text.ShouldBe(expected);
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
