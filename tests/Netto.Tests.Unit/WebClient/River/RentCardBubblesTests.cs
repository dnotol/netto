using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the rent card's bubbles say about how the rent is taxed, read as a viewer reads them on the card
// drawn open (CardHtml): the title's, the floor's, the flat charges' and the rent field's, with the rates
// and coefficients the year's rules hold. The 2026 rules: the cadastral income indexed by 2.3 plus 40% is
// the floor, flat charges are 40% of what is taxed and at most 2/3 of the cadastral income revalued by 5.75.
public class RentCardBubblesTests
{
    // 12% of a cadastral income of 1,200 let, a rent of 300 under the floor of 463.68.
    private static readonly RiverInputs UnderTheFloor = RiverInputs.Example.AddToPlan(OptionalModule.Rent)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(300m);

    [Fact]
    public async Task Title_bubble_doesNotSayTheRentIsTaxedOnLessThanTheRent()
    {
        var bubble = CardHtml.BubbleAfter(await OpenAsync(UnderTheFloor), "Home office rent");

        bubble.ShouldSatisfyAllConditions(
            () => bubble.ShouldNotContain("less than the rent"),
            () => bubble.ShouldContain("floor"));
    }

    [Fact]
    public async Task FlatCharges_bubble_saysTheirShareOfWhatIsTaxedAndTheirLimit()
    {
        var bubble = CardHtml.BubbleAfter(await OpenAsync(UnderTheFloor), "Flat charges");

        bubble.ShouldSatisfyAllConditions(
            () => bubble.ShouldContain("40%"),
            () => bubble.ShouldContain("2/3"),
            () => bubble.ShouldContain("5.75"),
            () => bubble.ShouldContain("floor"));
    }

    [Fact]
    public async Task Floor_bubble_saysTheIndexationAndTheUplift()
    {
        var bubble = CardHtml.BubbleAfter(await OpenAsync(UnderTheFloor), "Taxed on the floor");

        bubble.ShouldSatisfyAllConditions(
            () => bubble.ShouldContain("2.3"),
            () => bubble.ShouldContain("40%"));
    }

    [Fact]
    public async Task Rent_bubble_saysTheCapAsFiveThirds()
    {
        var bubble = CardHtml.BubbleAfter(await OpenAsync(UnderTheFloor), "Rent for the year");

        bubble.ShouldSatisfyAllConditions(
            () => bubble.ShouldContain("5/3"),
            () => bubble.ShouldContain("5.75"));
    }

    // The household in the plan with a partner taxed jointly: the whole net rent goes in the director's column, which the line it joins
    // the taxable income on says, and that a home owned together is shared by law.
    [Fact]
    public async Task JoinsTaxableIncome_withAPartnerTaxedJointly_saysAllOfItIsTaxedInYourColumn()
    {
        var bubble = CardHtml.BubbleAfter(await OpenAsync(UnderTheFloor.AddToPlan(OptionalModule.Household).WithPartner(true)), "Joins your taxable income");

        bubble.ShouldSatisfyAllConditions(
            () => bubble.ShouldContain("all of it in your column"),
            () => bubble.ShouldContain("does not split it yet"));
    }

    [Fact]
    public async Task JoinsTaxableIncome_withoutAPartner_hasNoBubble()
    {
        var html = await OpenAsync(UnderTheFloor.AddToPlan(OptionalModule.Household).WithPartner(false));

        CardHtml.Bubbles(html).Select(bubble => bubble.Label).ShouldNotContain("Joins your taxable income");
    }

    [Fact]
    public async Task JoinsTaxableIncome_withAPartnerTaxedSeparately_hasNoBubble()
    {
        var html = await OpenAsync(UnderTheFloor.AddToPlan(OptionalModule.Household).WithPartner(true).WithTaxedSeparately(true));

        CardHtml.Bubbles(html).Select(bubble => bubble.Label).ShouldNotContain("Joins your taxable income");
    }

    private static async Task<string> OpenAsync(RiverInputs inputs) => CardHtml.Card(await CardHtml.RenderCardsOpenAsync(inputs), "rent");
}
