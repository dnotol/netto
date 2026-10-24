using System.Reflection;

namespace Netto.Tests.Rules;

// How the rules tests reach a rule set: its type's For(year), asked for every year it might have rules
// for, and the properties a rules record holds its amounts, its sources and its nested records in.
internal static class RuleSets
{
    // The income years asked, wide enough to find a rule set for any year Netto will cover.
    public static IReadOnlyList<int> Years { get; } = [.. Enumerable.Range(2000, 101)];

    // The type's rules for the year, through its For(int), or null when it has none.
    public static object? For(Type type, int year) =>
        (type.GetMethod("For", BindingFlags.Public | BindingFlags.Static, [typeof(int)])
            ?? throw new InvalidOperationException($"{type.Name} has no For(int).")).Invoke(null, [year]);

    // Each income year's rules the type has, with its year.
    public static IEnumerable<(int Year, object Rules)> Of(Type type)
    {
        foreach (var year in Years)
        {
            if (For(type, year) is { } rules)
                yield return (year, rules);
        }
    }

    // The public instance properties of the type, indexers aside.
    public static IEnumerable<PropertyInfo> Properties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0);
}
