using System.Reflection;
using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Tests.WebClient.River;

// Every refusal the engine declares, read off it by reflection: each static Refusal field of Netto.Core,
// once per refusal, since YearSimulation passes GrossToNet's IncomeYearNotSupported on as its own. Only
// Core declares one (Refusal's constructor is internal), the car card's private cost among them, so a
// refusal the engine adds is in every test that reads these, with no list to keep by hand.
internal static class EngineRefusals
{
    public static IReadOnlyList<Refusal> All { get; } =
    [
        .. typeof(YearSimulation).Assembly.GetTypes()
            .SelectMany(DeclaredBy)
            .Distinct()
            .OrderBy(refusal => refusal.Key, StringComparer.Ordinal),
    ];

    // The refusals the type declares itself.
    public static IEnumerable<Refusal> DeclaredBy(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(Refusal))
            .Select(field => field.GetValue(null))
            .OfType<Refusal>();
}
