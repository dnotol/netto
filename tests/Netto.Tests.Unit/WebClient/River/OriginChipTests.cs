using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The chip beside an input says where its value comes from, drawn as a viewer meets it (RiverRendering).
// An amount the example holds none of, a lever's before the user types it, draws no chip at all, so an
// « example » chip stands only beside what the example gives (ExampleOriginsTests).
public class OriginChipTests
{
    private static readonly RiverInputs Everything = RiverInputs.Example
        .AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Allowances).AddToPlan(OptionalModule.Rent)
        .AddToPlan(OptionalModule.Car).AddToPlan(OptionalModule.Copyright);

    [Fact]
    public async Task OriginChip_none_drawsNothing()
    {
        var html = await RenderAsync<OriginChip>(new Folds(new BrowserStorage()), new() { ["Origin"] = InputOrigin.None });

        html.Trim().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(InputOrigin.Example, "example")]
    [InlineData(InputOrigin.Default, "default")]
    [InlineData(InputOrigin.Derived, "derived")]
    [InlineData(InputOrigin.Yours, "yours")]
    public async Task OriginChip_origin_drawsItsChip(InputOrigin origin, string chip)
    {
        var html = await RenderAsync<OriginChip>(new Folds(new BrowserStorage()), new() { ["Origin"] = origin });

        Chips(html).ShouldBe([chip]);
    }

    [Fact]
    public async Task RiverField_none_drawsItsLabelAndNoChip()
    {
        var html = await RenderAsync<RiverField>(new Folds(new BrowserStorage()),
            new() { ["Label"] = "Eco vouchers", ["For"] = "river-eco-vouchers", ["Origin"] = (InputOrigin?)InputOrigin.None });

        (html.Contains(">Eco vouchers</label>"), Chips(html).Count).ShouldBe((true, 0));
    }

    // None of the three amounts is the example's: no chip until one is typed.
    [Fact]
    public async Task VoucherCard_example_drawsNoChip()
    {
        var html = await RenderOpenAsync<VoucherCard>(OptionalModule.Vouchers, Everything);

        Chips(html).ShouldBeEmpty();
    }

    [Fact]
    public async Task VoucherCard_amountTyped_drawsItsChipAlone()
    {
        var html = await RenderOpenAsync<VoucherCard>(OptionalModule.Vouchers, Everything.WithEcoVouchers(100m));

        html.ShouldSatisfyAllConditions(
            () => Chips(html).ShouldBe(["yours"]),
            () => Head(html, "river-eco-vouchers").ShouldContain("origin-chip--yours"));
    }

    // The example's car, whose facts open by themselves while its catalogue value is missing: that value,
    // its yearly costs and the contribution are nothing, beside no chip; its first registration and its
    // order are the example's dates, the only « example » chips of the card.
    [Fact]
    public async Task CarCard_example_drawsTheExamplesChipsOnItsDatesAlone()
    {
        var html = await RenderOpenAsync<CarCard>(OptionalModule.Car, Everything);

        html.ShouldSatisfyAllConditions(
            () => new[] { "river-car-catalogue", "river-car-costs", "river-car-contribution" }.Where(field => Head(html, field).Contains("origin-chip")).ShouldBeEmpty(),
            () => Chips(html).Count(chip => chip == "example").ShouldBe(2));
    }

    // The card opened by the viewer.
    private static Task<string> RenderOpenAsync<TCard>(OptionalModule module, RiverInputs inputs)
        where TCard : IComponent
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(ModuleFolds.Name(module), true);

        return RenderAsync<TCard>(folds, new() { ["Inputs"] = inputs, ["Run"] = inputs.Run(), ["Refused"] = null });
    }

    // The origin of each chip drawn, in the order of the page.
    private static List<string> Chips(string html) =>
        [.. Regex.Matches(html, "origin-chip--(\\w+)").Select(match => match.Groups[1].Value)];

    // From a field's label to its input: its name, its help and the chip beside them, if any.
    private static string Head(string html, string field)
    {
        var label = html.IndexOf($"id=\"{field}-label\"", StringComparison.Ordinal);
        label.ShouldBeGreaterThan(-1);
        return html[label..html.IndexOf($"id=\"{field}\"", label, StringComparison.Ordinal)];
    }
}
