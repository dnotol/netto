---
type: adr
title: The architecture pages and the decisions taken live in the repository; the claude.ai maps stay as a frozen rule reference
description: docs/architecture holds the pages and ADRs; Notion keeps decisions under debate; the maps are kept and linked but no longer updated.
status: stable
decided: 2026-10-29
tags: [docs]
sources:
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

How Netto's code is shaped was described in three places: the River Map and three subsystem maps on claude.ai, updated in each slice ([subsystem maps as Artifacts](/2026-10-23-subsystem-maps-as-artifacts.md)); the Notion page « Engine architecture », edited in place and stale since 22.10.2026; and dated decisions spread over « Goal & scope » and the stories' decision sections. The skillz plugin's `architecture-views` skill (0.7.0) sets a standard for every repository, and this was Netto's fresh-start pass.

# Decision

- `docs/architecture` holds interactive pages, one question each, drawn from the code (`atlas.js` and `atlas.css` copied unchanged from the skill), updated in the same change as the code they describe.
- Every decision taken is an ADR in `docs/architecture/decisions`, an OKF bundle, dated the day it was decided and never rewritten: a changed decision is a new ADR.
- Notion keeps the decisions under debate and links to the ADR once one is taken.
- The claude.ai maps (River, Household, Company car, Copyright royalty, Map Ideas) are kept and linked as the tax-rule reference (amounts, worked examples, documents), but no longer updated in each slice.

# Alternatives rejected

- Retire the maps: their amounts and worked examples, which the pages do not redraw, would live only in code and `official-docs/`. Offered to the operator on 29.10.2026 and declined.
- Keep updating the maps in each slice: two places to keep true. Offered and declined.

# Consequences

- Supersedes [subsystem maps as Artifacts](/2026-10-23-subsystem-maps-as-artifacts.md).
- `CLAUDE.md` points to `docs/architecture` for how the code is shaped, instead of asking for map updates in each slice.
- Notion's « Engine architecture » is replaced by a link to these pages and ADRs once the operator approves it.
