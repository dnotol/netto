using System.Reflection;
using Netto.Web.Client.River;

namespace Netto.Tests.WebClient.River;

// Every input of the river, one at a time given another value, walked as the river's records take them in
// their constructors (RiverInputsShapeTests), so an input added to the river is walked too: a test that
// holds each input to something (RiverDifferencesTests) fails on the new one until it is. An input is an
// Input<T>, or another record of the river that takes an origin (MunicipalityChoice, whose value is its
// rate); a bool or an int a record holds besides, a composition's flags and the income year, is one too.
// The engine's own records, a municipality's rates among them, are no input of the river.
internal static class RiverLeaves
{
    // The river with the input at that path given another value, and with its origin changed alone, none
    // for an input that has no origin.
    internal sealed record Leaf(string Path, RiverInputs WithAnotherValue, RiverInputs? WithAnotherOrigin);

    public static IReadOnlyList<Leaf> Of(RiverInputs river) =>
        [.. Walk(river, "").Select(leaf => new Leaf(leaf.Path, (RiverInputs)leaf.Value, (RiverInputs?)leaf.Origin))];

    private static IEnumerable<(string Path, object Value, object? Origin)> Walk(object record, string path)
    {
        foreach (var parameter in RiverInputsShapeTests.Parameters(record.GetType()))
        {
            var current = Read(record, parameter);
            var at = path.Length == 0 ? Name(parameter) : $"{path}.{Name(parameter)}";

            if (current is not null && TakesAnOrigin(current.GetType()))
            {
                yield return (at, Rebuilt(record, parameter, WithAnotherValue(current)), Rebuilt(record, parameter, WithAnotherOrigin(current)));
            }
            else if (current is not null && IsARecordOfTheRiver(current.GetType()))
            {
                foreach (var (childPath, value, origin) in Walk(current, at))
                    yield return (childPath, Rebuilt(record, parameter, value), origin is null ? null : Rebuilt(record, parameter, origin));
            }
            else if (current is bool or int)
            {
                yield return (at, Rebuilt(record, parameter, Another(current)), null);
            }
        }
    }

    private static bool IsARecordOfTheRiver(Type type) =>
        type.IsClass && type.Assembly == typeof(RiverInputs).Assembly && type.GetConstructors().Length == 1;

    private static bool TakesAnOrigin(Type type) =>
        IsARecordOfTheRiver(type) && RiverInputsShapeTests.Parameters(type).Any(parameter => parameter.ParameterType == typeof(InputOrigin));

    // The value an input of the river holds beside its origin: the one value-typed parameter that is no origin.
    private static ParameterInfo ValueOf(Type input) =>
        RiverInputsShapeTests.Parameters(input).Single(parameter => parameter.ParameterType.IsValueType && parameter.ParameterType != typeof(InputOrigin));

    private static ParameterInfo OriginOf(Type input) =>
        RiverInputsShapeTests.Parameters(input).Single(parameter => parameter.ParameterType == typeof(InputOrigin));

    private static object WithAnotherValue(object input)
    {
        var value = ValueOf(input.GetType());
        return Rebuilt(input, value, Another(Read(input, value), value.ParameterType));
    }

    private static object WithAnotherOrigin(object input)
    {
        var origin = OriginOf(input.GetType());
        var current = (InputOrigin)Read(input, origin)!;
        return Rebuilt(input, origin, Enum.GetValues<InputOrigin>().First(other => other != current));
    }

    // Another value of the same type, so the engine's figures can tell it from the first.
    private static object Another(object? value) => Another(value, value?.GetType() ?? throw new InvalidOperationException("A value of nothing has no type."));

    private static object Another(object? value, Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying switch
        {
            _ when underlying == typeof(bool) => !(bool)value!,
            _ when underlying == typeof(int) => (int)value! + 1,
            _ when underlying == typeof(decimal) => (value is decimal amount ? amount : 0m) + 1m,
            _ when underlying == typeof(DateOnly) => ((DateOnly)value!).AddDays(1),
            _ when underlying.IsEnum => Enum.GetValues(underlying).Cast<object>().First(other => !other.Equals(value)),
            _ => throw new InvalidOperationException($"No other value of {type.Name} is known."),
        };
    }

    // The record again with one parameter of its constructor given that value.
    private static object Rebuilt(object record, ParameterInfo changed, object value)
    {
        var parameters = RiverInputsShapeTests.Parameters(record.GetType());
        var arguments = parameters.Select(parameter => parameter.Position == changed.Position ? value : Read(record, parameter)).ToArray();

        return record.GetType().GetConstructors().Single().Invoke(arguments);
    }

    // What a record holds for a parameter of its constructor: its property of the same name.
    private static object? Read(object record, ParameterInfo parameter)
    {
        var name = Name(parameter);
        var property = record.GetType().GetProperty(name)
            ?? throw new InvalidOperationException($"{record.GetType().Name} has no property {name} for its constructor's parameter.");
        return property.GetValue(record);
    }

    private static string Name(ParameterInfo parameter) => parameter.Name
        ?? throw new InvalidOperationException($"Parameter {parameter.Position} of {parameter.Member.DeclaringType?.Name}'s constructor has no name.");
}
