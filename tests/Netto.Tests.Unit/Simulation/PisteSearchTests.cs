using Netto.Core.Copyright;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// An idea's gain is the optimised plan with the lever less the optimised plan without it: the « without »
// side the Optimiser's own search (retuning only the levers in the plan), the « with » side the same search
// with that lever's finders too. The figures pinned here are the engine's own, a regression pin; where a
// figure is derived by hand, the derivation stands beside it.
public class PisteSearchTests
{
    private static readonly DateOnly On11November = new(2026, 11, 11);

    // Fixtures.ExampleWithRoyalty as a scenario: the river's example year with a copyright royalty that
    // follows the pay at a creative share of 25% (the fixture says where the figures pinned on it come from).
    private static readonly Scenario ExampleWithRoyalty = RiverExample with
    {
        Copyright = new CopyrightRoyalty(0m) { FollowsPay = true, CreativeShare = 0.25m },
    };

    private static readonly IReadOnlySet<LeverKind> RoyaltyInPlan = new HashSet<LeverKind> { LeverKind.Copyright };

    private static readonly IReadOnlySet<LeverKind> NoLever = new HashSet<LeverKind>();

    // The figures of Fixtures.ExampleWithRoyalty: optimised without the vouchers, 65,933.99; with them,
    // 65,996.99; the idea gains 63.00. By hand, at the optimised pay of
    // 20,807, where the company pays 18% withholding on its dividend: eco vouchers are deductible for no
    // part (VoucherCalculator deducts only the meal vouchers' 4 a day), so the 250 the company pays for
    // them joins its taxable income and its corporate tax does not move; its profit after tax, all paid
    // out as a dividend, falls by 250, the withholding on it by 18% of 250, 45.00, the net dividend by
    // 205.00, while the 250 of vouchers reach the pocket exempt: 250 - 205 = +45.00. The sport/culture
    // vouchers the same way on 100: 100 - 82 = +18.00. The pay stays where the split put it.
    [Fact]
    public void Run_vouchersOnTheExampleWithARoyalty_gainsWhatTheirTunedPlanAdds()
    {
        var without = Without(ExampleWithRoyalty, RoyaltyInPlan);

        var (piste, error) = PisteSearch.Run(without, ExampleWithRoyalty, LeverKind.Vouchers, On11November, RoyaltyInPlan);

        error.ShouldBeNull();
        piste.ShouldNotBeNull();
        piste.Without.PlanInPocket.ShouldBe(65_933.99m);
        piste.With.PlanInPocket.ShouldBe(65_996.99m);
        piste.Gain.ShouldBe(63.00m);
        piste.Adds.Select(move => (move.Move, move.Gain)).ShouldBe([(new EcoVouchersMove(250m), 45.00m), (new SportCultureVouchersMove(100m), 18.00m)]);
    }

    // Two mappings from a move to its lever, the finder's and the idea search's, must not disagree: on a plan
    // where every finder has a move to propose, the lever the idea search reads off a move is its finder's.
    [Fact]
    public void LeverOf_theMoveOfEveryFinder_isTheLeverOfItsFinder()
    {
        var everyFinderProposes = RiverExample with
        {
            Copyright = new CopyrightRoyalty(0m) { CreativeShare = 0.25m },
            Rent = new HomeOfficeRent(1_500m, 0.15m, 0m),
            TaxPrepayments = TaxPrepayments.None,
        };

        var finders = MoveSearch.Finders(On11November);

        var proposed = finders.Select(finder => (Finder: finder.GetType().Name, Move: finder.Find(everyFinderProposes)));
        proposed.Where(found => found.Move is null).ShouldBeEmpty();
        finders.Select(finder => (finder.GetType().Name, Lever: PisteSearch.LeverOf(finder.Find(everyFinderProposes)!)))
            .ShouldBe(finders.Select(finder => (finder.GetType().Name, finder.Lever)));
    }

    // The search does no arithmetic of its own: the gain is the pocket of one run of the year less another's.
    [Fact]
    public void Run_gain_isTheDifferenceOfTwoRunsOfTheYear()
    {
        var without = Without(ExampleWithRoyalty, RoyaltyInPlan);

        var piste = PisteSearch.Run(without, ExampleWithRoyalty, LeverKind.Vouchers, On11November, RoyaltyInPlan).Value.ShouldNotBeNull();

        piste.Gain.ShouldBe(Run(piste.With.Moves[^1].After.Scenario).NetInPocket - Run(piste.Without.Moves[^1].After.Scenario).NetInPocket);
    }

    // The rent's idea is plan against plan: the pay retunes around the rent, so the plan gains more than the
    // rent's own move line says. Both are the engine's own figures (a regression pin): a cadastral income of
    // 1,500, 15% of it let.
    [Fact]
    public void Run_rent_gainsThePlanAgainstPlanNotTheRentsMoveLine()
    {
        var home = ExampleWithRoyalty with { Rent = new HomeOfficeRent(1_500m, 0.15m, 0m) };
        var without = Without(ExampleWithRoyalty, RoyaltyInPlan);

        var piste = PisteSearch.Run(without, home, LeverKind.Rent, On11November, RoyaltyInPlan).Value.ShouldNotBeNull();

        var rent = piste.Adds.ShouldHaveSingleItem();
        rent.Move.ShouldBeOfType<RentMove>();
        rent.Gain.ShouldBe(276.41m);
        piste.Gain.ShouldBe(296.82m);
        piste.Gain.ShouldNotBe(rent.Gain);
    }

    // Without the home's cadastral income no rent can be set: the idea adds nothing.
    [Fact]
    public void Run_rentWithoutItsCadastralIncome_addsNothing()
    {
        var without = Without(ExampleWithRoyalty, RoyaltyInPlan);

        var piste = PisteSearch.Run(without, ExampleWithRoyalty, LeverKind.Rent, On11November, RoyaltyInPlan).Value.ShouldNotBeNull();

        piste.Adds.ShouldBeEmpty();
        piste.Gain.ShouldBe(0m);
    }

    // A creative share given, the royalty follows the pay: the engine's own figure on the example (a
    // regression pin).
    [Fact]
    public void Run_royaltyWithACreativeShare_followsThePay()
    {
        var withShare = RiverExample with { Copyright = new CopyrightRoyalty(0m) { CreativeShare = 0.25m } };
        var without = Without(RiverExample, NoLever);

        var piste = PisteSearch.Run(without, withShare, LeverKind.Copyright, On11November, NoLever).Value.ShouldNotBeNull();

        piste.Adds.ShouldHaveSingleItem().Move.ShouldBeOfType<CopyrightFollowsPayMove>();
        piste.Gain.ShouldBe(311.30m);
    }

    // No creative share, no royalty a ruling accepts: the idea adds nothing.
    [Fact]
    public void Run_royaltyWithoutACreativeShare_addsNothing()
    {
        var without = Without(RiverExample, NoLever);

        var piste = PisteSearch.Run(without, RiverExample, LeverKind.Copyright, On11November, NoLever).Value.ShouldNotBeNull();

        piste.Adds.ShouldBeEmpty();
        piste.Gain.ShouldBe(0m);
    }

    // The plan with the lever is refused as the engine refuses it.
    [Fact]
    public void Run_leverFactsTheEngineRefuses_isRefused()
    {
        var refused = ExampleWithRoyalty with { Rent = new HomeOfficeRent(-1m, 0.15m, 0m) };
        var without = Without(ExampleWithRoyalty, RoyaltyInPlan);

        var (piste, error) = PisteSearch.Run(without, refused, LeverKind.Rent, On11November, RoyaltyInPlan);

        piste.ShouldBeNull();
        error.ShouldBe(RentCalculator.CadastralIncomeNegative);
    }

    // An idea measures one lever alone: the vouchers' plan never takes the rent's move, even with the home's
    // facts in the scenario and the rent out of the plan.
    [Fact]
    public void Run_anIdea_takesNoOtherLeversMove()
    {
        var home = ExampleWithRoyalty with { Rent = new HomeOfficeRent(1_500m, 0.15m, 0m) };
        var without = Without(home, RoyaltyInPlan);

        var piste = PisteSearch.Run(without, home, LeverKind.Vouchers, On11November, RoyaltyInPlan).Value.ShouldNotBeNull();

        piste.With.Moves.ShouldNotContain(move => move.Move is RentMove);
        piste.Gain.ShouldBe(63.00m);
    }

    // A lever already in the plan is no idea: asking for one is a caller's mistake.
    [Fact]
    public void Run_aLeverInThePlan_throws()
    {
        var without = Without(ExampleWithRoyalty, RoyaltyInPlan);

        Should.Throw<ArgumentException>(() => PisteSearch.Run(without, ExampleWithRoyalty, LeverKind.Copyright, On11November, RoyaltyInPlan));
    }

    // Only the levers whose gain in cash the engine measures are ideas: the IT and phone, the car and the
    // allowances are not.
    [Theory]
    [InlineData(LeverKind.Equipment)]
    [InlineData(LeverKind.Car)]
    [InlineData(LeverKind.Allowances)]
    public void Run_aLeverThatIsNoIdea_throws(LeverKind lever)
    {
        var without = Without(RiverExample, NoLever);

        Should.Throw<ArgumentOutOfRangeException>(() => PisteSearch.Run(without, RiverExample, lever, On11November, NoLever));
    }

    [Fact]
    public void Levers_areTheVouchersTheRentAndTheRoyalty()
    {
        PisteSearch.Levers.ShouldBe([LeverKind.Vouchers, LeverKind.Rent, LeverKind.Copyright]);
    }

    // The Optimiser's own search of the plan: the levers in it retuned, none added.
    private static MovePlan Without(Scenario plan, IReadOnlySet<LeverKind> inPlan) =>
        MoveSearch.Run(plan, On11November, inPlan).Value.ShouldNotBeNull();
}
