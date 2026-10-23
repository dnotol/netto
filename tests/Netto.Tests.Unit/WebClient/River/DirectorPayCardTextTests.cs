using System.Globalization;
using System.Resources;
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
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.DirectorPayCard", typeof(DirectorPayCard).Assembly);

    // Every label and bubble of Director pay's lines: the contributions the company pays, a car's
    // benefit and a royalty's part beyond its limits with the taxable pay they make, and what more by the
    // last deadline leaves no surcharge due.
    [Fact]
    public void DirectorPayCardResx_everyLabelAndBubbleOfItsLines_hasAText()
    {
        var taxed = RiverInputs.Example.AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
            .AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m).WithContributionsPaidBy(ContributionsPaidBy.Company);
        var due = RiverInputs.Example.WithTaxWithheld(0m);
        var lines = DirectorPayFigures.Taxed(taxed.Run().Pay.ShouldNotBeNull())
            .Concat(DirectorPayFigures.Surcharge(DirectorPayView.From(due, due.Run().Pay.ShouldNotBeNull())));

        Missing(lines.SelectMany(line => new[] { line.Key, line.Help }).OfType<string>()).ShouldBeEmpty();
    }

    [Fact]
    public void DirectorPayCardResx_everyTaxSurchargeState_hasAText()
    {
        Missing(Enum.GetValues<TaxSurchargeState>().Select(DirectorPayCardText.Surcharge)).ShouldBeEmpty();
    }

    [Fact]
    public void DirectorPayCardResx_everyPayerOfTheContributions_hasAText()
    {
        Missing(Enum.GetValues<ContributionsPaidBy>().Select(DirectorPayCardText.PaidBy)).ShouldBeEmpty();
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
        ];
        var phrases = variations.Select(inputs => DirectorPaySettingsView.From(inputs, inputs.Run()))
            .SelectMany(view => view.Situation.Concat(view.Prepaid));

        Missing(phrases.Select(phrase => phrase.Key)).ShouldBeEmpty();
    }

    // How the withholding is derived and what the key formula is are said in the withheld field's
    // bubble, which no term can open inside: its text marks none.
    [Fact]
    public void DirectorPayCardResx_withheldFieldsBubble_marksNoTerm()
    {
        var text = Resources.GetString("WithheldHint", CultureInfo.InvariantCulture).ShouldNotBeNull();

        TermText.Split(text).Term.ShouldBeNull();
    }

    // What a figure cannot say stays a sentence of fifteen words at most: why no surcharge is due.
    [Fact]
    public void DirectorPayCardResx_everySentenceItKeeps_isAtMostFifteenWords()
    {
        var kept = InterfaceTexts.Of(typeof(DirectorPayCard)).Where(text => text.Key.StartsWith("Surcharge_", StringComparison.Ordinal)).ToList();

        kept.ShouldSatisfyAllConditions(
            () => kept.ShouldNotBeEmpty(),
            () => kept.Where(text => InterfaceTexts.WordCount(text.Value) > 15).Select(text => text.Key).ShouldBeEmpty());
    }

    private static IEnumerable<string> Missing(IEnumerable<string> keys) =>
        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
