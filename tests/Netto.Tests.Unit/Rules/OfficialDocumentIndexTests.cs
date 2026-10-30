using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.Rules;

// The reader of official-docs/INDEX.md, on a made-up index: which income years may cite a document,
// and why one may not.
public class OfficialDocumentIndexTests
{
    private const string Index = """
        # Index of official documents

        | File | Issuer | Title | URL | Version | Retrieved | Applies to | Status | Notes |
        |---|---|---|---|---|---|---|---|---|
        | `personal-tax/issuer_article-2025_2025-02-01.html` | Issuer | Article (2025) | [Site](https://example.org/article-2025) | 01.02.2025 | 19.10.2026 | 2025 | `current` | Tax year 2026. |
        | `personal-tax/issuer_article-2026_2026-02-01.html` | Issuer | Article (2026) | [Site](https://example.org/article-2026) | 01.02.2026 | 19.10.2026 | 2026 | `current` | Tax year 2027. |
        | `personal-tax/issuer_decree_2017-12-22.html` | Issuer | Decree | [Site](https://example.org/decree) | 22.12.2017 | 19.10.2026 | 2017 onwards | `current` | Unchanged since. |
        | `personal-tax/issuer_explanations_2026-04-13.pdf` | Issuer | Explanations | [PDF](https://example.org/explanations.pdf) | 13.04.2026 | 19.10.2026 | 2025 to 2026 | `current` | Income year 2026 borrows it. |
        | `social-contributions/issuer_leaflet-2026_2026-01-05.pdf` | Issuer | Leaflet | [PDF](https://example.org/leaflet.pdf) | 05.01.2026 | 19.10.2026 | 2026 | `superseded by social-contributions/issuer_leaflet-2026_2026-06-01.pdf` | First edition. |
        | `social-contributions/issuer_leaflet-2026_2026-06-01.pdf` | Issuer | Leaflet | [PDF](https://example.org/leaflet.pdf) | 01.06.2026 | 19.10.2026 | 2026 | `current` | Same address. |
        | `corporate-tax/issuer_notice-2027_2027-08-01.html` | Issuer | Notice | [Site](https://example.org/notice-2027) | 01.08.2027 | 19.10.2026 | 2027 | `not yet applicable` | Tax year 2028. |
        """;

    private static readonly OfficialDocumentIndex Documents = OfficialDocumentIndex.Parse(Index);

    [Fact]
    public void Files_index_listsEveryRow()
    {
        Documents.Files.Count.ShouldBe(7);
    }

    [Theory]
    [InlineData("personal-tax/issuer_article-2026_2026-02-01.html", "https://example.org/article-2026", 2026)]
    [InlineData("personal-tax/issuer_decree_2017-12-22.html", "https://example.org/decree", 2017)]
    [InlineData("personal-tax/issuer_decree_2017-12-22.html", "https://example.org/decree", 2040)]
    [InlineData("personal-tax/issuer_explanations_2026-04-13.pdf", "https://example.org/explanations.pdf", 2025)]
    [InlineData("personal-tax/issuer_explanations_2026-04-13.pdf", "https://example.org/explanations.pdf", 2026)]
    [InlineData("social-contributions/issuer_leaflet-2026_2026-06-01.pdf", "https://example.org/leaflet.pdf", 2026)]
    public void Problem_currentDocumentForTheYear_isNone(string document, string url, int incomeYear)
    {
        var problem = Documents.Problem(Citing(document, url), incomeYear);

        problem.ShouldBeNull();
    }

    [Theory]
    [InlineData("personal-tax/issuer_article-2025_2025-02-01.html", "https://example.org/article-2025", 2026, "2025")]
    [InlineData("personal-tax/issuer_decree_2017-12-22.html", "https://example.org/decree", 2016, "2017 onwards")]
    [InlineData("personal-tax/issuer_explanations_2026-04-13.pdf", "https://example.org/explanations.pdf", 2024, "2025 to 2026")]
    [InlineData("personal-tax/issuer_explanations_2026-04-13.pdf", "https://example.org/explanations.pdf", 2027, "2025 to 2026")]
    public void Problem_documentForOtherYears_namesTheYearsItAppliesTo(string document, string url, int incomeYear, string appliesTo)
    {
        var problem = Documents.Problem(Citing(document, url), incomeYear);

        problem.ShouldBe($"{document} applies to {appliesTo}, not to income year {incomeYear}");
    }

    [Fact]
    public void Problem_supersededDocument_namesWhatSupersedesIt()
    {
        const string Document = "social-contributions/issuer_leaflet-2026_2026-01-05.pdf";

        var problem = Documents.Problem(Citing(Document, "https://example.org/leaflet.pdf"), 2026);

        problem.ShouldBe($"{Document} is superseded by social-contributions/issuer_leaflet-2026_2026-06-01.pdf");
    }

    [Fact]
    public void Problem_documentNotYetApplicable_saysSo()
    {
        const string Document = "corporate-tax/issuer_notice-2027_2027-08-01.html";

        var problem = Documents.Problem(Citing(Document, "https://example.org/notice-2027"), 2027);

        problem.ShouldBe($"{Document} is not yet applicable");
    }

    [Theory]
    [InlineData("personal-tax/issuer_article-2027_2027-02-01.html")]
    [InlineData("")]
    public void Problem_documentNotInTheIndex_saysItIsNotFiled(string document)
    {
        var problem = Documents.Problem(Citing(document, "https://example.org/article-2027"), 2027);

        problem.ShouldBe($"« {document} » is not listed in official-docs/INDEX.md");
    }

    [Fact]
    public void Problem_urlOtherThanTheIndexOne_namesBoth()
    {
        const string Document = "personal-tax/issuer_article-2026_2026-02-01.html";

        var problem = Documents.Problem(Citing(Document, "https://example.org/article-2025"), 2026);

        problem.ShouldBe($"{Document} is cited at https://example.org/article-2025, but INDEX.md gives https://example.org/article-2026");
    }

    [Theory]
    [InlineData("Income year 2026")]
    [InlineData("2026-2027")]
    [InlineData("2027 to 2026")]
    [InlineData("from 2017")]
    [InlineData("")]
    public void Parse_appliesToNotAYearOrARange_throwsNamingTheRow(string appliesTo)
    {
        var index = Index.Replace("| 2017 onwards |", $"| {appliesTo} |", StringComparison.Ordinal);

        var error = Should.Throw<FormatException>(() => OfficialDocumentIndex.Parse(index));

        error.Message.ShouldContain("personal-tax/issuer_decree_2017-12-22.html");
    }

    [Fact]
    public void Parse_indexWithoutAnAppliesToColumn_throws()
    {
        var index = Index.Replace("| Applies to |", "| Years |", StringComparison.Ordinal);

        var error = Should.Throw<FormatException>(() => OfficialDocumentIndex.Parse(index));

        error.Message.ShouldContain("Applies to");
    }

    private static Source Citing(string document, string url) => new("any_passage", new Uri(url), document);
}
