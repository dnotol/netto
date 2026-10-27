using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Where each move links to: the card of the Plan tab it acts on while that card is on the tab, named by
// its title, or the When to pay tab for a payment.
public class MoveLinksTests
{
    private static readonly DateOnly On21December = new(2026, 12, 21);

    // The river with every lever whose move links to its card in the plan.
    private static readonly RiverInputs EveryLever = RiverInputs.Example
        .AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Rent).AddToPlan(OptionalModule.Copyright);

    // Each move with the card it acts on, and the card component whose .resx holds its title.
    public static TheoryData<Move, string, Type> Cards => new()
    {
        { new PayMove(20_799m), "split", typeof(SplitCard) },
        { new EcoVouchersMove(250m), "vouchers", typeof(VoucherCard) },
        { new SportCultureVouchersMove(100m), "vouchers", typeof(VoucherCard) },
        { new RentMove(1_380m), "rent", typeof(RentCard) },
        { new CopyrightFollowsPayMove(), "copyright", typeof(CopyrightCard) },
    };

    [Theory]
    [MemberData(nameof(Cards))]
    public void To_moveWithItsCardOnThePlanTab_linksTheCard(Move move, string card, Type _)
    {
        MoveLinks.To(move, EveryLever).ShouldBe(new MoveLink(PlanTab.Plan, card));
    }

    // The card a link opens is one the map knows, by the name its fold is remembered under.
    [Theory]
    [MemberData(nameof(Cards))]
    public void To_moveWithItsCardOnThePlanTab_linksACardOfTheRiversOrder(Move move, string card, Type _)
    {
        (MoveLinks.To(move, EveryLever)?.Card, MapModules.Named(card) is not null).ShouldBe((card, true));
    }

    // The Plan tab, with the card to open on arrival.
    [Theory]
    [MemberData(nameof(Cards))]
    public void Href_card_isThePlanTabWithTheCard(Move move, string card, Type _)
    {
        MoveLinks.To(move, EveryLever).ShouldNotBeNull().Href.ShouldBe($"plan?card={card}");
    }

    // « See it on » the card's own title, as its heading reads.
    [Theory]
    [MemberData(nameof(Cards))]
    public void Text_card_namesTheCardsTitle(Move move, string _, Type card)
    {
        var title = ResxText.For(card).Get("Title");

        Text(MoveLinks.To(move, EveryLever).ShouldNotBeNull()).ShouldBe($"See it on {title.ShouldNotBeNull()}");
    }

    // A lever out of the plan has no card on the Plan tab: its move links nowhere until « Apply » adds it.
    public static TheoryData<Move> Levers => new()
    {
        new EcoVouchersMove(250m), new SportCultureVouchersMove(100m), new RentMove(1_380m), new CopyrightFollowsPayMove(),
    };

    [Theory]
    [MemberData(nameof(Levers))]
    public void To_leverOutOfThePlan_linksNothing(Move move)
    {
        MoveLinks.To(move, RiverInputs.Example).ShouldBeNull();
    }

    // Applying the move adds its lever, and its card, to the plan: the link appears.
    [Theory]
    [MemberData(nameof(Levers))]
    public void To_leverMoveApplied_linksItsCard(Move move)
    {
        var today = RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m).WithCreativeShare(0.75m);
        var applied = MovesView.Of(today, On21December).Apply(new PlannedMove(move, 1m, Year(today), Year(today)), RiverInputs.Example);

        MoveLinks.To(move, applied).ShouldNotBeNull().Tab.ShouldBe(PlanTab.Plan);
    }

    // A river that starts at the pay has no split: the pay's move links nowhere.
    [Fact]
    public void To_payMoveRiverStartingAtThePay_linksNothing()
    {
        MoveLinks.To(new PayMove(20_799m), RiverInputs.Example.SkipCompany()).ShouldBeNull();
    }

    public static TheoryData<Move> Payments => new()
    {
        new CompanyAdvancePaymentMove(3, On21December, 29_700.45m), new DirectorAdvancePaymentMove(3, On21December, 14_317.50m),
    };

    // A payment's place is the When to pay tab, whatever the river holds.
    [Theory]
    [MemberData(nameof(Payments))]
    public void To_payment_linksTheWhenToPayTab(Move move)
    {
        var link = MoveLinks.To(move, RiverInputs.Example.SkipCompany()).ShouldNotBeNull();

        (link, link.Href).ShouldBe((new MoveLink(PlanTab.WhenToPay, null), "plan/when-to-pay"));
    }

    // « See it in » the tab's own name, as the tab bar reads it.
    [Theory]
    [MemberData(nameof(Payments))]
    public void Text_payment_namesTheTab(Move move)
    {
        var tab = ResxText.For<PlanTabBar>().Get(PlanTabBarText.Name(PlanTab.WhenToPay));

        Text(MoveLinks.To(move, RiverInputs.Example).ShouldNotBeNull()).ShouldBe($"See it in {tab.ShouldNotBeNull()}");
    }

    private static string? Text(MoveLink link) =>
        ResxText.For<OptimisationTab>().Get(OptimisationTabText.Link(link));

    private static YearResult Year(RiverInputs river) => river.Run().Year.ShouldNotBeNull();
}
