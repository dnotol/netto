using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A trial copy of the plan for one idea: the viewer's answers on top of the plan on screen, the lever still
// out of it, searched as the river with the lever added, kept as that river with the values of the idea's
// own moves, never with the retunes. The plan on screen never changes before « Keep ». The figures pinned
// are the engine's own (PisteSearchTests holds the hand derivation of the vouchers' 45 and 18).
public class PisteTrialTests
{
    private static readonly DateOnly On11November = new(2026, 11, 11);

    // The river's example with a royalty that follows the pay at a creative share of 25%, the position
    // paper's plan: 62,624.32 in the pocket.
    private static readonly RiverInputs ExampleWithRoyalty =
        RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.25m).WithRoyaltyFollowingPay();

    [Fact]
    public void Of_aLeverInThePlan_throws()
    {
        Should.Throw<ArgumentException>(() => PisteTrial.Of(ExampleWithRoyalty, LeverKind.Copyright));
    }

    [Theory]
    [InlineData(LeverKind.Car)]
    [InlineData(LeverKind.Allowances)]
    public void Of_aLeverThatIsNoIdea_throws(LeverKind lever)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PisteTrial.Of(RiverInputs.Example, lever));
    }

    // Each idea asks what its card holds and Netto cannot see: the vouchers a yes on their card's conditions,
    // the rent the home's cadastral income and the share let, the royalty the creative share and its two
    // statuses.
    [Theory]
    [InlineData(LeverKind.Vouchers, new[] { PisteQuestion.VouchersGranted })]
    [InlineData(LeverKind.Rent, new[] { PisteQuestion.CadastralIncome, PisteQuestion.ShareLet })]
    [InlineData(LeverKind.Copyright, new[] { PisteQuestion.CreativeShare, PisteQuestion.AverageWithinCap, PisteQuestion.HoldsAttestation })]
    public void Questions_eachIdea_asksWhatItsCardHolds(LeverKind lever, PisteQuestion[] questions)
    {
        PisteTrial.Of(RiverInputs.Example, lever).Questions.ShouldBe(questions);
    }

    // An amount with no default, or a share none is given for, keeps the gain from being measured; a status
    // has its default, and the vouchers' yes changes no figure.
    [Theory]
    [InlineData(LeverKind.Vouchers, new PisteQuestion[0])]
    [InlineData(LeverKind.Rent, new[] { PisteQuestion.CadastralIncome, PisteQuestion.ShareLet })]
    [InlineData(LeverKind.Copyright, new[] { PisteQuestion.CreativeShare })]
    public void Open_onTheExample_isWhatTheGainStillNeeds(LeverKind lever, PisteQuestion[] open)
    {
        PisteTrial.Of(RiverInputs.Example, lever).Open.ShouldBe(open);
    }

    [Fact]
    public void Open_rentAnswered_isNone()
    {
        var trial = PisteTrial.Of(RiverInputs.Example, LeverKind.Rent).WithCadastralIncome(1_500m).WithShareLet(0.15m);

        trial.Open.ShouldBeEmpty();
    }

    [Fact]
    public void Open_royaltyShareGiven_isNone()
    {
        var trial = PisteTrial.Of(RiverInputs.Example, LeverKind.Copyright).WithCreativeShare(0.25m);

        trial.Open.ShouldBeEmpty();
    }

    // The facts the plan keeps for a lever out of it are the trial's first answers.
    [Fact]
    public void Open_factsKeptByThePlan_areAnswered()
    {
        var kept = RiverInputs.Example.WithCadastralIncome(1_500m).WithShareLet(0.15m);

        var trial = PisteTrial.Of(kept, LeverKind.Rent);

        trial.Open.ShouldBeEmpty();
        trial.Answered.ShouldBeFalse();
    }

    // The vouchers are tried only once the viewer says the company grants them on their card's conditions.
    [Fact]
    public void CanTry_vouchers_onlyAfterTheYes()
    {
        var trial = PisteTrial.Of(ExampleWithRoyalty, LeverKind.Vouchers);

        (trial.CanTry, trial.WithGranted(true).CanTry, trial.WithGranted(true).Open.Count).ShouldBe((false, true, 0));
    }

    [Fact]
    public void CanTry_rent_onlyOnceAnswered()
    {
        var trial = PisteTrial.Of(RiverInputs.Example, LeverKind.Rent);

        (trial.CanTry, trial.WithCadastralIncome(1_500m).WithShareLet(0.15m).CanTry).ShouldBe((false, true));
    }

    // Answers go to the trial's river, the viewer's own, the lever still out; the plan stays as it is.
    [Fact]
    public void Answers_leaveThePlanAsItIs()
    {
        var trial = PisteTrial.Of(RiverInputs.Example, LeverKind.Rent).WithCadastralIncome(1_500m).WithShareLet(0.15m);

        (trial.Plan, trial.Answered, trial.River.InPlan(OptionalModule.Rent), trial.River.Rent.CadastralIncome.Origin)
            .ShouldBe((RiverInputs.Example, true, false, InputOrigin.Yours));
    }

    // The trial copy is the river with the lever added: the scenario the idea is searched on.
    [Fact]
    public void WithLever_isTheRiverWithTheLeverAdded()
    {
        var trial = PisteTrial.Of(RiverInputs.Example, LeverKind.Rent).WithCadastralIncome(1_500m);

        trial.WithLever.ShouldBe(trial.River.AddToPlan(OptionalModule.Rent));
    }

    // The vouchers on the position paper's plan, through the river: the Optimiser's own plan, 65,933.99,
    // against the vouchers' plan, 65,996.99, the engine's own figures (a regression pin).
    [Fact]
    public void Search_vouchersOnTheExampleWithARoyalty_gains63()
    {
        var without = ExampleWithRoyalty.FindMoves(On11November)?.Value.ShouldNotBeNull();

        var (piste, error) = PisteTrial.Of(ExampleWithRoyalty, LeverKind.Vouchers).Search(without!, On11November).ShouldNotBeNull();

        error.ShouldBeNull();
        (piste.ShouldNotBeNull().Without.PlanInPocket, piste.With.PlanInPocket, piste.Gain).ShouldBe((65_933.99m, 65_996.99m, 63.00m));
    }

    // The rent's answers reach the search through the trial's river: with the lever added, the river runs the
    // home's facts, which a river with the rent out never shows the engine. Plan against plan, the engine's
    // own figure (a regression pin, PisteSearchTests).
    [Fact]
    public void Search_rentAnswered_measuresTheHomesFacts()
    {
        var without = ExampleWithRoyalty.FindMoves(On11November)?.Value.ShouldNotBeNull();
        var trial = PisteTrial.Of(ExampleWithRoyalty, LeverKind.Rent).WithCadastralIncome(1_500m).WithShareLet(0.15m);

        var piste = trial.Search(without!, On11November)?.Value.ShouldNotBeNull();

        piste!.Gain.ShouldBe(296.82m);
    }

    // A plan that starts at the pay has no ideas, as it has no moves.
    [Fact]
    public void Search_aPlanStartingAtThePay_isNone()
    {
        var plan = RiverInputs.Example.SkipCompany();

        PisteTrial.Of(plan, LeverKind.Vouchers).Search(new MovePlan(0m, [], 0m), On11November).ShouldBeNull();
    }

    // Keeping the vouchers adds them to the plan at their maxima, the values of the idea's own moves, and
    // retunes nothing else: the pay stays where the plan had it.
    [Fact]
    public void Keep_vouchers_addsThemAtTheirMaximaAndRetunesNothing()
    {
        var trial = PisteTrial.Of(ExampleWithRoyalty, LeverKind.Vouchers).WithGranted(true);
        var piste = Searched(trial);

        var kept = trial.Keep(piste);

        (kept.InPlan(OptionalModule.Vouchers), kept.Vouchers.EcoVouchers.Value, kept.Vouchers.SportCultureVouchers.Value, kept.DirectorPay)
            .ShouldBe((true, 250m, 100m, ExampleWithRoyalty.DirectorPay));
        kept.AdvancePayments.ShouldBe(ExampleWithRoyalty.AdvancePayments);
    }

    // The vouchers are kept only after the yes.
    [Fact]
    public void Keep_vouchersWithoutTheYes_throws()
    {
        var trial = PisteTrial.Of(ExampleWithRoyalty, LeverKind.Vouchers);
        var piste = Searched(trial);

        Should.Throw<InvalidOperationException>(() => trial.Keep(piste));
    }

    // Keeping the rent adds it with the home's facts the viewer gave and the rent at its cap.
    [Fact]
    public void Keep_rent_addsTheHomesFactsAndTheRentAtItsCap()
    {
        var trial = PisteTrial.Of(ExampleWithRoyalty, LeverKind.Rent).WithCadastralIncome(1_500m).WithShareLet(0.15m);
        var piste = Searched(trial);
        var cap = piste.Adds.ShouldHaveSingleItem().Move.ShouldBeOfType<RentMove>().Rent;

        var kept = trial.Keep(piste);

        (kept.InPlan(OptionalModule.Rent), kept.Rent.CadastralIncome.Value, kept.Rent.ShareLet.Value, kept.Rent.Rent.Value)
            .ShouldBe((true, 1_500m, 0.15m, cap));
        kept.Run().Error.ShouldBeNull();
    }

    // Keeping the royalty adds it following the pay at the creative share the viewer gave.
    [Fact]
    public void Keep_royalty_addsItFollowingThePay()
    {
        var trial = PisteTrial.Of(RiverInputs.Example, LeverKind.Copyright).WithCreativeShare(0.25m);
        var piste = Searched(trial);

        var kept = trial.Keep(piste);

        (kept.InPlan(OptionalModule.Copyright), kept.Copyright.FollowsPay.Value, kept.Copyright.CreativeShare.Value).ShouldBe((true, true, 0.25m));
    }

    // An idea is kept with its own measure only.
    [Fact]
    public void Keep_anotherIdeasPiste_throws()
    {
        var vouchers = Searched(PisteTrial.Of(ExampleWithRoyalty, LeverKind.Vouchers));
        var rent = PisteTrial.Of(ExampleWithRoyalty, LeverKind.Rent).WithCadastralIncome(1_500m).WithShareLet(0.15m);

        Should.Throw<ArgumentException>(() => rent.Keep(vouchers));
    }

    private static Piste Searched(PisteTrial trial)
    {
        var without = trial.Plan.FindMoves(On11November)?.Value.ShouldNotBeNull();
        return trial.Search(without!, On11November)?.Value.ShouldNotBeNull()!;
    }
}
