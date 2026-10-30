---
type: research
title: A French culture in a WebAssembly client published without ICU data
description: Whether a .NET 10 Blazor WebAssembly client with InvariantGlobalization can create a culture named fr, load its satellite resources and switch language, and what MudBlazor 9 needs for French.
tags: [site, language]
sources:
  - resource: https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0
  - resource: https://learn.microsoft.com/en-us/dotnet/core/runtime-config/globalization
  - resource: https://learn.microsoft.com/en-us/dotnet/core/compatibility/globalization/6.0/culture-creation-invariant-mode
  - resource: https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md
  - resource: https://github.com/dotnet/aspnetcore/blob/release/10.0/src/Components/WebAssembly/WebAssembly/src/Hosting/WebAssemblyCultureProvider.cs
  - resource: https://github.com/MudBlazor/Translations
generated: {by: claude-sonnet-5-5/researcher, at: 2026-10-30T17:30:00+01:00}
---

# Question

Netto's client is published with `InvariantGlobalization=true`. Can it run in French, and how does the viewer switch language?

# Findings

- With `InvariantGlobalization=true` the SDK defaults `PredefinedCulturesOnly` to true (`Microsoft.NET.Sdk.targets`, SDK 10.0.301, line 104) and `new CultureInfo("fr")` throws `CultureNotFoundException`. With `<PredefinedCulturesOnly>false</PredefinedCulturesOnly>` any culture name can be created, carrying the invariant culture's data. Its parent is then the invariant culture, so `fr-BE` would never reach a `.fr.resx`: only the neutral `fr` works. `TwoLetterISOLanguageName` reads `iv` for every culture in this mode. The foundation analysis confirmed all of this in a scratch console app.
- Satellite assemblies are still built and published in invariant mode; only the `icudt*` files are left out (`Microsoft.NET.Sdk.BlazorWebAssembly.5_0.targets`). `ResourceManager` probes satellites with no globalization-mode check. The scratch app found `Texts.fr.resx` with the UI culture set to `fr`. The browser was not checked during the research.
- Blazor WebAssembly loads satellites once, in `WebAssemblyHost.RunAsync`, for `CurrentCulture` and `CurrentUICulture` (both count since .NET 10). Microsoft documents setting `CultureInfo.DefaultThreadCurrentUICulture` in `Program.cs` after `Build()` and before `RunAsync()`, read from localStorage through `IJSRuntime`, and switching with a forced reload. No public API loads a second culture's satellites at run time.
- An invariant clone takes French formats when every value is set by hand before `ReadOnly`: the month names and their genitive arrays (13 entries each), the number, currency and percent separators, the symbols. A custom format string such as `"0.0%"` ignores `PercentPositivePattern`, so the space goes in the symbol.
- Turning invariant mode off instead costs an ICU file (about 0.3 MB compressed per Microsoft), and `icudt_EFIGS.dat` covers `fr-FR`, not `fr-BE`.
- MudBlazor 9 ships its interface strings in English only; French comes from the `MudBlazor.Translations` package or from an application's own `MudLocalizer`.
- `NeutralResourcesLanguage` is honoured in invariant mode only when `PredefinedCulturesOnly` is false.

# Confidence

High for the throw, the flag, satellite publishing and the reload; the satellite load in the browser stays to be seen in a browser check.
