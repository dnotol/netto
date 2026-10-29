---
type: adr
title: Components come from MudBlazor and every text from a resource file
description: MudBlazor for components; English UI copy in a .resx beside each component, read through IStringLocalizer, never inline.
status: stable
decided: 2026-10-18
tags: [site]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd12817fb0b0c2cae8a3eaa0
  - resource: https://app.notion.com/p/3fdb6b39bd1281afae0eeea06814a574
  - resource: git:0d1feb6 Add MudBlazor, the dark and gold theme and resource-file text
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

The first front page ([Work Item](https://app.notion.com/p/3fdb6b39bd1281afae0eeea06814a574)) needed a component library and a place for interface text ([Work Item](https://app.notion.com/p/3fdb6b39bd12817fb0b0c2cae8a3eaa0)), on 18 October 2026.

# Decision

Components come from MudBlazor (9 today, registered with `AddMudServices`). A native `<table>` or `<select>` is fine where MudBlazor adds nothing, taking the site's tokens and focus ring. Interface text is English and lives in a `.resx` beside each component (`Pages/Home.resx` for `Pages/Home.razor`), read through `IStringLocalizer<T>` (`AddLocalization` in the client's `Program.cs`). No text is written inline in a `.razor`. Keys built in code go through a helper with its own test (`River/<Card>CardText.cs`).

# Alternatives rejected

None recorded at the time.

# Consequences

- `WebClient/RazorLocalizationTests` fails on inline text.
- MudBlazor's quirks are worked around in Netto's own components (`Fold`, `RiverNumericField`), listed in CLAUDE.md's gotchas.
- `MudBlazor.min.css` loads before `app.css`; scoped CSS reaches MudBlazor's elements only through `::deep`.
