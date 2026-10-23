using System.Globalization;
using System.Resources;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The rent card builds resource keys from what it shows. A key without a text in its .resx would show
// on screen as the key itself.
public class RentCardTextTests
{
    // The name IStringLocalizer<RentCard> reads River/RentCard.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.RentCard", typeof(RentCard).Assembly);

    // The rent's summary ends on what the split shares: « before your pay », and « before your pay and
    // royalty » once the royalty is in the plan.
    [Fact]
    public void RentCardResx_everySummary_hasAText()
    {
        Missing([RentCardText.Summary(false), RentCardText.Summary(true)]).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, "Summary")]
    [InlineData(true, "Summary_Royalty")]
    public void RentSummary_royaltyInThePlan_saysBeforeYourPayAndRoyalty(bool royalty, string key)
    {
        RentCardText.Summary(royalty).ShouldBe(key);
    }

    [Fact]
    public void RentCardResx_theCapAndWhatTheRentIsTaxedOn_markTheirTerms()
    {
        Terms(["Cap", "TaxedOnTheRent", "TaxedOnTheFloor"]).ShouldBe(["cadastral income", "flat charges", "flat charges"]);
    }

    [Fact]
    public void RentCardResx_cadastralIncomeAndFlatCharges_sayWhatTheyMean()
    {
        Missing([RentCardText.CadastralIncomeTerm, RentCardText.FlatChargesTerm]).ShouldBeEmpty();
    }

    // The lines of « Your home »: the example's facts and facts given.
    [Fact]
    public void RentCardResx_everyPhraseOfItsSettingsLine_hasAText()
    {
        RiverInputs[] variations = [RiverInputs.Example, RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m)];
        var phrases = variations.SelectMany(inputs => RentSettingsView.From(inputs).Home);

        Missing(phrases.Select(phrase => phrase.Key).Append("HomeTitle")).ShouldBeEmpty();
    }

    // The term each sentence marks, null where its text marks none or is missing.
    private static List<string?> Terms(IEnumerable<string> keys) =>
        [.. keys.Select(key => Resources.GetString(key, CultureInfo.InvariantCulture) is { } text ? TermText.Split(text).Term : null)];

    private static IEnumerable<string> Missing(IEnumerable<string> keys) =>
        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
