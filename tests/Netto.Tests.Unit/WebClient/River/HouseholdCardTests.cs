using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Household card on the river's example (RiverInputsTests), 50,000 of pay, the partner in the plan, as a
// viewer reads it among the river's cards, all open (CardHtml): its figures before its notes, a summary of
// figures with their labels, a note for what a figure cannot say with « marital quotient » explained in a
// bubble, and the two statuses behind « About you two ». The figures are HouseholdFiguresTests'.
public class HouseholdCardTests
{
    private static readonly ResxText Texts = ResxText.For<HouseholdCard>();

    private static readonly RiverInputs WithPartner = RiverInputs.Example.AddToPlan(OptionalModule.Household);

    public static TheoryData<string, RiverInputs> Summaries => new()
    {
        { "a partner with no income", WithPartner },
        { "a partner earning a third of the two", WithPartner.WithPartnerIncome(40_000m) },
        { "a year taxed separately", WithPartner.WithTaxedSeparately(true) },
    };

    [Fact]
    public async Task Card_partnerWithNoIncome_drawsItsFiguresThenWhatTheRiverCounts()
    {
        var html = await CardAsync(WithPartner);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).Select(line => line.Label).ShouldBe(
            [
                Texts.Format("Figure_YourIncome"), Texts.Format("Figure_QuotientToPartner"), Texts.Format("Figure_YourTaxableIncome"),
                Texts.Format("Figure_PartnerTaxableIncome"), Texts.Format("Figure_QuotientSaves"),
            ]),
            () => CardHtml.Lines(html)[1].Amount.ShouldBe("−€11,780.00"),
            () => CardHtml.Prose(html).ShouldContain(Texts.Format(HouseholdCardText.Counted)),
            () => html.IndexOf("figure-list", StringComparison.Ordinal).ShouldBeLessThan(html.IndexOf("household__note", StringComparison.Ordinal)));
    }

    [Theory]
    [MemberData(nameof(Summaries))]
    public async Task Summary_card_isFiguresWithTheirLabelsAndNoSentence(string _, RiverInputs river)
    {
        var summary = CardHtml.Summary(await CardAsync(river)).ShouldNotBeNull();

        CardHtml.SummaryFaults(summary).ShouldBeEmpty();
    }

    [Fact]
    public async Task Summary_partnerWithNoIncome_isTheQuotientAndWhatItSaves()
    {
        var html = await CardAsync(WithPartner);
        var saves = HouseholdFiguresTests.View(WithPartner).QuotientSaves;

        CardHtml.Summary(html).ShouldBe(Texts.Format("Summary_Quotient", "€11,780.00", Netto.Web.Client.Formats.Euros(saves)));
    }

    // Taxed separately the year has no joint figures: the note says why, the quotient explained in its bubble.
    [Fact]
    public async Task Card_yearTaxedSeparately_saysSoWithTheQuotientExplainedAndDrawsNoFigure()
    {
        var html = await CardAsync(WithPartner.WithTaxedSeparately(true));

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).ShouldBeEmpty(),
            () => CardHtml.Prose(html).ShouldContain(paragraph => paragraph.StartsWith("Taxed separately this year: each of you files alone", StringComparison.Ordinal)),
            () => System.Text.RegularExpressions.Regex.IsMatch(html, @">\s*marital quotient\s*</button>").ShouldBeTrue(),
            () => html.ShouldContain(Texts.Format(HouseholdCardText.QuotientMeaning)));
    }

    [Fact]
    public async Task Card_partnerEarningAThirdOfTheTwo_saysNoQuotientGoesEitherWay()
    {
        var html = await CardAsync(WithPartner.WithPartnerIncome(40_000m));

        CardHtml.Prose(html).ShouldContain(paragraph => paragraph.StartsWith("No marital quotient goes either way: the one of you who earns less has 30% or more", StringComparison.Ordinal));
    }

    // The statuses behind « About you two », assumed until the user says otherwise.
    [Fact]
    public async Task Card_example_statesBothStatusesAsAssumed()
    {
        var html = await CardAsync(WithPartner);

        CardHtml.TextInSight(html).ShouldContain($"{Texts.Format("Situation_NotBothAged66", 2027)}");
    }

    [Theory]
    [MemberData(nameof(Summaries))]
    public async Task Card_anyYear_leaksNoKey(string _, RiverInputs river)
    {
        CardHtml.LeakedKeys(await CardAsync(river)).ShouldBeEmpty();
    }

    // The engine's refusal of the partner's income shows on the card, its field marked.
    [Fact]
    public async Task Card_partnersIncomeBelowNothing_saysTheEnginesRefusalOnIt()
    {
        var html = await CardAsync(WithPartner.WithPartnerIncome(-1m));

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).ShouldBeEmpty(),
            () => CardHtml.TextInSight(html).ShouldContain(ResxText.For<RefusedMessage>().Format("Error_partner_income_negative")));
    }

    private static async Task<string> CardAsync(RiverInputs river) => CardHtml.Card(await CardHtml.RenderCardsOpenAsync(river), "household");
}
