using System.Collections;
using System.Reflection;
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

    // The rule sets: each type of Netto.Core whose For(year) returns one of its own, one per layer
    // and income year, so a new layer is guarded as soon as it has one.
    private static readonly IReadOnlyList<Type> RuleTypes = Core.GetTypes()
        .Where(type => type.GetMethod("For", BindingFlags.Public | BindingFlags.Static, [typeof(int)])?.ReturnType == type)
        .ToList();

    private static readonly IReadOnlyList<int> Years = Enumerable.Range(2000, 101).ToList();

    // The income years a year's steps can run for: those every layer has rules for. A step cites a
    // source that is not a year's own (PersonalTaxRules.IncomeTaxScale, DividendRules.ExemptionRefund),
    // so that source must hold for each of these years: today 2026 alone.
    private static readonly IReadOnlyList<int> StepYears = Years
        .Where(year => RuleTypes.All(type => RuleSet(type, year) is not null))
        .ToList();

    [Fact]
    public void RuleTypes_core_includeEveryLayer()
    {
        string[] layers = ["SocialContributionRules", "PersonalTaxRules", "MunicipalRates", "CorporateTaxRules", "DividendRules", "WithholdingRules", "VoucherRules", "AllowanceRules", "RentRules", "CarRules", "CopyrightRules"];

        layers.ShouldBeSubsetOf(RuleTypes.Select(type => type.Name));
    }

    [Fact]
    public void StepYears_rules_includeAtLeastOneYear()
    {
        StepYears.ShouldNotBeEmpty();
    }

    [Fact]
    public void RuleSet_everyAmountAndSource_citesADocumentCurrentForItsIncomeYear()
    {
        var citations =
            from type in RuleTypes
            from year in Years
            let rules = RuleSet(type, year)
            where rules is not null
            from citation in Citations(rules, $"{type.Name} {year}")
            select (citation.Path, Problem: Documents.Problem(citation.Source, year));

        Problems(citations).ShouldBeEmpty();
    }

    [Fact]
    public void StepSource_citesADocumentCurrentForEveryYearStepsRunFor()
    {
        var citations =
            from type in Core.GetTypes()
            from property in type.GetProperties(BindingFlags.Public | BindingFlags.Static)
            where property.PropertyType == typeof(Source)
            let source = (Source)property.GetValue(null)!
            from year in StepYears
            select ($"{type.Name}.{property.Name} for {year}", Documents.Problem(source, year));

        Problems(citations).ShouldBeEmpty();
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

    private static object? RuleSet(Type type, int year) =>
        type.GetMethod("For", BindingFlags.Public | BindingFlags.Static, [typeof(int)])!.Invoke(null, [year]);

    // Every source a rule set holds, through its amounts, its lists (the scale, the municipalities)
    // and the records of Netto.Core inside it (LowEarnedIncomeCreditRules), with the path to it.
    private static IEnumerable<(string Path, Source Source)> Citations(object? value, string path) => value switch
    {
        null or string => [],
        Source source => [(path, source)],
        RuleAmount amount => [($"{path} ({amount.Key})", amount.Source)],
        IEnumerable items => items.Cast<object?>().SelectMany((item, index) => Citations(item, $"{path}[{index}]")),
        _ when value.GetType().Assembly == Core => value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0)
            .SelectMany(property => Citations(property.GetValue(value), $"{path}.{property.Name}")),
        _ => [],
    };
}
