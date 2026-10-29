---
type: adr
title: The map's lines are read from the cards' own views, never computed by the page
description: Superseded - MapLinks.Of reads each card's figure and drain lines; MapBundle.Sum adds them; MapCardsTests holds every amount to a card's.
status: deprecated
superseded_by: /2026-10-29-map-lines-read-from-the-cards-the-pocket-line-summing-them.md
decided: 2026-10-25
tags: [site]
sources:
  - resource: https://app.notion.com/p/404b6b39bd128141985adff511a771c9
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:1f821b1 Read the map's links from the cards' own lines: what each module passes on, ends in a bucket or adds to a tax base"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

« I see on one map how my modules act on each other » ([story](https://app.notion.com/p/404b6b39bd128141985adff511a771c9)), built attended in four slices, drew the river from above with the money between modules. A map doing its own arithmetic could disagree with the cards beside it.

# Decision

Every line of the map is a line its card already shows: its drains, a lever's hand-off, the split's sides, the car's and the vouchers' figure lines, never arithmetic of the page, and none of €0.00. `Tabs/MapLinks.Of(inputs, run)` reads the cards' own views (`River/ModuleViews.cs` and each card's figures); lines between the same two ends of the same kind are bundled and summed by `MapBundle.Sum`, which rounds each to the cent. A link is of one of four kinds, told apart by its line: money passed on, money into a bucket, money that only changes a tax base, and a condition one module sets on another ([the year's conditions](/2026-10-25-the-engine-names-the-years-conditions.md)).

# Alternatives rejected

None recorded at the time.

# Consequences

- `MapCardsTests` renders the cards open from the same run and holds every link and bundle amount to a figure the card draws, or their sum.
- A new module is its place in `MapModules.Order` and `MapLayout.Place`, its lines in `MapLinks.Of` (a new destination an arm in `MapLinks.EndOf`, a new pair a route family in `MapLayout`); `MapLayoutTests` and `MapAmountsTests` hold every composition.
- The legend's filter is a viewer preference (`MapLinkFilter`, key `netto.map.lines`).
- On 27.10.2026 the map became the Plan tab ([the plan is the map](/2026-10-27-the-plan-is-the-map.md)); the pipeline is drawn on the [map lines page](../map-lines.html).
