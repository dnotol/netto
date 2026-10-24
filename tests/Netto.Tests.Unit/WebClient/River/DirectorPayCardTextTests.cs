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
    // benefit and a royalty's part beyond its limits with the taxable pay they make, and what more by the
    // first deadline still open leaves no surcharge due.
    [Fact]
    public void DirectorPayCardResx_everyLabelAndBubbleOfItsLines_hasAText()
    {
        var taxed = RiverInputs.Example.AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
            .AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m).WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.0305m);
        var due = RiverInputs.Example.WithTaxWithheld(0m);
        var lines = DirectorPayFigures.Taxed(taxed.Run().Pay.ShouldNotBeNull())
            .Concat(DirectorPayFigures.Surcharge(MoreBy.Director(due, due.Run().Pay.ShouldNotBeNull(), new DateOnly(2026, 10, 23))));

        Resources.Missing(lines.SelectMany(line => new[] { line.Key, line.Help }).OfType<string>()).ShouldBeEmpty();
    }

    [Fact]
    public void DirectorPayCardResx_everyTaxSurchargeState_hasAText()
    {
        Resources.Missing(Enum.GetValues<TaxSurchargeState>().Select(DirectorPayCardText.Surcharge)).ShouldBeEmpty();
    }

    // With a surcharge due and a deadline open the card draws a line, not a note; with it due and every
    // deadline past it says so; any other state is why none is due, as before.
    [Fact]
    public void DirectorPayCardText_surchargeDueWithEveryDeadlinePast_isTheTooLateNote()
    {
        DirectorPayCardText.Note(TaxSurchargeState.Due, null).ShouldBe("Surcharge_TooLate");
    }

    [Fact]
    public void DirectorPayCardText_surchargeNotDue_isWhyNoneIsDue()
    {
        var states = Enum.GetValues<TaxSurchargeState>().Where(state => state != TaxSurchargeState.Due).ToList();

        states.Select(state => DirectorPayCardText.Note(state, null)).ShouldBe(states.Select(DirectorPayCardText.Surcharge));
    }

    [Fact]
    public void DirectorPayCardResx_everyNote_hasAText()
    {
        var more = new MoreBy(new DateOnly(2026, 12, 21), 1m);
        var notes = Enum.GetValues<TaxSurchargeState>().SelectMany(state => new[] { DirectorPayCardText.Note(state, null), DirectorPayCardText.Note(state, more) });

        Resources.Missing(notes).ShouldBeEmpty();
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
