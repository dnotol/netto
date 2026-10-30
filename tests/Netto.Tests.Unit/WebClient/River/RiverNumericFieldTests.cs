using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Utilities.Exceptions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// A number field of the river looks and behaves the same everywhere unless a card says otherwise: outlined
// and dense, without spin buttons, in the site's culture, taking what is typed after 400 ms of quiet. An
// amount in euros (Euros) has two decimals and a € before it; a field the river bounds is taken when it
// is left (TakenOnLeaving). Each is drawn as the explicit block the cards used to pass, MudBlazor's own
// ids and the field's refusal id aside.
public class RiverNumericFieldTests
{
    // What every card passed before the field carried its own defaults: an amount taken while typing.
    private static readonly Dictionary<string, object?> ExplicitEuros = new()
    {
        ["Format"] = "N2",
        ["Culture"] = Formats.Culture,
        ["Adornment"] = Adornment.Start,
        ["AdornmentText"] = "€",
        ["HideSpinButtons"] = true,
        ["Variant"] = Variant.Outlined,
        ["Margin"] = Margin.Dense,
        ["Immediate"] = true,
        ["DebounceInterval"] = 400d,
    };

    [Fact]
    public async Task Euros_alone_drawsTheBlockEveryAmountPassed()
    {
        var html = await RenderAsync(new() { ["Euros"] = true });

        html.ShouldBe(await RenderAsync(ExplicitEuros));
    }

    [Fact]
    public async Task Euros_alone_drawsTheAmountWithTwoDecimalsAfterAEuroSign()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync(new() { ["Euros"] = true }));

        (html.Contains("value=\"1,234.50\"", StringComparison.Ordinal), html.IndexOf('€', StringComparison.Ordinal) < html.IndexOf("<input", StringComparison.Ordinal))
            .ShouldBe((true, true));
    }

    // In French the amount reads like the administration's, the euro after it.
    [Fact]
    public async Task Euros_inFrench_drawsTheAmountTheFrenchWayBeforeAEuroSign()
    {
        using var french = InLanguage.French();

        var html = WebUtility.HtmlDecode(await RenderAsync(new() { ["Euros"] = true }));

        (html.Contains("value=\"1.234,50\"", StringComparison.Ordinal), html.IndexOf('€', StringComparison.Ordinal) > html.IndexOf("<input", StringComparison.Ordinal))
            .ShouldBe((true, true));
    }

    // What is typed is read strictly (TypedNumberConverter): in French a decimal point is no number, never ten
    // or a hundred times the amount, nor a group led by 0 a thousand times it, and a French number is its value.
    [Fact]
    public async Task Typed_inFrench_isReadStrictly()
    {
        using var french = InLanguage.French();
        var field = await CaptureAsync(new() { ["Euros"] = true });

        var converter = field.Converting.ShouldBeOfType<TypedNumberConverter<decimal>>();

        converter.ShouldSatisfyAllConditions(
            () => Should.Throw<ConversionException>(() => converter.ConvertBack("1.5")),
            () => Should.Throw<ConversionException>(() => converter.ConvertBack("12.50")),
            () => Should.Throw<ConversionException>(() => converter.ConvertBack("0.500")),
            () => Should.Throw<ConversionException>(() => converter.ConvertBack("012.345")),
            () => converter.ConvertBack("0,5").ShouldBe(0.5m),
            () => converter.ConvertBack("1.234,56").ShouldBe(1_234.56m));
    }

    // What is kept in the company and the split's exact pay: the same look, taken when the field is left.
    [Fact]
    public async Task TakenOnLeaving_euros_drawsTheBlockTheBoundedFieldsPassed()
    {
        var html = await RenderAsync(new() { ["Euros"] = true, ["TakenOnLeaving"] = true });

        html.ShouldBe(await RenderAsync(new(ExplicitEuros.Where(parameter => parameter.Key is not ("Immediate" or "DebounceInterval")))));
    }

    [Fact]
    public async Task Defaults_none_takeWhatIsTypedAfterAQuietMomentInTheSitesCulture()
    {
        var field = await CaptureAsync(new() { ["Euros"] = true });

        (field.Immediate, field.DebounceInterval, field.GetState(state => state.Culture), field.Variant, field.Margin, field.HideSpinButtons)
            .ShouldBe((true, 400d, Formats.Culture, Variant.Outlined, Margin.Dense, true));
    }

    [Fact]
    public async Task TakenOnLeaving_given_takesTheValueWhenTheFieldIsLeft()
    {
        var field = await CaptureAsync(new() { ["Euros"] = true, ["TakenOnLeaving"] = true });

        (field.Immediate, field.DebounceInterval).ShouldBe((false, 0d));
    }

    // A card's own parameters win over the field's: a card can show the spin buttons or take no margin.
    [Fact]
    public async Task Parameters_given_winOverTheDefaults()
    {
        var field = await CaptureAsync(new() { ["Margin"] = Margin.None, ["HideSpinButtons"] = false, ["DebounceInterval"] = 0d, ["Format"] = "0.0" });

        (field.Margin, field.HideSpinButtons, field.DebounceInterval, field.GetState(state => state.Format), field.Immediate).ShouldBe((Margin.None, false, 0d, "0.0", true));
    }

    // Without Euros, no adornment: a percentage or an emission brings its own.
    [Fact]
    public async Task Euros_notGiven_drawsNoEuroSign()
    {
        var html = await RenderAsync(new() { ["Format"] = "0" });

        html.Contains('€', StringComparison.Ordinal).ShouldBeFalse();
    }

    // The field drawn for an amount of 1,234.5, its ids made comparable.
    private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
    {
        var html = await RiverRendering.RenderAsync<Host>(new Folds(new BrowserStorage()), new() { ["Parameters"] = parameters });

        return Regex.Replace(Regex.Replace(html, "river-refusal-[0-9a-f]{32}", "river-refusal-ID"), "(numericField|mudinput)[a-z0-9]{8}", "$1ID");
    }

    // The field the host drew, read once it has been drawn.
    private static async Task<RiverNumericField<decimal>> CaptureAsync(Dictionary<string, object?> parameters)
    {
        RiverNumericField<decimal>? captured = null;
        await RiverRendering.RenderAsync<Host>(new Folds(new BrowserStorage()), new() { ["Parameters"] = parameters, ["Captured"] = (Action<RiverNumericField<decimal>>)(field => captured = field) });

        return captured.ShouldNotBeNull();
    }

    // Draws one field of the river with the parameters given, as a card's markup would, and hands it over.
    private sealed class Host : ComponentBase
    {
        [Parameter]
        public Dictionary<string, object?> Parameters { get; set; } = [];

        [Parameter]
        public Action<RiverNumericField<decimal>>? Captured { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<RiverNumericField<decimal>>(0);
            builder.AddComponentParameter(1, "InputId", "river-test");
            builder.AddComponentParameter(2, "Value", 1_234.5m);
            foreach (var (name, value) in Parameters)
                builder.AddComponentParameter(3, name, value);
            builder.AddComponentReferenceCapture(4, field => Captured?.Invoke((RiverNumericField<decimal>)field));
            builder.CloseComponent();
        }
    }
}
