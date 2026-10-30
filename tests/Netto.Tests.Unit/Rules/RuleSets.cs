using System.Collections;
using System.Reflection;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;

namespace Netto.Tests.Rules;

// How the rules tests reach a rule set: its type's For(year), or For(year, region) for a layer whose
// rules hang off where the taxpayer lives, asked for every year it might have rules for and every
// region, and the properties a rules record holds its amounts, its sources and its nested records in.
internal static class RuleSets
{
    private static readonly Type[][] Lookups = [[typeof(int)], [typeof(int), typeof(Region)]];

    // The income years asked, wide enough to find a rule set for any year Netto will cover.
    public static IReadOnlyList<int> Years { get; } = [.. Enumerable.Range(2000, 101)];

    // The type's For(int) or For(int, Region) that returns one of its own, or null when it has neither:
    // the mark of a rule set.
    public static MethodInfo? Lookup(Type type) =>
        Lookups.Select(parameters => type.GetMethod("For", BindingFlags.Public | BindingFlags.Static, parameters))
            .FirstOrDefault(method => method?.ReturnType == type);

    // The type's rules for the year, through its For(int), or one for each region that has some through
    // its For(int, Region): none when it has no rules for the year.
    public static IEnumerable<object> For(Type type, int year)
    {
        var lookup = Lookup(type) ?? throw new InvalidOperationException($"{type.Name} has no For(int) nor For(int, Region).");
        IEnumerable<object?[]> arguments = lookup.GetParameters().Length == 1 ? [[year]] : Enum.GetValues<Region>().Select(region => new object?[] { year, region });

        return arguments.Select(argument => lookup.Invoke(null, argument)).OfType<object>();
    }

    // Each income year's rules the type has, with its year.
    public static IEnumerable<(int Year, object Rules)> Of(Type type) =>
        from year in Years
        from rules in For(type, year)
        select (year, rules);

    // The public instance properties of the type, indexers aside.
    public static IEnumerable<PropertyInfo> Properties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0);

    // What each property of a rules record that holds a T holds, by the property's name, in the order the
    // record declares them: its amounts or its sources, so a test can hold each slot to its key or its
    // document and a swap of two neighbours fails.
    public static IEnumerable<(string Slot, T? Value)> Slots<T>(object record) where T : class =>
        Properties(record.GetType()).Where(property => property.PropertyType == typeof(T)).Select(property => (property.Name, property.GetValue(record) as T));

    // Every type of Netto.Core whose For(year), or For(year, region), returns one of its own: one per layer.
    public static IReadOnlyList<Type> Types { get; } = [.. typeof(Source).Assembly.GetTypes().Where(type => Lookup(type) is not null)];

    // Every source a rule set holds, through its amounts, its lists (the scale, the municipalities)
    // and the records of Netto.Core inside it (LowEarnedIncomeCreditRules), with the path to it.
    public static IEnumerable<(string Path, Source Source)> Citations(object? value, string path) => value switch
    {
        null or string => [],
        Source source => [(path, source)],
        RuleAmount amount => [($"{path} ({amount.Key})", amount.Source)],
        IEnumerable items => items.Cast<object?>().SelectMany((item, index) => Citations(item, $"{path}[{index}]")),
        _ when value.GetType().Assembly == typeof(Source).Assembly => Properties(value.GetType())
            .SelectMany(property => Citations(property.GetValue(value), $"{path}.{property.Name}")),
        _ => [],
    };

    // Every source of every rule set of every year, each once, with the first path it was found at.
    public static IReadOnlyList<(string Path, Source Source)> Sources() =>
        [.. (from type in Types
             from set in Of(type)
             from citation in Citations(set.Rules, $"{type.Name} {set.Year}")
             select citation)
            .DistinctBy(citation => citation.Source)];
}
