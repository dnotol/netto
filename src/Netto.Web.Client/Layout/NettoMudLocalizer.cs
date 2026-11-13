using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Netto.Web.Client.Layout;

// MudBlazor's own words, the few Netto shows, in the page's language: a field's error on what is no number
// (Converter_InvalidNumber, TypedNumberConverter) or no date (Converter_InvalidDateTime, TypedDateConverter), the
// clear button of the municipality picker and the date fields (MudInput_Clear), and the date calendar's buttons
// to the month or the year before or after (MudBaseDatePicker_*).
// MudBlazor ships English only; out of English it asks this localizer first, and falls back to its own English
// for a key the .resx lacks (DefaultLocalizationInterceptor). The neutral .resx holds MudBlazor's English word for word,
// the .fr.resx the French. A MudBlazor text Netto comes to show is a key added to both.
internal sealed class NettoMudLocalizer(IStringLocalizer<NettoMudLocalizer> texts) : MudLocalizer
{
    public override LocalizedString this[string key] => texts[key];
}
