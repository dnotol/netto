---
type: adr
title: Running totals are the map's alone: the company's line and the pocket line, never a card
description: No card shows the profit left after it or the pocket so far; the map's company line reads its stretches from HandOff, its pocket line sums the cards, and MapCardsTests holds both to conservation.
status: draft
decided: 2026-11-12
tags: [site]
sources:
  - resource: https://app.notion.com/p/416b6b39bd1281e1835ded965297ab67
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-12T18:00:00+01:00}
---

# Context

[The map's lines are read from the cards' own views, the pocket line alone summing them](/2026-10-29-map-lines-read-from-the-cards-the-pocket-line-summing-them.md) made every amount on the map a figure its card shows, the pocket line's totals the one exception. The cards therefore carried the engine's running profit (« Reste après les titres-repas … · vers les indemnités ») so that the company's line had a figure to read, and the pocket's bucket card repeated the pocket line's totals under the same labels as its sources. The story « [Every figure I read means one thing, and I see none I do not need](https://app.notion.com/p/416b6b39bd1281e1835ded965297ab67) » found both read as second values for the same thing.

# Decision

- **A card shows what its module does with the money, never what is left after it**: no lever's card carries the running profit, in its summary or its lines, and the pocket's bucket card lists one amount per source.
- **The map's company line is a running total like the pocket line**: `MapLinks` reads each stretch from `HandOff.Drain`, the engine's own step, not from a card's line.
- `MapCardsTests` holds every other amount to its card's figure, each stretch of the company's line to the stretch above less the lever's share, the last to what the split shares, and the pocket line as before.

# Alternatives rejected

- The running profit kept on the cards so the map reads it there: it is the figure the audit found useless on a card, pointing at the next lever even when that lever holds nothing.
- No amount on the company's line: the map would no longer say how much profit reaches the split.

# Consequences

- Once accepted this amends the 2026-10-29 decision, which stays otherwise: the pocket line is no longer the only sum the map draws that no card shows.
- Once accepted this also amends [a bucket is chosen like a module](/2026-11-06-a-bucket-is-chosen-like-a-module.md): a bucket's card gives no share, the bar's reach being the one percentage, and no running total.
