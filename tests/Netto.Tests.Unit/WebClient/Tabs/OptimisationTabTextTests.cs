using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

public class OptimisationTabTextTests
{
    // The name IStringLocalizer<OptimisationTab> reads Tabs/OptimisationTab.resx under.
    private static readonly ResxText Resources = ResxText.For<OptimisationTab>();

    // The three figures, the pocket after each move, Apply all and a link's name.
    [Fact]
    public void OptimisationTabResx_everyFixedKey_hasAText()
    {
        new[]
            {
                OptimisationTabText.FromInPocket, OptimisationTabText.PlanInPocket, OptimisationTabText.MoreThanFrom, OptimisationTabText.After,
                OptimisationTabText.ApplyAll, OptimisationTabText.LinkLabel,
            }
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    [Fact]
    public void OptimisationTabResx_everyState_hasAText()
    {
        Enum.GetValues<MovesState>().Select(OptimisationTabText.State)
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    // The figures name the plan on screen, the plan every move applied leaves, and the difference.
    [Fact]
    public void Figures_readYourPlanOptimisedAndTheCertainGain()
    {
        new[] { OptimisationTabText.FromInPocket, OptimisationTabText.PlanInPocket, OptimisationTabText.MoreThanFrom }.Select(Resources.Get)
            .ShouldBe(["Your plan", "Optimised", "Certain gain"]);
    }

    // The line that opens the list says what the moves are measured from and what they touch, and that they
    // come one on top of the other: with no introduction above, it is where the viewer reads it.
    [Fact]
    public void State_moves_saysTheyAreMeasuredFromThePlanOnScreenAndRetuneOnlyItsLevers()
    {
        Resources.Get(OptimisationTabText.State(MovesState.Moves)).ShouldBe(
            "Measured from the plan on screen. Netto only retunes the levers already in your plan, with the facts you gave; each move on top of the ones above it:");
    }

    // Every state speaks of the plan on screen: none names Today, nor asks to save the plan.
    [Fact]
    public void State_everyState_namesNeitherTodayNorASave()
    {
        Enum.GetValues<MovesState>().Select(state => Resources.Get(OptimisationTabText.State(state))!)
            .Where(text => text.Contains("Today", StringComparison.Ordinal) || text.Contains("save", StringComparison.OrdinalIgnoreCase))
            .ShouldBeEmpty();
    }

    [Fact]
    public void State_searching_looksForTheMovesFromThePlanOnScreen()
    {
        Resources.Get(OptimisationTabText.State(MovesState.Searching)).ShouldBe("Looking for the moves from the plan on screen…");
    }

    [Fact]
    public void ApplyAll_readsApplyAll()
    {
        Resources.Get(OptimisationTabText.ApplyAll).ShouldBe("Apply all");
    }

    [Fact]
    public void OptimisationTabResx_everyReason_hasAText()
    {
        Enum.GetValues<MoveReasonKind>().Select(kind => OptimisationTabText.Why(kind, MoveReasonRest.None))
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    // A sentence that says its figure and the gain beside it has a wording for a rest below the figure and one for a
    // rest above it, in both languages; the others have none, so the key of a rest they never carry is no text.
    [Fact]
    public void OptimisationTabResx_everyBridgedReason_hasItsRestLessAndRestMoreTextsInBothLanguages()
    {
        var keys = Enum.GetValues<MoveReasonKind>().Where(MoveReasons.IsBridged)
            .SelectMany(kind => new[] { MoveReasonRest.Less, MoveReasonRest.More }.Select(rest => OptimisationTabText.Why(kind, rest))).ToList();

        (keys.Count, string.Join(", ", keys.Where(key => Resources.Get(key) is null || French.Get(key) is null)))
            .ShouldBe((10, ""));
    }

    private static readonly ResxText French = ResxText.French<OptimisationTab>();

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

    // The sentences that say their figure and the gain beside it, a rest below (the gain less than the figure) or
    // above it, in English and French (the amounts as the page formats them).
    public static TheoryData<string, string, string[], string, string> BridgedReasons => new()
    {
        { nameof(MoveReasonKind.VouchersNotDeducted), nameof(MoveReasonRest.Less), ["€100.00", "€82.00", "€18.00"],
            "€100.00 more reaches you tax-free, €82.00 less from the rest: €18.00 more in your pocket. The company deducts none of the vouchers.",
            "Vous recevez 100,00 € de plus sans impôt, 82,00 € de moins du reste : 18,00 € de plus dans votre poche. La société ne déduit aucun de ces chèques." },
        { nameof(MoveReasonKind.VouchersNotDeducted), nameof(MoveReasonRest.More), ["€100.00", "€5.00", "€105.00"],
            "€100.00 more reaches you tax-free, €5.00 more from the rest: €105.00 more in your pocket. The company deducts none of the vouchers.",
            "Vous recevez 100,00 € de plus sans impôt, 5,00 € de plus du reste : 105,00 € de plus dans votre poche. La société ne déduit aucun de ces chèques." },
        { nameof(MoveReasonKind.Vouchers), nameof(MoveReasonRest.Less), ["€250.00", "€800.00", "€200.00", "€50.00"],
            "€250.00 more reaches you tax-free, €200.00 less from the rest: €50.00 more in your pocket. The company deducts €800.00 of the vouchers.",
            "Vous recevez 250,00 € de plus sans impôt, 200,00 € de moins du reste : 50,00 € de plus dans votre poche. La société en déduit 800,00 €." },
        { nameof(MoveReasonKind.Rent), nameof(MoveReasonRest.Less), ["€1,858.01", "€2,136.71", "€1,858.01", "€2,136.71", "€246.00", "€32.70"],
            "The rent the company pays you goes from €1,858.01 to €2,136.71, and what reaches you of it after its tax from €1,858.01 to €2,136.71, €246.00 less from the rest: €32.70 more in your pocket.",
            "Le loyer que la société vous paie passe de 1.858,01 € à 2.136,71 €, et ce qui vous en revient après impôt de 1.858,01 € à 2.136,71 €, 246,00 € de moins du reste : 32,70 € de plus dans votre poche." },
        { nameof(MoveReasonKind.CompanySurchargeCancelled), nameof(MoveReasonRest.Less), ["€564.47", "€101.60", "€462.87"],
            "It cancels the surcharge of €564.47 on the company's corporate tax, €101.60 less once the withholding on the dividend is paid: €462.87 more in your pocket.",
            "Cet ajustement supprime la majoration de 564,47 € sur l'impôt des sociétés, 101,60 € de moins une fois le précompte mobilier sur le dividende payé : 462,87 € de plus dans votre poche." },
        { nameof(MoveReasonKind.CompanySurcharge), nameof(MoveReasonRest.Less), ["€675.00", "€200.00", "€100.00", "€375.00"],
            "The surcharge on the company's corporate tax goes from €675.00 to €200.00, €100.00 less once the withholding on the dividend is paid: €375.00 more in your pocket.",
            "La majoration sur l'impôt des sociétés passe de 675,00 € à 200,00 €, 100,00 € de moins une fois le précompte mobilier sur le dividende payé : 375,00 € de plus dans votre poche." },
    };

    [Theory]
    [MemberData(nameof(BridgedReasons))]
    public void Why_bridgedReason_saysItsFigureTheRestAndTheGainBesideIt(string kind, string rest, string[] amounts, string english, string french)
    {
        var key = OptimisationTabText.Why(Enum.Parse<MoveReasonKind>(kind), Enum.Parse<MoveReasonRest>(rest));

        using var inFrench = InLanguage.French();
        var frenchAmounts = amounts.Select(amount => (object)Formats.Euros(decimal.Parse(
            amount.Replace("€", "", StringComparison.Ordinal), System.Globalization.CultureInfo.InvariantCulture))).ToArray();

        (Resources.Format(key, amounts), French.Format(key, frenchAmounts)).ShouldBe((english, french));
    }

    [Theory]
    [MemberData(nameof(Reasons))]
    public void Why_reason_saysWhatChangesWithTheMove(string kind, string[] amounts, string expected)
    {
        var text = Resources.Format(OptimisationTabText.Why(Enum.Parse<MoveReasonKind>(kind), MoveReasonRest.None), amounts);

        text.ShouldBe(expected);
    }

    // A link's name starts with what it shows, then says which move it is about.
    [Fact]
    public void LinkLabel_startsWithTheLinksTextThenNamesTheMove()
    {
        var text = Resources.Format(OptimisationTabText.LinkLabel,
            "See it on The split", "Pay yourself €20,799.00 instead of €50,000.00");

        text.ShouldBe("See it on The split: Pay yourself €20,799.00 instead of €50,000.00");
    }

    // The example's move (MovesViewTests).
    [Fact]
    public void Move_pay_saysThePayFromAndTo()
    {
        var text = Resources.Format(OptimisationTabText.Move(new PayMove(20_799m)), "€20,799.00", "€50,000.00");

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

        var text = Resources.Format(OptimisationTabText.Move(move),
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
        var text = Resources.Format(OptimisationTabText.Move(move), amount);

        text.ShouldBe(expected);
    }

    // Letting the royalty follow the pay, with the share it reads, and the pay it may hold down so the
    // company pays both.
    [Fact]
    public void Move_royaltyFollowingThePay_saysTheShareAndThatThePayStaysWithinWhatTheCompanyPays()
    {
        var text = Resources.Format(OptimisationTabText.Move(new CopyrightFollowsPayMove()),
            "75%");

        text.ShouldBe("Let your copyright royalty follow your pay, at what a ruling accepts for 75% of your time spent creating, your pay kept within what the company can pay beside it");
    }
}
