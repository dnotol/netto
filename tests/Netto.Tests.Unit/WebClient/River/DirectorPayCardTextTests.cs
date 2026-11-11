using Netto.Core.PersonalTax;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Director pay card builds resource keys from the pay's states and the director's situation. A key
// without a text in its .resx would show on screen as the key itself.
public class DirectorPayCardTextTests
{
    // The name IStringLocalizer<DirectorPayCard> reads River/DirectorPayCard.resx under.
    private static readonly ResxText Resources = ResxText.For<DirectorPayCard>();

    // Every label and bubble of Director pay's lines: the contributions the company pays, a car's
    // benefit and a royalty's part beyond its limits with the taxable pay they make.
    [Fact]
    public void DirectorPayCardResx_everyLabelAndBubbleOfItsLines_hasAText()
    {
        var taxed = RiverInputs.Example.AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
            .AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m).WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.0305m);
        var lines = DirectorPayFigures.Taxed(taxed.Run().Pay.ShouldNotBeNull());

        Resources.Missing(lines.SelectMany(line => new[] { line.Key, line.Help }).OfType<string>()).ShouldBeEmpty();
    }

    // The note says why a surcharge is due or why none is, in every state, and suggests no payment.
    [Fact]
    public void DirectorPayCardResx_everySurchargeNote_hasATextThatNamesNoAmountNorDate()
    {
        var notes = Enum.GetValues<TaxSurchargeState>().Select(DirectorPayCardText.Note).ToList();

        notes.ShouldSatisfyAllConditions(
            () => Resources.Missing(notes).ShouldBeEmpty(),
            () => notes.Select(note => Resources.Get(note).ShouldNotBeNull()).Where(text => text.Contains("{0}")).ShouldBeEmpty());
    }

    // Why the pay goes no further: beside a royalty that follows the pay only while the engine set it at
    // what a ruling accepts; one that is none leaves the whole profit before pay, as no royalty does.
    public static TheoryData<string, RiverInputs, string> CeilingHints => new()
    {
        { "no royalty", RiverInputs.Example, "CeilingHint" },
        { "following without a share", RiverInputs.Example.AddToPlan(OptionalModule.Copyright), "CeilingHint" },
        { "following above the cap", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithAverageWithinCap(false).WithCreativeShare(0.75m), "CeilingHint" },
        { "following at 75%", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m), "CeilingHint_Follows" },
        { "fixed", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m), "CeilingHint_Fixed" },
    };

    [Theory]
    [MemberData(nameof(CeilingHints))]
    public void CeilingHint_namesTheFollowingRoyaltyOnlyWhileARulingAcceptsOne(string _, RiverInputs inputs, string key)
    {
        DirectorPayCardText.CeilingHint(SplitView.From(inputs, inputs.Run()).Royalty).ShouldBe(key);
        Resources.Missing([key]).ShouldBeEmpty();
    }

    // The pay's slider and bound read their own texts: its label, how far the pay goes, and the slider's
    // value with the royalty beside it.
    [Fact]
    public void DirectorPayCardResx_thePayFieldsTexts_haveAText()
    {
        Resources.Missing(["OwnPayLabel", "Ceiling", DirectorPayCardText.SliderValueWithRoyalty]).ShouldBeEmpty();
    }

    // The card's own source names the fee (AR n° 38, art. 20 §4): what no text says is that it is
    // deductible, which Netto's reading supplies.
    [Fact]
    public void FundFeeReading_saysNoTextMakesTheFeeDeductible()
    {
        Resources.Get("FundFeeReading").ShouldNotBeNull().ShouldStartWith("No text that makes it deductible names your fund's management fee. Netto reads it as");
    }

    [Fact]
    public void DirectorPayCardResx_everyPayerOfTheContributions_hasAText()
    {
        Resources.Missing(Enum.GetValues<ContributionsPaidBy>().Select(DirectorPayCardText.PaidBy)).ShouldBeEmpty();
    }

    // Every way the lines of Director pay's disclosures read: either payer, the default rate, a
    // municipality's, a rate of one's own over it, a starter or not, the pay slip's withholding with
    // and without a pay slip, an amount withheld entered, and advance payments or none.
    [Fact]
    public void DirectorPayCardResx_everyPhraseOfItsSettingsLines_hasAText()
    {
        var example = RiverInputs.Example;
        var municipality = MunicipalRates.IncomeYear2026.Municipalities[0];
        RiverInputs[] variations =
        [
            example,
            example.WithContributionsPaidBy(ContributionsPaidBy.Company).WithStarter(true).WithTaxWithheld(1_000m).WithTaxAdvancePayment(0, 1_000m),
            example.WithMunicipality(example.Municipality.Pick(municipality)).WithReserve(-1m),
            example.WithMunicipality(example.Municipality.Pick(municipality).Enter(0.09m)),
            example.WithFundFeeRate(0.0305m),
        ];
        var phrases = variations.Select(inputs => DirectorPaySettingsView.From(inputs, inputs.Run()))
            .SelectMany(view => view.Situation.Concat(view.Prepaid));

        Resources.Missing(phrases.Select(phrase => phrase.Key)).ShouldBeEmpty();
    }

    // How the withholding is derived and what the key formula is are said in the withheld field's
    // bubble, which no term can open inside: its text marks none.
    [Fact]
    public void DirectorPayCardResx_withheldFieldsBubble_marksNoTerm()
    {
        var text = Resources.Get("WithheldHint").ShouldNotBeNull();

        TermText.Split(text).Term.ShouldBeNull();
    }
}
