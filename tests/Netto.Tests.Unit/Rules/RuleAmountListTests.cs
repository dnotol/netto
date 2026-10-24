using System.Reflection;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.Rules;

// The steps of the pay, the personal tax, the corporate tax and the withholding cite the amounts they used
// through their rule set's lists (Amounts, IncomeTaxAmounts, SurchargeAmounts and the like), which is also
// how SourceDocumentTests reaches them. An amount added to one of these rule records and to none of its
// lists would be computed with, yet cited by no step and held to no document. So every amount such a record
// holds, its nested rules records' included, is in a list of the record or of those nested records. Bands
// and deadlines are records of their own, listed through their rule set (Scale, Deadlines), and not walked.
public class RuleAmountListTests
{
    private static readonly IReadOnlyList<int> Years = Enumerable.Range(2000, 101).ToList();

    public static TheoryData<Type> RuleTypes => new() { typeof(SocialContributionRules), typeof(PersonalTaxRules), typeof(CorporateTaxRules), typeof(WithholdingRules) };

    [Theory]
    [MemberData(nameof(RuleTypes))]
    public void Amounts_everyAmountOfEveryYearsRules_isInOneOfItsLists(Type ruleType)
    {
        var sets = RuleSets(ruleType).ToList();

        var unlisted = sets.SelectMany(set => Unlisted(set.Rules).Select(amount => $"{ruleType.Name} {set.Year}: {amount}")).ToList();

        sets.ShouldNotBeEmpty();
        unlisted.ShouldBeEmpty();
    }

    // The rules records each walk reaches, pinned: a rules record nested in one of these later is walked too,
    // and this says so.
    [Theory]
    [InlineData(typeof(SocialContributionRules), new[] { "SocialContributionRules" })]
    [InlineData(typeof(PersonalTaxRules), new[] { "PersonalTaxRules", "LowEarnedIncomeCreditRules", "IncomeTaxAdvancePaymentRules" })]
    [InlineData(typeof(CorporateTaxRules), new[] { "CorporateTaxRules", "AdvancePaymentRules" })]
    [InlineData(typeof(WithholdingRules), new[] { "WithholdingRules" })]
    public void Records_ruleSet_areItAndItsNestedRulesRecords(Type ruleType, string[] expected)
    {
        var (_, rules) = RuleSets(ruleType).First();

        var records = Records(rules).Select(record => record.GetType().Name);

        records.ShouldBe(expected);
    }

    // Each income year's rules the type has: For(year) for the years SourceDocumentTests reads.
    private static IEnumerable<(int Year, object Rules)> RuleSets(Type ruleType)
    {
        var forYear = ruleType.GetMethod("For", BindingFlags.Public | BindingFlags.Static, [typeof(int)])
            ?? throw new InvalidOperationException($"{ruleType.Name} has no For(int).");
        foreach (var year in Years)
        {
            if (forYear.Invoke(null, [year]) is { } rules)
                yield return (year, rules);
        }
    }

    // The amounts the rule set and its nested rules records hold that none of their lists holds.
    private static IEnumerable<string> Unlisted(object rules)
    {
        var records = Records(rules).ToList();
        var listed = records.SelectMany(Lists).ToHashSet();

        return records.SelectMany(record => Held(record).Where(held => !listed.Contains(held.Amount)).Select(held => $"{record.GetType().Name}.{held.Name}"));
    }

    // The record and, in order, each rules record it holds, theirs after it.
    private static IEnumerable<object> Records(object record) =>
    [
        record,
        .. Properties(record)
            .Where(property => property.PropertyType.Name.EndsWith("Rules", StringComparison.Ordinal) && property.PropertyType.Assembly == typeof(RuleAmount).Assembly)
            .Select(property => property.GetValue(record))
            .OfType<object>()
            .SelectMany(Records),
    ];

    // The amounts the record holds itself.
    private static IEnumerable<(string Name, RuleAmount Amount)> Held(object record)
    {
        foreach (var property in Properties(record))
        {
            if (property.PropertyType == typeof(RuleAmount) && property.GetValue(record) is RuleAmount amount)
                yield return (property.Name, amount);
        }
    }

    // The amounts its lists name: Amounts, and every list whose name ends in Amounts.
    private static IEnumerable<RuleAmount> Lists(object record) =>
        Properties(record)
            .Where(property => property.Name.EndsWith("Amounts", StringComparison.Ordinal) && typeof(IEnumerable<RuleAmount>).IsAssignableFrom(property.PropertyType))
            .SelectMany(property => property.GetValue(record) as IEnumerable<RuleAmount> ?? []);

    private static IEnumerable<PropertyInfo> Properties(object record) =>
        record.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0);
}
