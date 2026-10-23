using System.Reflection;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The example the river opens on gives five things: the company's revenue and running costs, the
// director's pay, and a car's first registration and order, the dates of a new car. Every other amount
// it holds none of, a lever's before the user types it among them: there is nothing to enter over, so
// it names no origin, and an « example » chip stands only beside what the example gives. The inputs
// are walked as the river's records take them in their constructors (RiverInputsShapeTests), so an
// input added to the river is held to this too.
public class ExampleOriginsTests
{
    [Fact]
    public void Example_everyAmountItHoldsNoneOf_namesNoOrigin()
    {
        var nothing = Inputs(RiverInputs.Example, "").Where(input => input.Value is 0m or 0).ToList();

        nothing.ShouldSatisfyAllConditions(
            () => nothing.ShouldNotBeEmpty(),
            () => nothing.Where(input => input.Origin != InputOrigin.None).Select(input => $"{input.Path}: {input.Origin}").ShouldBeEmpty());
    }

    [Fact]
    public void Example_whatItGives_isAllThatNamesTheExample()
    {
        Inputs(RiverInputs.Example, "").Where(input => input.Origin == InputOrigin.Example).Select(input => input.Path)
            .ShouldBe(["Company.Revenue", "Company.RunningCosts", "Car.FirstRegistration", "Car.Acquired", "DirectorPay"]);
    }

    // The municipal rate is an input with an origin of its own, though no Input: a rate, the default the
    // example starts from.
    [Fact]
    public void Example_municipalRate_isAnInputWithItsDefaultOrigin()
    {
        Inputs(RiverInputs.Example, "").Where(input => input.Path == "Municipality").ShouldBe([("Municipality", (object?)0.07m, InputOrigin.Default)]);
    }

    // Every input of a record of the river, by its path. A record that takes an origin is one input, the
    // value it takes beside that origin: an Input<T>'s value, or MunicipalityChoice's rate. A record of
    // the river that takes none is made of inputs and walked the same way; anything else it takes, a
    // year, a composition's flags, the engine's own records, is no input.
    private static IEnumerable<(string Path, object? Value, InputOrigin Origin)> Inputs(object record, string path)
    {
        var parameters = RiverInputsShapeTests.Parameters(record.GetType());
        if (parameters.Any(parameter => parameter.ParameterType == typeof(InputOrigin)))
        {
            var value = parameters.Single(parameter => parameter.ParameterType.IsValueType && parameter.ParameterType != typeof(InputOrigin));
            var origin = parameters.Single(parameter => parameter.ParameterType == typeof(InputOrigin));
            yield return (path, Read(record, value), Read(record, origin) is InputOrigin given
                ? given
                : throw new InvalidOperationException($"{record.GetType().Name} holds no {Name(origin)}."));
            yield break;
        }

        foreach (var parameter in parameters)
        {
            if (Read(record, parameter) is { } part && part.GetType() is { IsClass: true } partType && partType.Assembly == typeof(RiverInputs).Assembly)
            {
                foreach (var input in Inputs(part, path.Length == 0 ? Name(parameter) : $"{path}.{Name(parameter)}"))
                    yield return input;
            }
        }
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
