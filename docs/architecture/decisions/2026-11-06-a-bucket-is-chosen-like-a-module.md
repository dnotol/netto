---
type: adr
title: A bucket is chosen like a module, its read-only card saying what fills it
description: The map's choice is a module or a bucket (MapChoice); a chosen bucket shows BucketCard beside the map, one line per module sending money into it, read from the map's links and summing to the bar's bucket; the bar's bucket chooses it too.
status: draft
decision: proposed
decided: 2026-11-06
tags: [site]
sources:
  - resource: https://app.notion.com/p/410b6b39bd12811180dec086b9c9a15c
generated: {by: claude-opus-5-5/architecture-views, at: 2026-11-06T12:00:00+01:00}
---

# Context

The map pins the chosen module's card beside it ([the plan is the map](/2026-10-27-the-plan-is-the-map.md)) and draws every line into the five buckets ([Expenses the fifth bucket](/2026-10-28-expenses-are-the-fifth-bucket.md)), but a bucket is a name only: nothing says, in one place, what fills it. The operator asked to choose a bucket on the map or in the bar and see a card of what flows into it and from which module ([story](https://app.notion.com/p/410b6b39bd12811180dec086b9c9a15c)).

# Decision

- **The map's choice is a module or a bucket**: `Tabs/MapChoice`, a closed record with `Of(RiverModule)` and `InBucket(key)`, its `Name` the module's fold name or the bucket's key, so `plan?card=pocket` names a bucket as `plan?card=<fold name>` names a module. A bucket is not a `RiverModule`: `HandOff.Cost`, `NextModule.After` and `CompositionView` take no bucket arm. The five buckets are always drawn, so a chosen bucket survives every edit.
- **A bucket's card is read-only** (`River/BucketCard`, its view `BucketCardView`): its total, its share of the bar's start (`BucketsView`), one line per module sending money into it, net of what comes back, €0.00 left out; the pocket's card also lists the pocket line's running totals. Its lines are read from the map's links (`MapLinks.Of`, `MapBundle.Sum`), arithmetic of display only, nothing in `Netto.Core`; they add up to the bar's bucket to the cent (`BucketCardsTests`). No inputs, no « Remove », no receives line over it (each amount said once), no refusal of its own.
- **A chosen bucket keeps drawn** the lines at either end of it, and for the pocket the whole gold line; the rest fade.
- **On the map a bucket is a button** like a module, after the modules in tab order. **In the bar** the bucket's amount is a button stretched over the whole bucket, its name keeping its bubble: it chooses the bucket on the Plan tab, going to `/plan` from another tab, where the tab's title takes the focus. It is an action, not a toggle.

# Alternatives rejected

- A bucket as a `RiverModule`: every switch throwing on an unnamed module would need a bucket arm, and a bucket has no inputs, hand-off or next module.
- A line per step (contributions, personal tax…) on the card: the module's own card already says them, and the lines would no longer be one per module.
- The bucket's name as the bar's button: its « what it holds » bubble would go.

# Consequences

- `MapTab.Chosen`, `MapPanel` and `MapView.ShowingFor`, `Drawn` and `Amounts` take a `MapChoice`.
- A new bucket would be a key in `BucketsView.Keys` and its texts; nothing switches on buckets beyond `MapChoice.InBucket`.
- The choice is shown on the [map lines page](../map-lines.html).
