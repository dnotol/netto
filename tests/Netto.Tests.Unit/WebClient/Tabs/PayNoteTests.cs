using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the Optimisation tab says under the pay's move when the river, at that pay, has a better one: the
// river's own best pay, the one the split card names, from the search the page already runs for the river
// (RiverInputs.SearchSplit), never a search of the tab's own. Found in the browser on 2026-10-25: Today saved
// with no lever, then a copyright royalty that follows the pay added to the river at an 80% creative share:
// the tab's pay line read « In the river » at Today's best pay while the split card named 51,000.
public class PayNoteTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    private static readonly RiverInputs Today = RiverInputs.Example.WithRevenue(168_000m).WithRunningCosts(20_000m);

    private static readonly RiverInputs WithARoyalty = Today.WithCreativeShare(0.8m).AddToPlan(OptionalModule.Copyright);

    // The pay Today's moves give, 20,799: the river holds no royalty there.
    private static readonly PayMove TodaysPay = MovesView.Of(Today, On22October).Moves.Select(move => move.Move).OfType<PayMove>().Single();

    // The river with a royalty at Today's pay, as « Apply » leaves it: the split names another best pay.
    [Fact]
    public void RiversBestPay_riverWithARoyaltyAtTheMovesPay_isTheBestPayTheSplitNames()
    {
        var river = WithARoyalty.WithDirectorPay(TodaysPay.Pay);

        var best = PayNote.RiversBestPay(TodaysPay, river, river.SearchSplit());

        (TodaysPay.Pay, best).ShouldBe((20_799m, (decimal?)51_000m));
    }

    // Before « Apply » too: the river's pay is elsewhere, and what it would leave at the move's pay is
    // still not the best.
    [Fact]
    public void RiversBestPay_riverNotAtTheMovesPayWhoseBestIsAnother_isTheBestPayToo()
    {
        var best = PayNote.RiversBestPay(TodaysPay, WithARoyalty, WithARoyalty.SearchSplit());

        best.ShouldBe(51_000m);
    }

    // The river Today's moves were found for: its best is the move's pay, the split card offers nothing.
    [Fact]
    public void RiversBestPay_riverTheMoveWasFoundFor_isNone()
    {
        var river = Today.WithDirectorPay(TodaysPay.Pay);

        PayNote.RiversBestPay(TodaysPay, river, river.SearchSplit()).ShouldBeNull();
    }

    // The prepaid example, whose best pay is 20,799 (SplitSearchViewTests): a pay a euro from it is the best,
    // as the split card takes it, and one far from it is not.
    private static readonly RiverInputs Prepaid = RiverInputs.Example.WithAdvancePayment(0, 25_000m).WithTaxWithheld(1_000_000m);

    [Fact]
    public void RiversBestPay_moveAEuroFromTheBest_isNone()
    {
        PayNote.RiversBestPay(new PayMove(20_800m), Prepaid, Prepaid.SearchSplit()).ShouldBeNull();
    }

    [Fact]
    public void RiversBestPay_moveFarFromTheBest_isTheBest()
    {
        PayNote.RiversBestPay(new PayMove(50_000m), Prepaid, Prepaid.SearchSplit()).ShouldBe(20_799m);
    }

    // Only the pay's move is weighed against the river's best pay: the others change other values.
    public static TheoryData<string, Move> OtherMoves => new()
    {
        { "a company payment", new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 1_000m) },
        { "a director payment", new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 1_000m) },
        { "eco vouchers", new EcoVouchersMove(250m) },
        { "sport and culture vouchers", new SportCultureVouchersMove(100m) },
        { "the rent", new RentMove(1_380m) },
        { "the royalty following the pay", new CopyrightFollowsPayMove() },
    };

    [Theory]
    [MemberData(nameof(OtherMoves))]
    public void RiversBestPay_moveThatIsNotThePay_isNone(string _, Move move)
    {
        PayNote.RiversBestPay(move, WithARoyalty, WithARoyalty.SearchSplit()).ShouldBeNull();
    }

    // Nothing to name while the page found no best pay for the river: the company's year skipped has no
    // search, and a river refused at the move's pay leaves no figure to weigh it by.
    [Fact]
    public void RiversBestPay_noSearch_isNone()
    {
        var river = Today.SkipCompany();

        PayNote.RiversBestPay(TodaysPay, river, river.SearchSplit()).ShouldBeNull();
    }

    [Fact]
    public void RiversBestPay_riverRefused_isNone()
    {
        var refused = WithARoyalty.WithReserve(-1m);

        PayNote.RiversBestPay(TodaysPay, refused, WithARoyalty.SearchSplit()).ShouldBeNull();
    }

    // No pay keeps the reserve: the split card says so rather than name a best pay, and so is the tab silent.
    [Fact]
    public void RiversBestPay_noPayKeepsTheReserve_isNone()
    {
        var river = Today.WithReserve(1_000_000m);

        PayNote.RiversBestPay(TodaysPay, river, river.SearchSplit()).ShouldBeNull();
    }

    // It reads the search it is given, the page's, and runs none of its own: a search whose best pay is
    // another says that one.
    [Fact]
    public void RiversBestPay_theSearchGiven_isTheOneRead()
    {
        var search = WithARoyalty.SearchSplit().ShouldNotBeNull();
        var found = search with { Best = search.Best.ShouldNotBeNull() with { Pay = 51_234m } };

        PayNote.RiversBestPay(TodaysPay, WithARoyalty, found).ShouldBe(51_234m);
    }

    // The sentence under the pay's move names the pay and says it is not this one.
    [Fact]
    public void BestPayText_saysTheRiversBestPayAndThatItIsNotTheMoves()
    {
        var text = ResxText.For<OptimisationTab>().Format("BestPay", "€51,000.00");

        text.ShouldBe("For your river as it stands, the best pay is €51,000.00, not this one.");
    }
}
