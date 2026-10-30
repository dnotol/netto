using System.Reflection;
using System.Text.RegularExpressions;
using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.Rules;

// A rule set cites only documents current for its income year (CLAUDE.md): every amount of every
// rule set, and every source the steps cite, names a document filed in official-docs/ that INDEX.md
// lists, at the same URL, as current for that income year.
public class SourceDocumentTests
{
    private static readonly Assembly Core = typeof(Source).Assembly;

    private static readonly OfficialDocumentIndex Documents = OfficialDocumentIndex.Load();

    // The rule sets: each type of Netto.Core whose For(year), or For(year, region), returns one of its
    // own, one per layer, income year and region, so a new layer is guarded as soon as it has one.
    private static readonly IReadOnlyList<Type> RuleTypes = Core.GetTypes()
        .Where(type => RuleSets.Lookup(type) is not null)
        .ToList();

    [Fact]
    public void RuleTypes_core_includeEveryLayer()
    {
        string[] layers = ["SocialContributionRules", "CompanyContributionRules", "PersonalTaxRules", "MunicipalRates", "CorporateTaxRules", "DividendRules", "WithholdingRules", "VoucherRules", "AllowanceRules", "RentRules", "CarRules", "CopyrightRules"];

        layers.ShouldBeSubsetOf(RuleTypes.Select(type => type.Name));
    }

    // Every layer's lookup yields a rule set for some year, or the citation tests below would hold nothing.
    [Fact]
    public void RuleSets_everyRuleType_yieldsAtLeastOneSet()
    {
        var empty = RuleTypes.Where(type => !RuleSets.Of(type).Any()).Select(type => type.Name);

        empty.ShouldBeEmpty();
    }

    [Fact]
    public void RuleSet_everyAmountAndSource_citesADocumentCurrentForItsIncomeYear()
    {
        var citations =
            from type in RuleTypes
            from set in RuleSets.Of(type)
            from citation in RuleSets.Citations(set.Rules, $"{type.Name} {set.Year}")
            select (citation.Path, Problem: Documents.Problem(citation.Source, set.Year));

        Problems(citations).ShouldBeEmpty();
    }

    // A document several rule sets cite is one record (SharedSources), each rule set taking the passage
    // it rests on: every citation of a file has its title and its address, and one name in a list, or
    // that name followed by the paragraph it cites (« Circular 2021/C/20, §31 »).
    [Fact]
    public void Citations_ofOneDocument_shareItsTitleAddressAndName()
    {
        var mismatched =
            from type in RuleTypes
            from set in RuleSets.Of(type)
            from citation in RuleSets.Citations(set.Rules, $"{type.Name} {set.Year}")
            group citation.Source by citation.Source.Document into document
            let name = document.Select(source => source.Label).OrderBy(label => label.Length).First()
            where document.Select(source => (source.Title, source.Url)).Distinct().Count() > 1
                || document.Any(source => source.Label != name && !source.Label.StartsWith(name + ", ", StringComparison.Ordinal))
            select $"{document.Key}: {string.Join(" | ", document.Select(source => source.Label).Distinct())}";

        mismatched.ShouldBeEmpty();
    }

    // An article of CIR 92 or of the AR/CIR 92 is built by Source.Fisconet, which files it by its article
    // and income year and words its title and its name in a list the same way for every article.
    [Fact]
    public void Citations_ofACir92Article_areBuiltByFisconet()
    {
        var unbuilt = RuleSets.Sources()
            .Where(citation => IsCodeArticle(citation.Source.Document) && citation.Source.Article is null)
            .Select(citation => $"{citation.Path}: {citation.Source.Document}");

        unbuilt.ShouldBeEmpty();
    }

    // A source a step cites is its income year's rule set's, held above to that year's documents, so a
    // new income year is a data drop: Netto.Core keeps no source in a public static member of a public
    // type, which every year would share.
    [Fact]
    public void StaticSources_core_noneIsPublic()
    {
        var statics =
            from type in Core.GetTypes()
            where type.IsVisible
            from member in type.GetMembers(BindingFlags.Public | BindingFlags.Static)
            where (member is PropertyInfo property && property.PropertyType == typeof(Source))
                || (member is FieldInfo field && field.FieldType == typeof(Source))
            select $"{type.Name}.{member.Name}";

        statics.ShouldBeEmpty();
    }

    [Fact]
    public void Index_everyListedFile_isInOfficialDocs()
    {
        var missing = Documents.Files.Where(file => !File.Exists(Path.Combine(OfficialDocumentIndex.Folder.FullName, file)));

        missing.ShouldBeEmpty();
    }

    // Each problem once, at the first place it was found: a document every municipality's rate cites
    // is one problem, not two hundred.
    private static List<string> Problems(IEnumerable<(string Path, string? Problem)> citations) =>
        citations
            .Where(citation => citation.Problem is not null)
            .GroupBy(citation => citation.Problem!)
            .Select(group => group.Count() == 1
                ? $"{group.First().Path}: {group.Key}"
                : $"{group.First().Path} and {group.Count() - 1} more: {group.Key}")
            .ToList();

    // A file of official-docs/ that is an article of CIR 92 or AR/CIR 92 itself, a commentary on one aside.
    private static bool IsCodeArticle(string document) =>
        Regex.IsMatch(document, @"/spf-finances_(ar)?cir92-art-");
}
