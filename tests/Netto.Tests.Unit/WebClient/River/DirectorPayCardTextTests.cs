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

    // Director pay says how a car's benefit counts in the pay.
    [Fact]
    public void DirectorPayCardResx_benefitInThePay_hasItsText()
    {
        Missing(["Benefit"]).ShouldBeEmpty();
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

    [Fact]
    public void DirectorPayCardResx_withholdingDerivedFromThePaySlip_marksTheKeyFormula()
    {
        Terms(["WithheldDerived"]).ShouldBe(["key formula"]);
    }

    [Fact]
    public void DirectorPayCardResx_keyFormula_saysWhatItMeans()
    {
        Missing([DirectorPayCardText.KeyFormulaTerm]).ShouldBeEmpty();
    }

    // The term each sentence marks, null where its text marks none or is missing.
    private static List<string?> Terms(IEnumerable<string> keys) =>
        [.. keys.Select(key => Resources.GetString(key, CultureInfo.InvariantCulture) is { } text ? TermText.Split(text).Term : null)];

    private static IEnumerable<string> Missing(IEnumerable<string> keys) =>
        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
