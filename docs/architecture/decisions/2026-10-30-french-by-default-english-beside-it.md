---
type: adr
title: The site speaks French by default, English one switch away
description: English in each neutral .resx, French in a .fr.resx beside it; the client stays invariant with PredefinedCulturesOnly=false and sets only the UI culture, at start-up, from the viewer's choice; Formats picks its culture by language; the switch reloads and carries the river.
status: draft
decision: proposed
decided: 2026-10-30
tags: [site]
sources:
  - resource: https://app.notion.com/p/409b6b39bd12818f9c82e79a28e87297
  - resource: ../../knowledge/research/2026-10-30-french-culture-in-the-browser.md
  - resource: ../../knowledge/domain/french-glossary.md
generated: {by: claude-opus-5-5/architecture-views, at: 2026-10-30T18:00:00+01:00}
---

# Context

The operator asked for the whole site in French by default, with English kept beside it and easy to switch to ([story](https://app.notion.com/p/409b6b39bd12818f9c82e79a28e87297)). Two decisions forbid it today: [every text from a resource file](/2026-10-18-mudblazor-and-resx-text.md), in English, and [numbers on the invariant culture, the app's culture never set](/2026-10-19-invariant-culture-for-numbers.md). The client is published with `InvariantGlobalization`, so a culture created by name throws ([research](../../knowledge/research/2026-10-30-french-culture-in-the-browser.md)).

# Decision

- English stays in each component's neutral `.resx`; French goes in `<Name>.fr.resx` beside it, the same keys, placeholders and `[term]` markers (`ResxTranslationTests`). Its words follow the [glossary](../../knowledge/domain/french-glossary.md).
- The client keeps `InvariantGlobalization` and sets `PredefinedCulturesOnly=false`, so the culture named `fr` can be created. Only the neutral `fr` is used, never `fr-BE`, whose parent is invariant in this mode.
- `Program.cs` calls `Languages.Apply` after `Build()` and before `RunAsync()`. It sets the UI culture (`DefaultThreadCurrentUICulture` and `CurrentUICulture`) from the viewer's choice in localStorage (`netto.language`), French when there is none, and never `CurrentCulture`. That stays invariant, so a number formatted without a culture never turns French in an attribute.
- `Formats` picks its culture by the UI language: the English one as today, or a French one built on an invariant clone, reading « 1.234,56 € », « 20,5 % », « 1er janvier 2027 ».
- The switch (« Français » / « English ») stores the choice and reloads the page, since satellite resources load once at start-up; the river is carried across the reload in sessionStorage, so unsaved edits stay.
- MudBlazor's own strings go through Netto's `MudLocalizer`.
- Tests run on the invariant culture, so they read English; French is read through `InLanguage`.
- What the two decisions replaced still held: components come from MudBlazor; no text is written inline in a `.razor`, every text read through `IStringLocalizer<T>` (`RazorLocalizationTests`), keys built in code through a helper with its own test; every number goes through `Formats`, a rule amount formatted by its key's suffix; the browser and the tests format alike.

# Alternatives rejected

- French in the neutral `.resx` and English in `.en.resx`: forty files renamed and the English assertions moved, for a fallback the parity test already makes unnecessary.
- Turning `InvariantGlobalization` off: an ICU download for every visitor, and `icudt_EFIGS.dat` has no `fr-BE`.
- Setting `CurrentCulture` to French: raw numbers in attributes, SVG and range inputs would read `1234,5`.
- Switching without a reload: no public API loads a second culture's satellite resources at run time.
- The `MudBlazor.Translations` package: forty-five locales for the four MudBlazor strings Netto shows.
- A confirm dialog over unsaved edits on switching: friction and copy, where carrying the river costs one small interface.

# Consequences

- The two decisions above are superseded once this one is accepted.
- A French visitor downloads one satellite assembly, `fr/Netto.Web.Client.resources.dll`.
- The unit tests run with ICU, so a culture the browser cannot create is caught only by the csproj assertion in `ClientGlobalizationTests` and the browser check.
- A text a viewer types is read in their language's format: in French « 1.234,56 » or « 1234,56 », and a typed « 1234.56 » is refused.
