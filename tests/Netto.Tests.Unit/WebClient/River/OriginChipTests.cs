using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The chip beside an input says where its value comes from, drawn as a viewer meets it (RiverRendering).
// An amount the example holds none of, a lever's before the user types it, draws no chip at all, so an
// « example » chip stands only beside what the example gives (ExampleOriginsTests; the car's dates in
// CarCardTests).
public class OriginChipTests
{
    private static readonly RiverInputs VouchersInPlan = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);

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

        OriginChips(html).ShouldBe([chip]);
    }

    [Fact]
    public async Task RiverField_none_drawsItsLabelAndNoChip()
    {
        var html = await RenderAsync<RiverField>(new Folds(new BrowserStorage()),
            new() { ["Label"] = "Eco vouchers", ["For"] = "river-eco-vouchers", ["Origin"] = (InputOrigin?)InputOrigin.None });

        (html.Contains(">Eco vouchers</label>"), OriginChips(html).Count).ShouldBe((true, 0));
    }

    // None of the three amounts is the example's: no chip until one is typed.
    [Fact]
    public async Task VoucherCard_example_drawsNoChip()
    {
        var html = await RenderOpenAsync<VoucherCard>(OptionalModule.Vouchers, VouchersInPlan);

        OriginChips(html).ShouldBeEmpty();
    }

    [Fact]
    public async Task VoucherCard_amountTyped_drawsItsChipAlone()
    {
        var html = await RenderOpenAsync<VoucherCard>(OptionalModule.Vouchers, VouchersInPlan.WithEcoVouchers(100m));

        html.ShouldSatisfyAllConditions(
            () => OriginChips(html).ShouldBe(["yours"]),
            () => FieldHead(html, "river-eco-vouchers").ShouldContain("origin-chip--yours"));
    }
}
