using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

public class OptimisationTabTextTests
{
    // The name IStringLocalizer<OptimisationTab> reads Tabs/OptimisationTab.resx under.
    private static readonly ResxText Resources = ResxText.For<OptimisationTab>();

    // The pocket with every move, the pocket after each move and a link's name.
    [Fact]
    public void OptimisationTabResx_everyFixedKey_hasAText()
    {
        new[] { OptimisationTabText.PlanInPocket, OptimisationTabText.After, OptimisationTabText.LinkLabel }
            .Where(key => Resources.Get(key) is null)
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

    // The figures it opens on, for each plan the moves are from.
    [Theory]
    [MemberData(nameof(Baselines))]
    public void OptimisationTabResx_everyKeyFromEachPlan_hasAText(string _, MovesBaseline from)
    {
        new[] { OptimisationTabText.FromInPocket(from), OptimisationTabText.MoreThanFrom(from) }
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    // The note on a river that holds what the plan does not: one text for the plan in the river whatever it
    // holds, and from Today one for each number of cards it names, up to the one that says « and n more ».
    [Theory]
    [MemberData(nameof(Baselines))]
    public void OptimisationTabResx_riverChangedFromEachPlan_hasATextForEveryNumberOfCards(string _, MovesBaseline from)
    {
        Enumerable.Range(1, 6).Select(differences => OptimisationTabText.RiverChanged(from, differences))
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Baselines))]
    public void OptimisationTabResx_everyStateFromEachPlan_hasAText(string _, MovesBaseline from)
    {
        Enum.GetValues<MovesState>().Select(state => OptimisationTabText.State(state, from))
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    // The line that opens the list says the moves come one on top of the other: with no introduction above,
    // it is where the viewer reads it.
    [Fact]
    public void State_movesFromToday_saysEachMoveComesOnTopOfTheOnesAboveIt()
    {
        Resources.Get(OptimisationTabText.State(MovesState.Moves, FromToday)).ShouldBe("From Today, your current setup, each move on top of the ones above it:");
    }

    // Today's star is an icon beside the words, not a character in them.
    [Fact]
    public void FromInPocket_today_readsTodayInYourPocket()
    {
        Resources.Get(OptimisationTabText.FromInPocket(FromToday)).ShouldBe("Today, in your pocket");
    }

    // While no plan is starred Today, the tab says its moves are from the plan in the river as it was opened
    // or saved, under its name where it has one: each text's key, what the tab fills its {0} with (the
    // plan's name, « Plan 2 » here), and the text.
    public static TheoryData<string, string, string> FromThePlan => new()
    {
        { OptimisationTabText.State(MovesState.Moves, FromThePlanInTheRiver), "", "From the plan in your river, as you opened or saved it, each move on top of the ones above it:" },
        { OptimisationTabText.State(MovesState.Moves, FromANamedPlan), "Plan 2", "From « Plan 2 », as you opened or saved it, each move on top of the ones above it:" },
        { OptimisationTabText.State(MovesState.AlreadyBest, FromANamedPlan), "Plan 2", "The plan in your river is already the best Netto finds." },
        { OptimisationTabText.State(MovesState.StartsAtPay, FromThePlanInTheRiver), "",
            "The plan in your river starts at your pay: enter its company's year in the river and save it to see the moves." },
        { OptimisationTabText.State(MovesState.Refused, FromThePlanInTheRiver), "",
            "An amount of the plan in your river is refused: correct it in the river and save it to see the moves." },
        { OptimisationTabText.FromInPocket(FromANamedPlan), "", "This plan, in your pocket" },
        { OptimisationTabText.MoreThanFrom(FromANamedPlan), "", "More than this plan" },
        { OptimisationTabText.RiverChanged(FromThePlanInTheRiver, 0), "", "The river has changed since you opened or saved this plan: save it to measure the moves from it." },
    };

    // The plan in the river names no card: any change since it was opened or saved is one text.
    [Fact]
    public void RiverChanged_fromThePlanInTheRiver_isOneTextWhateverChanged()
    {
        Enumerable.Range(0, 5).Select(differences => OptimisationTabText.RiverChanged(FromThePlanInTheRiver, differences)).Distinct()
            .ShouldBe(["RiverChanged_Plan"]);
    }

    // A note on a river that holds what Today does not names at least one card.
    [Fact]
    public void RiverChanged_fromTodayNamingNoCard_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => OptimisationTabText.RiverChanged(FromToday, 0));
    }

    // From Today, the river moves away by the values the moves set, unsaid; the cards whose facts differ,
    // the moves were not found for, and the note names them and says how to find the moves for the river.
    // Each text's number of differences, the names it shows (the tab words each, Difference, from its card
    // and whether the river or Today holds the lever) and the sentence the viewer reads; the first is the
    // case found in the browser on 2026-10-25, a royalty added to a river whose Today holds none.
    public static TheoryData<int, string[], string> TodaysNote => new()
    {
        { 1, ["Copyright royalty (not in Today)"], "Your river differs from Today in Copyright royalty (not in Today), beyond what the moves set: "
            + "the moves are still found for Today. Save the river and star it as Today to find them for it." },
        { 2, ["Company year", "Copyright royalty (not in Today)"], "Your river differs from Today in Company year and Copyright royalty (not in Today), beyond what the moves set: "
            + "the moves are still found for Today. Save the river and star it as Today to find them for it." },
        { 3, ["Company year", "Vouchers (not in Today)", "Home office rent"], "Your river differs from Today in Company year, Vouchers (not in Today) and Home office rent, beyond what the moves set: "
            + "the moves are still found for Today. Save the river and star it as Today to find them for it." },
        { 4, ["Company year", "Vouchers (not in Today)"], "Your river differs from Today in Company year, Vouchers (not in Today) and 2 more, beyond what the moves set: "
            + "the moves are still found for Today. Save the river and star it as Today to find them for it." },
        { 7, ["Company year", "Vouchers (not in Today)"], "Your river differs from Today in Company year, Vouchers (not in Today) and 5 more, beyond what the moves set: "
            + "the moves are still found for Today. Save the river and star it as Today to find them for it." },
    };

    [Theory]
    [MemberData(nameof(TodaysNote))]
    public void RiverChanged_fromToday_namesTheCardsAndSaysHowToFindTheMovesForTheRiver(int differences, string[] names, string expected)
    {
        var text = Resources.Format(OptimisationTabText.RiverChanged(FromToday, differences), OptimisationTabText.RiverChangedArguments(names, differences));

        text.ShouldBe(expected);
    }

    // Three names at most: all of them up to three, else the first two, and the text says how many more.
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 2)]
    [InlineData(9, 2)]
    public void Shown_differences_areAllUpToThreeElseTheFirstTwo(int differences, int shown)
    {
        var all = RiverDifferences.Nameable.Take(differences).Select(module => new RiverDifference(module, RiverDifferenceKind.Changed)).ToList();

        OptimisationTabText.Shown(all).ShouldBe(all.Take(shown));
    }

    // « And 1 more » never reads: the text that counts what is left starts from four differences.
    [Fact]
    public void RiverChanged_fromToday_countsWhatIsLeftFromFourDifferences()
    {
        Enumerable.Range(1, 5).Select(differences => OptimisationTabText.RiverChanged(FromToday, differences))
            .ShouldBe(["RiverChanged_One", "RiverChanged_Two", "RiverChanged_Three", "RiverChanged_More", "RiverChanged_More"]);
    }

    // Each card a difference can name has its name, and the river's order is the page's.
    [Fact]
    public void OptimisationTabResx_everyNameableModule_hasAName()
    {
        RiverDifferences.Nameable.Select(OptimisationTabText.Module)
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    // The name a difference gives a card is the title the card draws, so the two cannot drift: each card's
    // own .resx, the income year having none.
    private static readonly (string Module, Type Card)[] Cards =
    [
        ("CompanyYear", typeof(CompanyYearCard)),
        ("Vouchers", typeof(VoucherCard)),
        ("Allowances", typeof(AllowanceCard)),
        ("Rent", typeof(RentCard)),
        ("Car", typeof(CarCard)),
        ("Copyright", typeof(CopyrightCard)),
        ("DirectorPay", typeof(DirectorPayCard)),
        ("CorporateTax", typeof(CorporateTaxCard)),
        ("Dividend", typeof(DividendCard)),
    ];

    public static TheoryData<string, Type> CardTitles => new(Cards);

    [Theory]
    [MemberData(nameof(CardTitles))]
    public void Module_aCard_isNamedByItsTitle(string module, Type card)
    {
        var name = Resources.Get(OptimisationTabText.Module(Enum.Parse<RiverModule>(module)));

        name.ShouldBe(ResxText.For(card).Get("Title"));
    }

    // The income year is the one name no card gives.
    [Fact]
    public void Module_theIncomeYear_isNamedAsSuch()
    {
        Resources.Get(OptimisationTabText.Module(RiverModule.River)).ShouldBe("Income year");
    }

    // Every card but the income year is held to its title above.
    [Fact]
    public void CardTitles_everyNameableModuleButTheIncomeYear_isHeldToItsCard()
    {
        RiverDifferences.Nameable.Where(module => module != RiverModule.River).Select(module => module.ToString()).ShouldBe(Cards.Select(card => card.Module));
    }

    // A difference says whether the river or Today holds a lever the other does not, beside the card's name.
    [Theory]
    [InlineData(nameof(RiverDifferenceKind.Changed), "Copyright royalty")]
    [InlineData(nameof(RiverDifferenceKind.Added), "Copyright royalty (not in Today)")]
    [InlineData(nameof(RiverDifferenceKind.LeftOut), "Copyright royalty (not in your river)")]
    public void Difference_kind_saysWhoHoldsTheLever(string kind, string expected)
    {
        var text = Resources.Format(OptimisationTabText.Difference(Enum.Parse<RiverDifferenceKind>(kind)), "Copyright royalty");

        text.ShouldBe(expected);
    }

    [Fact]
    public void OptimisationTabResx_everyKindOfDifference_hasATextTakingTheName()
    {
        Enum.GetValues<RiverDifferenceKind>().Select(OptimisationTabText.Difference)
            .Where(key => Resources.Get(key)?.Contains("{0}", StringComparison.Ordinal) != true)
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(FromThePlan))]
    public void Text_fromThePlanInTheRiver_saysItIsThatPlan(string key, string argument, string expected)
    {
        var text = Resources.Format(key, argument);

        text.ShouldBe(expected);
    }

    // While the page looks for the moves, the line that opens them once found says from which plan, named as
    // that line names it: the plan, what the tab fills its {0} with, and the text.
    public static TheoryData<string, MovesBaseline, string, string> LookedFor => new()
    {
        { "Today", FromToday, "", "Looking for the moves from Today, your current setup…" },
        { "the plan in the river", FromThePlanInTheRiver, "", "Looking for the moves from the plan in your river, as you opened or saved it…" },
        { "a named plan", FromANamedPlan, "Plan 2", "Looking for the moves from « Plan 2 », as you opened or saved it…" },
    };

    [Theory]
    [MemberData(nameof(LookedFor))]
    public void State_searching_namesThePlanItLooksForMovesFrom(string _, MovesBaseline from, string name, string expected)
    {
        var text = Resources.Format(OptimisationTabText.State(MovesState.Searching, from), name);

        text.ShouldBe(expected);
    }

    [Fact]
    public void OptimisationTabResx_everyReason_hasAText()
    {
        Enum.GetValues<MoveReasonKind>().Select(OptimisationTabText.Why)
            .Where(key => Resources.Get(key) is null)
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
        var text = Resources.Format(OptimisationTabText.Why(Enum.Parse<MoveReasonKind>(kind)), amounts);

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
