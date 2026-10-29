---
type: adr
title: A refusal is a typed value carrying its key and the input it refuses
description: Refusal(Key, RefusedInput), declared only in Core; the client routes it to its card through an exhaustive RiverErrors.Owner.
status: stable
decided: 2026-10-24
tags: [engine, site]
sources:
  - resource: https://app.notion.com/p/403b6b39bd1281b988c7e240f8369c20
  - resource: https://app.notion.com/p/403b6b39bd1281e783b2cc21e4f3c8ea
  - resource: "git:feff8a9 Make a refusal a typed value carrying its key and the input it refuses"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The engine refuses an input it cannot compute with rather than guessing (a negative amount, a combustion car without its CO2, a year without rules). Before 24.10.2026 a refusal was a string key, and the page had to work out which card and field it belonged to. It was decided in « The engine takes a new lever without touching the year's composition » ([story](https://app.notion.com/p/403b6b39bd1281b988c7e240f8369c20)).

# Decision

A refusal is a `Refusal` (`Netto.Core.Rules`): a stable key, unchanged from the strings before, and the `RefusedInput` it refuses. It is a `static readonly` field beside its rule, declared only in Core, and worded as `Error_<key>` in `RefusedMessage.resx`. A refusable result is `(T? Value, Refusal? Error)`. The client derives the card from the input through `RiverErrors.Owner`, an exhaustive switch that throws on an input no case names, and marks the field.

# Alternatives rejected

- A refusal as a bare string key the page matched to a card, as before 24.10.2026.

# Consequences

- No engine refusal can reach the river unmarked; `RiverErrorsTests` and `RiverTextTests` hold every input.
- A new input is a `RefusedInput` with its case in `RiverErrors.Owner`; a refusal declared on `YearSimulation`, `GrossToNet`, `WithholdingCalculator` or `CopyrightCalculator` also takes its row in `RiverErrorsTests.SpreadOverCards`.
- The engine's refusal goes to the card holding the refused field, and only there (`Plan.Refused`); the flow is drawn on the [edit page](../edit-to-repaint.html).
