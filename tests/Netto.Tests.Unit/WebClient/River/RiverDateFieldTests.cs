using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor.Utilities.Exceptions;
using Netto.Core.CompanyCar;
using Netto.Core.Rules;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A date field of the river: MudBlazor's date picker, typed or picked as day/month/year, read strictly, its
// calendar closing once a day is picked so the next click reaches the next field, its calendar button named
// in the page's language, and, while the engine refuses its date, marked with the engine's words tied to its
// input for a screen reader, as a number field of the river is (RiverNumericField).
public class RiverDateFieldTests
{
    private const string Field = "river-test";

    private static readonly ResxText Messages = ResxText.For<RefusedMessage>();

    private static readonly ResxText Texts = ResxText.For<RiverDateField>();

    [Fact]
    public async Task Refused_field_isDescribedByTheEnginesWords()
    {
        var html = await RenderAsync(new() { ["Error"] = true, ["Refusal"] = CarCalculator.ReturnedBeforeHeld });

        CardHtml.FieldMessage(html, Field).ShouldBe(Messages.Get($"Error_{CarCalculator.ReturnedBeforeHeld.Key}"));
    }

    [Fact]
    public async Task Accepted_field_isDescribedByNothing()
    {
        var html = await RenderAsync(new() { ["Error"] = false, ["Refusal"] = CarCalculator.ReturnedBeforeHeld });

        CardHtml.FieldMessage(html, Field).ShouldBeNull();
    }

    // The engine's words are the card's to show in sight (RefusedMessage): the field's copy is kept out of sight,
    // as a refused number field's is (RiverField's stylesheet).
    [Fact]
    public async Task Refused_field_keepsItsCopyOfTheWordsOutOfSight()
    {
        var html = await RenderAsync(new() { ["Error"] = true, ["Refusal"] = CarCalculator.ReturnedBeforeHeld });

        (html.Contains("river-date river-date--refused", StringComparison.Ordinal),
                Stylesheet.Read("src", "Netto.Web.Client", "River", "RiverField.razor.css").Contains(".river-date--refused .mud-input-control-helper-container", StringComparison.Ordinal))
            .ShouldBe((true, true));
    }

    [Fact]
    public async Task CalendarButton_inEnglish_isNamedByItsText()
    {
        var html = await RenderAsync([]);

        html.ShouldContain($"aria-label=\"{Texts.Get("OpenCalendar")}\"");
    }

    [Fact]
    public async Task CalendarButton_inFrench_isNamedInFrench()
    {
        using var french = InLanguage.French();

        var html = await RenderAsync([]);

        html.ShouldContain($"aria-label=\"{ResxText.French<RiverDateField>().Get("OpenCalendar")}\"");
    }

    // Picking a day closes the calendar and the overlay that holds the page while it is open, so a click on the
    // next field reaches it; the field is typed into as well as picked, and cleared to none.
    [Fact]
    public async Task Field_byDefault_closesOnAPickedDayAndIsTypedAndCleared()
    {
        var field = await CaptureAsync([]);

        (field.AutoClose, field.Editable, field.Clearable).ShouldBe((true, true, true));
    }

    // Typed as day/month/year, with or without the leading zeros, in both languages; anything else is no date,
    // never a date read another way, and an emptied field is none.
    [Fact]
    public async Task Typed_isReadStrictly()
    {
        var converter = (await CaptureAsync([])).Converting.ShouldBeOfType<TypedDateConverter>();

        converter.ShouldSatisfyAllConditions(
            () => converter.ConvertBack("17/09/2026").ShouldBe(new DateTime(2026, 9, 17)),
            () => converter.ConvertBack("1/9/2026").ShouldBe(new DateTime(2026, 9, 1)),
            () => converter.ConvertBack("").ShouldBeNull(),
            () => Should.Throw<ConversionException>(() => converter.ConvertBack("2026-09-17")),
            () => Should.Throw<ConversionException>(() => converter.ConvertBack("09/17/2026")),
            () => Should.Throw<ConversionException>(() => converter.ConvertBack("17/09/26")),
            () => Should.Throw<ConversionException>(() => converter.ConvertBack("17/09")),
            () => converter.Convert(new DateTime(2026, 9, 1)).ShouldBe("01/09/2026"));
    }

    [Fact]
    public async Task Typed_inFrench_isReadTheSameWay()
    {
        using var french = InLanguage.French();
        var converter = (await CaptureAsync([])).Converting.ShouldBeOfType<TypedDateConverter>();

        (converter.ConvertBack("17/09/2026"), converter.Convert(new DateTime(2026, 9, 17))).ShouldBe((new DateTime(2026, 9, 17), "17/09/2026"));
    }

    private static Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
        RiverRendering.RenderAsync<Host>(new Folds(new BrowserStorage()), new() { ["Parameters"] = parameters });

    private static async Task<RiverDateField> CaptureAsync(Dictionary<string, object?> parameters)
    {
        RiverDateField? captured = null;
        await RiverRendering.RenderAsync<Host>(new Folds(new BrowserStorage()),
            new() { ["Parameters"] = parameters, ["Captured"] = (Action<RiverDateField>)(field => captured = field) });

        return captured.ShouldNotBeNull();
    }

    // Draws one date field of the river with the parameters given, as a card's markup would, and hands it over.
    private sealed class Host : ComponentBase
    {
        [Parameter]
        public Dictionary<string, object?> Parameters { get; set; } = [];

        [Parameter]
        public Action<RiverDateField>? Captured { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<RiverDateField>(0);
            builder.AddComponentParameter(1, "InputId", Field);
            builder.AddComponentParameter(2, "Date", (DateTime?)new DateTime(2026, 9, 17));
            foreach (var (name, value) in Parameters)
                builder.AddComponentParameter(3, name, value);
            builder.AddComponentReferenceCapture(4, field => Captured?.Invoke((RiverDateField)field));
            builder.CloseComponent();
        }
    }
}
