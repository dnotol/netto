using System.Reflection;
using Netto.Core.Simulation;

namespace Netto.Tests.WebClient.River;

// Every refusal key the engine returns, read off it by reflection: each public const string Netto.Core
// declares, once per key, since YearSimulation passes GrossToNet's IncomeYearNotSupported on as its own.
// A key the engine adds is in every test that reads these, with no list to keep by hand.
internal static class EngineRefusals
{
    public static IReadOnlyList<string> Keys { get; } =
    [
        .. typeof(YearSimulation).Assembly.GetTypes()
            .SelectMany(DeclaredBy)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal),
    ];

    // The keys the type declares itself.
    public static IEnumerable<string> DeclaredBy(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.IsLiteral)
            .Select(field => field.GetRawConstantValue())
            .OfType<string>();
}
