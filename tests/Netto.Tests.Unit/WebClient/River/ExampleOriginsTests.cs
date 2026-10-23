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

    // Every input of a record of the river, by its path: the records it is made of are walked the same
    // way, and what is neither, a year or a municipality's rate, holds no input.
    private static IEnumerable<(string Path, object? Value, InputOrigin Origin)> Inputs(object record, string path)
    {
        var type = record.GetType();
        var constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().Length > 0 && !IsCopyConstructor(type, candidate));
        foreach (var parameter in constructor.GetParameters())
        {
            var name = $"{path}{parameter.Name}";
            switch (type.GetProperty(parameter.Name!)!.GetValue(record))
            {
                case { } input when input.GetType() is { IsGenericType: true } generic && generic.GetGenericTypeDefinition() == typeof(Input<>):
                    yield return (name, Read(input, nameof(Input<int>.Value)), (InputOrigin)Read(input, nameof(Input<int>.Origin))!);
                    break;
                case { } part when part.GetType() is { IsClass: true } partType && partType.Assembly == typeof(RiverInputs).Assembly:
                    foreach (var inner in Inputs(part, $"{name}."))
                        yield return inner;
                    break;
            }
        }
    }

    private static object? Read(object input, string property) => input.GetType().GetProperty(property)!.GetValue(input);

    private static bool IsCopyConstructor(Type type, ConstructorInfo constructor) =>
        constructor.GetParameters() is [var only] && only.ParameterType == type;
}
