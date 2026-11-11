using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The switches over the moves name every move they know and refuse the rest: a move added to the engine
// without its words, its place in the river or its link throws where the tab first meets it, never shows
// no words, an « Apply » that has nothing to apply, or no link.
public class UnknownMoveTests
{
    private static readonly DateOnly On21December = new(2026, 12, 21);

    [Fact]
    public void MoveArguments_aMoveItHasNoWordsFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => OptimisationTabText.MoveArguments(new UnknownMove(), 20_799m, 0.75m));
    }

    [Fact]
    public void Apply_aMoveTheRiverCannotApply_isRefusedNotAppliedAsNothing()
    {
        var river = RiverInputs.Example;
        var year = river.Run().Year.ShouldNotBeNull();
        var unknown = new PlannedMove(new UnknownMove(), 1m, year, year);

        Should.Throw<ArgumentOutOfRangeException>(() => MovesView.Of(river, On21December).Apply(unknown, river));
    }

    [Fact]
    public void To_aMoveWithNoPlaceInTheRiver_isRefusedNotLinkedNowhere()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MoveLinks.To(new UnknownMove(), RiverInputs.Example));
    }

    // The words of a move are its sentence's placeholders, formatted as the page reads them.
    public static TheoryData<Move, string[]> Words => new()
    {
        { new PayMove(20_799m), ["€20,799.00", "€30,000.00"] },
        { new CompanyAdvancePaymentMove(3, On21December, 29_700.45m), ["€29,700.45", "21 December 2026"] },
        { new DirectorAdvancePaymentMove(3, On21December, 14_317.50m), ["€14,317.50", "21 December 2026"] },
        { new EcoVouchersMove(250m), ["€250.00"] },
        { new SportCultureVouchersMove(100m), ["€100.00"] },
        { new RentMove(1_380m), ["€1,380.00"] },
        { new CopyrightFollowsPayMove(), ["75%"] },
    };

    [Theory]
    [MemberData(nameof(Words))]
    public void MoveArguments_aKnownMove_isItsFiguresFormattedAsThePageReadsThem(Move move, string[] expected)
    {
        OptimisationTabText.MoveArguments(move, 30_000m, 0.75m).ShouldBe(expected);
    }

    // A move the engine has but this file does not name: the stand-in for the one not written yet.
    private sealed record UnknownMove : Move
    {
        public override Scenario Apply(Scenario scenario) => scenario;
    }
}
