using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Web.Client.DirectorPay;
using Shouldly;

namespace Netto.Tests.WebClient.DirectorPay;

// The picker and the rate field on /plan, on income year 2026's rates (Namur 8.5%, Liège 8.0%,
// borrowed from income year 2025) and, for the search order and for a year whose own rates are
// published, on made-up municipalities.
public class MunicipalityChoiceTests
{
    private static readonly Source AnySource = new("Title", "Reference", new Uri("https://example.org"));

    private static readonly Municipality Namur = Named("Namur");

    private static readonly Municipality Liege = Named("Liège");

    [Fact]
    public void Start_exampleRate_isComputedAsAnEnteredRate()
    {
        var choice = Start();

        (choice.Municipality, choice.MunicipalRate).ShouldBe((null, MunicipalRates.Entered(0.07m)));
    }

    [Fact]
    public void Start_exampleRate_saysItIsAnExample()
    {
        var note = Start().Note;

        (note.Key, string.Join('|', note.Args)).ShouldBe(("RateNote_example", ""));
    }

    [Fact]
    public void Pick_municipality_takesItsOfficialRate()
    {
        var choice = Start().Pick(Namur);

        (choice.Municipality, choice.Rate, choice.MunicipalRate).ShouldBe((Namur, 0.085m, Namur.Rate));
    }

    [Fact]
    public void Pick_municipality2026_saysTheRateIsIncome2025sAnd2026sIsNotPublished()
    {
        var note = Start().Pick(Namur).Note;

        (note.Key, string.Join('|', note.Args)).ShouldBe(("RateNote_official_earlier", "Namur|2025|2026"));
    }

    [Fact]
    public void Pick_municipalityOfAYearWhoseRatesArePublished_saysItIsItsOfficialRate()
    {
        var alpha = new Municipality(10_001, "Alpha", new RuleAmount("municipal_rate", 0.08m, AnySource));
        var rates = new MunicipalRates(2026, 2026, new DateOnly(2027, 1, 1), [alpha]);

        var note = MunicipalityChoice.Start(rates, 0.07m).Pick(alpha).Note;

        (note.Key, string.Join('|', note.Args)).ShouldBe(("RateNote_official", "Alpha|2026"));
    }

    [Fact]
    public void Enter_afterPicking_overridesTheMunicipalityRate()
    {
        var choice = Start().Pick(Namur).Enter(0.09m);

        (choice.Municipality, choice.IsOverridden, choice.MunicipalRate).ShouldBe((Namur, true, MunicipalRates.Entered(0.09m)));
    }

    [Fact]
    public void Enter_afterPicking_saysWhichOfficialRateItReplaces()
    {
        var note = Start().Pick(Namur).Enter(0.09m).Note;

        (note.Key, string.Join('|', note.Args)).ShouldBe(("RateNote_overridden", "Namur|8.5%"));
    }

    [Fact]
    public void Enter_theMunicipalityRateAfterOverriding_isTheOfficialRateAgain()
    {
        var choice = Start().Pick(Namur).Enter(0.09m).Enter(0.085m);

        (choice.IsOverridden, choice.MunicipalRate).ShouldBe((false, Namur.Rate));
    }

    [Fact]
    public void UseOfficialRate_afterOverriding_restoresTheMunicipalityRate()
    {
        var choice = Start().Pick(Namur).Enter(0.09m).UseOfficialRate();

        (choice.Municipality, choice.Rate, choice.MunicipalRate).ShouldBe((Namur, 0.085m, Namur.Rate));
    }

    [Fact]
    public void Pick_anotherMunicipalityAfterOverriding_takesItsOfficialRate()
    {
        var choice = Start().Pick(Namur).Enter(0.09m).Pick(Liege);

        (choice.Municipality, choice.IsOverridden, choice.MunicipalRate).ShouldBe((Liege, false, Liege.Rate));
    }

    [Fact]
    public void Pick_nothingAfterPicking_keepsTheRateAsEntered()
    {
        var choice = Start().Pick(Namur).Pick(null);

        (choice.Municipality, choice.MunicipalRate).ShouldBe((null, MunicipalRates.Entered(0.085m)));
    }

    [Fact]
    public void Enter_withoutMunicipality_saysTheRateIsTheUsers()
    {
        var note = Start().Enter(0.075m).Note;

        (note.Key, string.Join('|', note.Args)).ShouldBe(("RateNote_entered", ""));
    }

    [Fact]
    public void Pick_nothingFromTheStart_keepsTheExample()
    {
        var choice = Start().Pick(null);

        (choice.Rate, choice.Note.Key).ShouldBe((0.07m, "RateNote_example"));
    }

    // Folded to compare: alpha beta, ehein, ete, zeta.
    [Fact]
    public void Search_emptyText_listsEveryMunicipalityInNameOrderIgnoringAccents()
    {
        var choice = MunicipalityChoice.Start(MadeUpRates("Zeta", "Été", "Alpha-Bêta", "Ehein"), 0.07m);

        var found = choice.Search("");

        found.Select(municipality => municipality.Name).ShouldBe(["Alpha-Bêta", "Ehein", "Été", "Zeta"]);
    }

    [Fact]
    public void Search_partOfTheName_findsEveryMunicipalityContainingIt()
    {
        var choice = MunicipalityChoice.Start(MadeUpRates("Saint-Alpha", "Beta-Saint", "Gamma"), 0.07m);

        var found = choice.Search("saint");

        found.Select(municipality => municipality.Name).ShouldBe(["Beta-Saint", "Saint-Alpha"]);
    }

    [Theory]
    [InlineData("liege", "Liège")]
    [InlineData("LIÈGE", "Liège")]
    [InlineData("La Bruyère", "La Bruyere")]
    [InlineData("braine l alleud", "Braine-l'Alleud")]
    [InlineData("  court saint  etienne ", "Court-Saint-Etienne")]
    public void Search_textDifferingInCaseAccentsOrPunctuation_findsTheMunicipality(string text, string name)
    {
        var found = Start().Search(text);

        found.Select(municipality => municipality.Name).ShouldContain(name);
    }

    [Fact]
    public void Search_textNoNameContains_findsNothing()
    {
        var found = Start().Search("Antwerpen");

        found.ShouldBeEmpty();
    }

    private static MunicipalityChoice Start() => MunicipalityChoice.Start(MunicipalRates.IncomeYear2026, 0.07m);

    private static Municipality Named(string name) =>
        MunicipalRates.IncomeYear2026.Municipalities.Single(municipality => municipality.Name == name);

    private static MunicipalRates MadeUpRates(params string[] names) =>
        new(2026, 2025, new DateOnly(2027, 1, 1),
            names.Select((name, index) => new Municipality(10_000 + index, name, new RuleAmount("municipal_rate", 0.08m, AnySource))).ToList());
}
