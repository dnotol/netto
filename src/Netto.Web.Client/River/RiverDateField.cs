using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Utilities.Exceptions;
using Netto.Core.Rules;

namespace Netto.Web.Client.River;

// A date field of the river: MudBlazor's date picker, marked in error while the engine refuses its date, as a
// number field of the river is (RiverNumericField). The card passes whether it does as Error and the engine's
// refusal as Refusal. MudBlazor sets Error from its own validation alone after each change, which clears the
// refusal, and applies a parameter only when it changes, so the refusal is set again once MudBlazor has
// validated. The field takes the engine's message as its error text, which MudBlazor ties to the input with
// aria-describedby, so a screen reader reads it with the field; the card shows it in sight (RefusedMessage), so
// RiverField keeps the field's copy out of sight while it is refused (river-date--refused), and MudBlazor's own
// error, a text that is no date, in sight. Every date field looks and behaves the same unless its card says
// otherwise: outlined and dense, in the site's culture, typed or picked as day/month/year in both languages, as
// Belgian papers write a date, read strictly (TypedDateConverter), cleared to none, its calendar closing once a
// day is picked, so its overlay, which holds the page while the calendar is open, never takes the click meant
// for the next field, and its calendar button named in the page's language. What is typed is taken on Enter or
// on leaving the field, as MudBlazor takes it; the field does not join TypedFields, whose actions leave the field
// before they read the river (a click on the language switch, checked in the browser on 2026-11-13).
public sealed class RiverDateField : MudDatePicker
{
    // The id of the error text, for aria-describedby; MudBlazor names it only from its own validation.
    private readonly string _errorId = $"river-refusal-{Guid.NewGuid():N}";

    // The engine's message the field last took as its error text, taken back once the refusal ends.
    private string? _refusalText;

    [Inject]
    private IStringLocalizer<RefusedMessage> Messages { get; set; } = default!;

    [Inject]
    private IStringLocalizer<RiverDateField> Texts { get; set; } = default!;

    // The engine's refusal on the card, worded as the field's error text while Error is set.
    [Parameter]
    public Refusal? Refusal { get; set; }

    // The river's defaults go in before the card's parameters, so a parameter the card gives wins; set again on
    // every call, which holds because each call carries every parameter the card gives (RiverNumericField).
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Variant = Variant.Outlined;
        Margin = Margin.Dense;
        Culture = Formats.Culture;
        Editable = true;
        Clearable = true;
        AutoClose = true;
        DateFormat = TypedDateConverter.Shown;
        AdornmentAriaLabel = Texts["OpenCalendar"];

        return base.SetParametersAsync(parameters);
    }

    // The converter the field reads and shows its text through, for a test to read what is typed as the field does.
    internal IConverter<DateTime?, string?> Converting => GetConverter();

    protected override IConverter<DateTime?, string?> GetDefaultConverter() => new TypedDateConverter();

    // The parameter keeps the name the Razor compiler gives it in the base component (CA1725).
    protected override void BuildRenderTree(RenderTreeBuilder __builder)
    {
        __builder.OpenElement(0, "div");
        __builder.AddAttribute(1, "class", ErrorState.RenderValue ? "river-date river-date--refused" : "river-date");
        __builder.OpenRegion(2);
        base.BuildRenderTree(__builder);
        __builder.CloseRegion();
        __builder.CloseElement();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await FollowRefusalAsync();
    }

    protected override async Task ValidateValue()
    {
        await base.ValidateValue();
        if (await FollowRefusalAsync())
            StateHasChanged();
    }

    // Marks the field and words its error while the card says the engine refuses it, and takes the words back
    // once it no longer does. True when that changed the field.
    private async Task<bool> FollowRefusalAsync()
    {
        if (ErrorState.RenderValue)
        {
            var text = Refusal is null ? null : Messages[RefusedMessageText.Error(Refusal)].Value;
            if (ErrorState.Value && ErrorTextState.Value == text && ErrorIdState.Value is not null)
                return false;

            await ErrorState.SetValueAsync(true);
            await ErrorTextState.SetValueAsync(text);
            await ErrorIdState.SetValueAsync(ErrorIdState.Value ?? _errorId);
            _refusalText = text;
            return true;
        }

        var worded = _refusalText is not null && ErrorTextState.Value == _refusalText;
        _refusalText = null;
        if (!worded)
            return false;

        await ErrorTextState.SetValueAsync(null);
        return true;
    }
}

// A date as the river's date fields read and show it: day/month/year, the leading zeros optional when typed, the
// same in both languages, in Western digits; anything else is no date, refused with MudBlazor's key, which the
// field words as its error (NettoMudLocalizer), never a date read another way. An emptied field is none.
internal sealed class TypedDateConverter : IReversibleConverter<DateTime?, string?>
{
    // How a date is shown, and the format a picked day is written in.
    public const string Shown = "dd/MM/yyyy";

    // MudBlazor's own key for a text that is no date (its LanguageResource, internal), which its localizer words.
    internal const string InvalidDate = "Converter_InvalidDateTime";

    private static readonly string[] Typed = [Shown, "d/M/yyyy"];

    public string? Convert(DateTime? input) => input?.ToString(Shown, CultureInfo.InvariantCulture);

    public DateTime? ConvertBack(string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? null
            : DateTime.TryParseExact(input.Trim(), Typed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : throw new ConversionException(InvalidDate);
}
