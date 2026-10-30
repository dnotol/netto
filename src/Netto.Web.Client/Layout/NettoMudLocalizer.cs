using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Netto.Web.Client.Layout;

// MudBlazor's own words, the few Netto shows, in the page's language: a field's error on what is no number
// (Converter_InvalidNumber, TypedNumberConverter) and the municipality picker's clear button (MudInput_Clear).
// MudBlazor ships English only; out of English it asks this localizer first, and falls back to its own English
// for a key the .resx lacks (DefaultLocalizationInterceptor). The neutral .resx holds MudBlazor's English word for word,
// the .fr.resx the French. A MudBlazor text Netto comes to show is a key added to both.
internal sealed class NettoMudLocalizer(IStringLocalizer<NettoMudLocalizer> texts) : MudLocalizer
{
    public override LocalizedString this[string key] => texts[key];
}
