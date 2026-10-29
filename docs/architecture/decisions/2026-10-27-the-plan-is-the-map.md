---
type: adr
title: The Plan tab is the map, with the chosen module's card beside it
description: /plan draws the river from above and pins the chosen module's editable card beside it; the column of cards goes.
status: stable
decided: 2026-10-27
tags: [site]
sources:
  - resource: https://app.notion.com/p/406b6b39bd12813dbcc4fdb2fec5fcc3
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:5b8ed75 Make the plan the map: /plan draws the river's modules with the chosen module's card beside it, Company year on arriving, and the column of cards, its outline and its glances go"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

From 25.10.2026 the plan had two views of the same river: the Plan tab's column of module cards with its outline, and a Map tab at `/plan/map` drawing the river from above with a pinned card beside it. The operator's streamlining pass of 27.10.2026 (« The plan opens on the map, with each module's card beside it ») kept one.

# Decision

`/plan` draws the map (`Tabs/MapTab`): the modules and the money between them, and beside them the chosen module's editable card, pinned open (`PinnedCard`). The chosen module is Company year on arriving, the module a link names (`plan?card=<fold name>`, `CardLinks`), a lever just added from « Add to plan » or by « Apply », or the viewer's choice; only the viewer's choice moves the focus to its card. Every edit goes to the page's one `RiverInputs` and `Run()`, so bar, map and card agree. The « Assumed » and « Not counted yet » notes stand under the map.

# Alternatives rejected

- The column of cards, its outline and its glances, and the separate Map tab at `/plan/map`: removed by the operator's streamlining pass.

# Consequences

- The cards, their figures and the river's rules were unchanged.
- On 28.10.2026 the page was fitted to one screen: the header, the bar and the tabs became one sticky band, and the card has no scroll area of its own: the page scrolls, the map sticks.
- The edit's path from field to map is drawn on the [edit page](../edit-to-repaint.html); the map's lines on the [map lines page](../map-lines.html) ([map lines read from the cards](/2026-10-25-map-lines-read-from-the-cards.md)).
