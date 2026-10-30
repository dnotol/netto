using System.Net;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Rules;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Tests.Rules;
using Netto.Web.Client.River;
using Netto.Web.Client.Steps;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Steps;

// The engine leaves a citation's words to the client (Source): its title, its name in a list and what
// its passage says, in Steps/SourceText.resx by its document and passage (SourceText). Every source of
// every rule set has them, and every text there belongs to one.
public class SourceTextTests
{
    private static readonly ResxText English = ResxText.For<SourceText>();

    private static readonly ResxText French = ResxText.French<SourceText>();

    // Each language's texts, read alone: a French text missing reads as missing, not as its English.
    private static readonly Dictionary<string, ResxText> Languages = new() { ["English"] = English, ["French"] = French };

    public static TheoryData<string> LanguageNames => [.. Languages.Keys];

    private static readonly Source Article157 =
        Source.Fisconet("personal-tax", "157", 2026, new DateOnly(2026, 7, 30), "c6e47940-53f0-4608-b567-ae774014a621", "surcharged_tax");

    [Fact]
    public void Words_fisconetArticle_readItsCoordinatedTextAndItsArticle()
    {
        var words = SourceText.Words(Article157, English.Get);

        (words.Title, words.Label).ShouldBe((
            "CIR 92, art. 157 (revenus 2026), coordinated text on Fisconetplus, last modified 30.07.2026",
            "CIR 92, art. 157"));
    }

    // A citation's dates identify its document: they read dd.MM.yyyy in French too.
    [Fact]
    public void Words_fisconetArticleInFrench_readItsCoordinatedTextAndItsArticle()
    {
        var words = SourceText.Words(Article157, French.Get);

        (words.Title, words.Label).ShouldBe((
            "CIR 92, art. 157 (revenus 2026), texte coordonné sur Fisconetplus, dernière modification le 30.07.2026",
            "CIR 92, art. 157"));
    }

    // In French a document is named by its official French name.
    [Fact]
    public void Words_documentInFrench_isNamedByItsOfficialName()
    {
        var circular = RentRules.IncomeYear2026.Conditions.Single(condition => condition.Key == "not_with_office_allowance").Sources.Single();

        var words = SourceText.Words(circular, French.Get);

        (words.Title, words.Label).ShouldBe((
            "Circulaire 2021/C/20 du 26.02.2021 relative aux interventions de l'employeur pour le télétravail, sur Fisconetplus, dernière modification le 09.09.2026",
            "Circulaire 2021/C/20, §31"));
    }

    [Fact]
    public void Words_articleOfTheRoyalDecree_readTheArCir92()
    {
        var source = Source.Fisconet("personal-tax", "18", 2026, new DateOnly(2026, 8, 10), "f3ebfcea-9d61-4d4a-bd76-33f13ef93825", "reference_emission", FisconetCode.ArCir92);

        var words = SourceText.Words(source, English.Get);

        (words.Title, words.Label).ShouldBe((
            "AR/CIR 92, art. 18 (revenus 2026), coordinated text on Fisconetplus, last modified 10.08.2026",
            "AR/CIR 92, art. 18"));
    }

    // INASTI's leaflet has no shorter name: a list gives its title.
    [Fact]
    public void Words_documentWithNoShorterName_isNamedByItsTitle()
    {
        var words = SourceText.Words(SocialContributionRules.IncomeYear2026.Source, English.Get);

        (words.Title, words.Label).ShouldBe(("INASTI, « Cotisations 2026 » (January 2026)", "INASTI, « Cotisations 2026 » (January 2026)"));
    }

    // The rent cites one paragraph of the telework circular, named with it.
    [Fact]
    public void Words_passageNamedOtherwiseThanItsDocument_isNamedByItsOwnName()
    {
        var circular = RentRules.IncomeYear2026.Conditions.Single(condition => condition.Key == "not_with_office_allowance").Sources.Single();

        var words = SourceText.Words(circular, English.Get);

        (words.Label, words.Reference).ShouldBe((
            "Circular 2021/C/20, §31",
            "§31: the flat-rate office allowance covers, among others, the use of an office space in the home, its rent included"));
    }

    // The surcharge's passage states art. 468's rule too, read from art. 466's record: it names both.
    [Fact]
    public void Words_passageStatingAnotherArticle_isNamedByBoth()
    {
        (SourceText.Words(MunicipalRates.IncomeYear2026.Surcharge, English.Get).Label, SourceText.Words(MunicipalRates.IncomeYear2026.Surcharge, French.Get).Label)
            .ShouldBe(("CIR 92, art. 466 and 468", "CIR 92, art. 466 et 468"));
    }

    [Fact]
    public void Words_sourceWithNoText_readTheKeys()
    {
        var source = new Source("nowhere", new Uri("https://example.org/"), "personal-tax/none.html");

        var words = SourceText.Words(source, English.Get);

        words.ShouldBe(new SourceWords("Title_personal-tax/none.html", "Title_personal-tax/none.html", "Reference_personal-tax/none.html#nowhere"));
    }

    // A source of a rule set with no words would show its key under a figure, or its English on a French page.
    [Theory]
    [MemberData(nameof(LanguageNames))]
    public void SourceTextResx_everySourceOfEveryRuleSet_hasItsWords(string language)
    {
        var sources = RuleSets.Sources();

        var missing = Languages[language].Missing(sources.SelectMany(citation => SourceText.Required(citation.Source)));

        sources.Count.ShouldBeGreaterThan(100);
        missing.ShouldBeEmpty();
    }

    // A text no source of a rule set reads is a citation left behind, or a key misspelt.
    [Theory]
    [MemberData(nameof(LanguageNames))]
    public void SourceTextResx_everyText_isReadBySomeSource(string language)
    {
        var read = RuleSets.Sources().SelectMany(citation => SourceText.Required(citation.Source).Concat(SourceText.Optional(citation.Source))).ToHashSet();

        Languages[language].Keys.Where(key => !read.Contains(key)).ShouldBeEmpty();
    }

    // A document has one name in a list, the passage it cites at most following it (« Circular 2021/C/20,
    // §31 »), so the citations of one text read as one.
    [Theory]
    [MemberData(nameof(LanguageNames))]
    public void Labels_ofOneDocument_areItsNameOrItsNameAndAPassage(string language)
    {
        var mismatched =
            from citation in RuleSets.Sources()
            group SourceText.Words(citation.Source, Languages[language].Get).Label by citation.Source.Document into document
            let name = document.OrderBy(label => label.Length).First()
            where document.Any(label => label != name && !label.StartsWith(name + ", ", StringComparison.Ordinal))
            select $"{document.Key}: {string.Join(" | ", document.Distinct())}";

        mismatched.ShouldBeEmpty();
    }

    // A lever's conditions cite their texts by name, each a link carrying the text's full title.
    [Fact]
    public async Task ConditionList_sources_areNamedAndTitledByTheirWords()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("conditions", true);
        var article = VoucherRules.IncomeYear2026.Conditions[0].Sources[0];
        var words = SourceText.Words(article, English.Get);

        var html = WebUtility.HtmlDecode(await RenderAsync<ConditionList>(folds, new() { ["Name"] = "conditions", ["Conditions"] = VoucherRules.IncomeYear2026.Conditions }));

        (words.Label, html.Contains($"title=\"{words.Title}\"", StringComparison.Ordinal), html.Contains($"{words.Label}<", StringComparison.Ordinal))
            .ShouldBe(("CIR 92, art. 38/1", true, true));
    }

    // On a French page the same links read in French.
    [Fact]
    public async Task ConditionList_sourcesOnAFrenchPage_areNamedAndTitledInFrench()
    {
        using var french = InLanguage.French();
        var folds = new Folds(new BrowserStorage());
        folds.Set("conditions", true);
        var question = VoucherRules.IncomeYear2026.Conditions.Single(condition => condition.Key == "days_actually_worked").Sources[1];
        var words = SourceText.Words(question, French.Get);

        var html = WebUtility.HtmlDecode(await RenderAsync<ConditionList>(folds, new() { ["Name"] = "conditions", ["Conditions"] = VoucherRules.IncomeYear2026.Conditions }));

        (words.Label, html.Contains($"title=\"{words.Title}\"", StringComparison.Ordinal), html.Contains($"{words.Label}<", StringComparison.Ordinal))
            .ShouldBe(("Question n° 15220", true, true));
    }
}

// A source's words as an English page reads them, for the rule sets' tests to name the texts a rule cites.
internal static class SourceWordsInEnglish
{
    private static readonly ResxText English = ResxText.For<SourceText>();

    public static SourceWords Of(Source source) => SourceText.Words(source, English.Get);
}
