using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Netto.Core.Rules;

namespace Netto.Web.Client.River;

// A number field of the river: MudBlazor's own, marked in error while the engine refuses its value.
// The card passes whether it does as Error, as it would to a MudNumericField, and the engine's refusal
// as Refusal. MudNumericField validates its number itself after each edit and sets Error from that alone,
// which clears the refusal; the card then passes the same Error again, and MudBlazor, which applies a
// parameter only when it changes, would leave the field unmarked. So the refusal is set again once
// MudBlazor has validated. The field also takes the engine's message as its error text, which
// MudBlazor ties to the input with aria-describedby, so a screen reader reads the message with the
// field; the card shows it in sight (RefusedMessage), so RiverField keeps the field's copy out of sight
// while it is refused (river-number--refused), and MudBlazor's own errors, a number it cannot read, in
// sight. Every field of the river looks and behaves the same unless its card says otherwise: outlined and
// dense, without spin buttons, in the site's culture, taking what is typed after 400 ms of quiet and reading
// it strictly (TypedNumberConverter), so a French « 1.5 » is no number rather than 15; an amount in euros
// (Euros) with two decimals and a € before it, after it in French; a field the river bounds taken once it is
// left (TakenOnLeaving), so the bounded amount replaces the typed one.
public sealed class RiverNumericField<T> : MudNumericField<T>
{
    // How long the field waits after a key before it takes what is typed.
    private const double Quiet = 400;
    // The id of the error text, for aria-describedby; MudBlazor names it only from its own validation.
    private readonly string _errorId = $"river-refusal-{Guid.NewGuid():N}";

    // The engine's message the field last took as its error text, taken back once the refusal ends.
    private string? _refusalText;

    // Its place among the fields an action that ends the page asks to take what is typed (TypedFields).
    private IDisposable? _joined;

    [Inject]
    private IStringLocalizer<RefusedMessage> Messages { get; set; } = default!;

    [Inject]
    private TypedFields Fields { get; set; } = default!;

    // The engine's refusal on the card, worded as the field's error text while Error is set.
    [Parameter]
    public Refusal? Refusal { get; set; }

    // An amount in euros: two decimals, and a € before it.
    [Parameter]
    public bool Euros { get; set; }

    // Taken when the field is left, not while typing: a value the river bounds, which MudBlazor redraws
    // only once the field has lost focus.
    [Parameter]
    public bool TakenOnLeaving { get; set; }

    // The river's defaults go in before the card's parameters, so a parameter the card gives wins. They are
    // set before MudBlazor first reads its parameters, which is when it takes the format and the culture.
    // They are set again on every call, which holds because each call carries every parameter the card
    // gives: a call carrying only cascading parameters would reset what the card gave to the defaults.
    // Nothing the field takes cascades today; one that does needs the defaults set on the first call only.
    public override Task SetParametersAsync(ParameterView parameters)
    {
        var takenOnLeaving = parameters.TryGetValue<bool>(nameof(TakenOnLeaving), out var leaving) && leaving;
        Variant = Variant.Outlined;
        Margin = Margin.Dense;
        HideSpinButtons = true;
        Culture = Formats.Culture;
        Immediate = !takenOnLeaving;
        DebounceInterval = takenOnLeaving ? 0 : Quiet;
        if (parameters.TryGetValue<bool>(nameof(Euros), out var euros) && euros)
        {
            Format = "N2";
            Adornment = Languages.Current == Language.French ? Adornment.End : Adornment.Start;
            AdornmentText = "€";
        }

        return base.SetParametersAsync(parameters);
    }

    // The converter the field reads and shows its text through, for a test to read what is typed as the field does.
    internal IConverter<T?, string?> Converting => GetConverter();

    protected override IConverter<T?, string?> GetDefaultConverter() => new TypedNumberConverter<T> { Culture = GetCulture, Format = GetFormat };

    // The parameter keeps the name the Razor compiler gives it in the base component (CA1725).
    protected override void BuildRenderTree(RenderTreeBuilder __builder)
    {
        __builder.OpenElement(0, "div");
        __builder.AddAttribute(1, "class", ErrorState.RenderValue ? "river-number river-number--refused" : "river-number");
        __builder.OpenRegion(2);
        base.BuildRenderTree(__builder);
        __builder.CloseRegion();
        __builder.CloseElement();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _joined = Fields.Add(TakeTypedAsync);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _joined?.Dispose();
        await base.DisposeAsyncCore();
    }

    // What is typed and not taken yet, taken now, as leaving the field would: nothing when the text is the value
    // as the field shows it, so a value is never rounded to the text that shows it.
    private Task TakeTypedAsync() =>
        string.Equals(ReadText, ConvertSet(ReadValue), StringComparison.Ordinal) ? Task.CompletedTask : UpdateValuePropertyAsync(true);

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

    // Marks the field and words its error while the card says the engine refuses it, and takes the
    // words back once it no longer does. True when that changed the field.
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
