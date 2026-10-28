using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The reserve the company keeps, on the Dividend card: the most it can pay out is a line once a reserve
// is kept, which with the dividend and what is kept says a covered reserve; a note only for a reserve it
// cannot cover, which the field's value (what is kept, never above what there is) does not say.
public class DividendReserveCardTests
{
    private static readonly ResxText Texts = ResxText.For<DividendCard>();

    // The example's year leaves 40,000 after tax to pay out.
    private static readonly RiverInputs Prepaid = RiverInputs.Example.WithAdvancePayment(0, 7_500m).WithTaxWithheld(11_334m);

    [Fact]
    public void From_aReserve_isTheOneTheYearKeeps()
    {
        var view = DividendView.From(Prepaid.WithReserve(15_000m).Run().Year.ShouldNotBeNull());

        (view.Reserve, view.KeepsReserve).ShouldBe((15_000m, true));
    }

    [Fact]
    public void From_aReserveBeyondWhatCanBePaidOut_isNotKept()
    {
        var view = DividendView.From(Prepaid.WithReserve(50_000m).Run().Year.ShouldNotBeNull());

        (view.Reserve, view.KeepsReserve).ShouldBe((50_000m, false));
    }

    public static TheoryData<string, decimal, string?> Notes => new()
    {
        { "no reserve", 0m, null },
        { "a reserve covered", 15_000m, null },
        { "a reserve covered to the cent", 40_000m, null },
        { "a reserve not covered", 40_000.01m, "Reserve_NotCovered" },
    };

    [Theory]
    [MemberData(nameof(Notes))]
    public void ReserveNote_isSaidOnlyForAReserveNotCovered(string _, decimal reserve, string? key)
    {
        DividendCardText.ReserveNote(DividendView.From(Prepaid.WithReserve(reserve).Run().Year.ShouldNotBeNull())).ShouldBe(key);
    }

    [Fact]
    public void DividendCardResx_theReserveTexts_haveATextOfTheirOwn()
    {
        Texts.Missing([DividendFigures.MostPaidOut, "Reserve_NotCovered"]).ShouldBeEmpty();
    }

    [Fact]
    public async Task DividendCard_aReserveCovered_drawsTheMostThatCanBePaidOutBeforeTheDividendAndNoNote()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Prepaid.WithReserve(15_000m)), "dividend");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).Take(2).Select(line => (line.Label, line.Amount))
                .ShouldBe([(Texts.Get(DividendFigures.MostPaidOut).ShouldNotBeNull(), "€40,000.00"), (Texts.Get(DividendFigures.Dividend).ShouldNotBeNull(), "€25,000.00")]),
            () => CardHtml.Prose(html).ShouldNotContain(Texts.Format("Reserve_NotCovered", Formats.Euros(15_000m))),
            () => CardHtml.LeakedKeys(html).ShouldBeEmpty());
    }

    // The field shows what the company keeps, which is all it has: the note is where the reserve typed is
    // said, with nothing paid out.
    [Fact]
    public async Task DividendCard_aReserveNotCovered_saysTheReserveInANoteAndPaysNothingOut()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Prepaid.WithReserve(50_000m)), "dividend");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).Select(line => (line.Label, line.Amount)).First()
                .ShouldBe((Texts.Get(DividendFigures.MostPaidOut).ShouldNotBeNull(), "€40,000.00")),
            () => CardHtml.Lines(html).ShouldNotContain(line => line.Label == Texts.Get(DividendFigures.Dividend)),
            () => CardHtml.Prose(html).ShouldContain(Texts.Format("Reserve_NotCovered", "€50,000.00")),
            () => CardHtml.LeakedKeys(html).ShouldBeEmpty());
    }

    // No reserve kept, nothing is stated of one.
    [Fact]
    public async Task DividendCard_noReserve_statesNothingOfOne()
    {
        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(Prepaid), "dividend");

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Lines(html).ShouldNotContain(line => line.Label == Texts.Get(DividendFigures.MostPaidOut)),
            () => CardHtml.Prose(html).ShouldNotContain(Texts.Format("Reserve_NotCovered", Formats.Euros(0m))));
    }
}
