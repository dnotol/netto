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

    private static readonly RiverInputs WithPartner = RiverInputs.Example.AddToPlan(OptionalModule.Household).WithPartner(true);

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
                Texts.Format("Figure_YourIncome"), Texts.Format("Figure_QuotientToPartner"), Texts.Format("Figure_YourIncomeOnTheScale"),
                Texts.Format("Figure_PartnerIncomeOnTheScale"), Texts.Format("Figure_QuotientSaves"), Texts.Format("Figure_HouseholdSaves"),
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
        var saves = WithPartner.Run().Pay.ShouldNotBeNull().HouseholdSaves;

        CardHtml.Summary(html).ShouldBe($"{Texts.Format("Summary_Quotient", "€11,780.00")} · {Texts.Format("Summary_LessTax", Netto.Web.Client.Formats.Euros(saves))}");
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

    // The quotient's figure is net of the credit for children, which the quotient can move: its bubble says so,
    // as the bubble of what the household saves altogether does.
    [Fact]
    public async Task Card_quotientSaves_bubbleNamesTheCreditForChildren()
    {
        var html = await CardAsync(WithPartner.WithChildren(2));

        CardHtml.BubbleAfter(html, Texts.Format("Figure_QuotientSaves")).ShouldContain("credit for children");
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

    // Without a partner the card asks for none of their facts, and the dependants still count: a single parent
    // of two, one under 3, has the supplements drawn before any note, with no joint year to draw.
    [Fact]
    public async Task Card_singleParentOfTwo_hidesThePartnerAndDrawsTheSupplements()
    {
        var html = await CardAsync(SingleParentOfTwo);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldNotContain("river-partner-income"),
            () => html.ShouldNotContain(Texts.Format("SituationTitle")),
            () => CardHtml.Lines(html).Select(line => line.Label).ShouldBe(
            [
                Texts.Format("Figure_SupplementChildren", 2), Texts.Format("Figure_SupplementUnder3", 1), Texts.Format("Figure_SupplementSingleParent"),
                Texts.Format("Figure_SupplementsToYou"), Texts.Format("Figure_DependantsSave"),
            ]),
            () => CardHtml.Lines(html)[0].Amount.ShouldBe("€5,130.00"),
            () => CardHtml.Prose(html).ShouldNotContain(Texts.Format(HouseholdCardText.Counted)));
    }

    // The dependants' counts are integer fields in a group named by its label, each labelled; the single
    // parent's tick shows while the year is not taxed jointly, which never takes it.
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Card_dependantsFields_areAGroupWithTheSingleParentsTickOnlyAlone(bool partner, bool tick)
    {
        var html = await CardAsync(WithPartner.WithPartner(partner));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("role=\"group\" aria-labelledby=\"household-dependants-title\""),
            () => html.ShouldContain("id=\"household-dependants-title\""),
            () => DependantFields.Where(id => !html.Contains($"for=\"{id}\"", StringComparison.Ordinal) || !html.Contains($"id=\"{id}\"", StringComparison.Ordinal)).ShouldBeEmpty(),
            () => html.Contains(Texts.Format("SingleParentLabel"), StringComparison.Ordinal).ShouldBe(tick));
    }

    [Fact]
    public async Task Summary_singleParentOfTwo_isTheDependantsAndWhatTheySave()
    {
        var saves = SingleParentOfTwo.Run().Pay.ShouldNotBeNull().HouseholdSaves;

        CardHtml.Summary(await CardAsync(SingleParentOfTwo))
            .ShouldBe($"{Texts.Format("Summary_Dependants", 2)} · {Texts.Format("Summary_LessTax", Netto.Web.Client.Formats.Euros(saves))}");
    }

    // The engine's refusal of a count shows on the card, its field marked.
    [Fact]
    public async Task Card_moreChildrenUnder3ThanChildren_saysTheEnginesRefusalOnIt()
    {
        var html = await CardAsync(WithPartner.WithChildren(1).WithChildrenUnder3(2));

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).ShouldBeEmpty(),
            () => CardHtml.TextInSight(html).ShouldContain(ResxText.For<RefusedMessage>().Format("Error_children_under_3_out_of_range")));
    }

    [Fact]
    public async Task Card_singleParentOfTwo_leaksNoKey()
    {
        CardHtml.LeakedKeys(await CardAsync(SingleParentOfTwo)).ShouldBeEmpty();
    }

    private static readonly string[] DependantFields = ["river-children", "river-children-under-3", "river-dependants-in-care", "river-other-dependants"];

    private static readonly RiverInputs SingleParentOfTwo = WithPartner.WithPartner(false).WithChildren(2).WithChildrenUnder3(1).WithSingleParent(true);

    private static async Task<string> CardAsync(RiverInputs river) => CardHtml.Card(await CardHtml.RenderCardsOpenAsync(river), "household");
}
