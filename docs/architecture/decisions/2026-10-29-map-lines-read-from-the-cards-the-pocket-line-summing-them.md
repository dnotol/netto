---
type: adr
title: The map's lines are read from the cards' own views, the pocket line alone summing them, and its legend is a key
description: MapLinks.Of reads each card's lines; the pocket line's totals are the only sums across cards, held by MapCardsTests; lines of money are always drawn, a module's tax-base and condition lines only while it is chosen.
status: stable
decided: 2026-10-29
tags: [site]
sources:
  - resource: https://app.notion.com/p/408b6b39bd1281d4a428cfda4b307820
  - resource: https://app.notion.com/p/408b6b39bd1281e7ad51d85a5454c194
  - resource: https://app.notion.com/p/408b6b39bd1281f0ac1dc5cbece2ae0f
  - resource: https://webspace.science.uu.nl/~telea001/uploads/VACourse/Shneiderman96.pdf
generated: {by: claude-opus-5-5/architecture-views, at: 2026-10-29T22:00:00+01:00}
---

# Context

[The map's lines are read from the cards' own views, never computed by the page](/2026-10-25-map-lines-read-from-the-cards.md) allowed the map no arithmetic beyond summing one card's lines between the same two ends, and made the legend a filter the viewer kept (`MapLinkFilter`, `netto.map.lines`). The map drawn by owner ([decision](/2026-10-29-the-map-drawn-by-owner.md)) needs a running total of what has reached the director, which adds several cards' lines; and the operator found the filter served no case: no one wants money passed on without money into a bucket, choosing a module already focuses the map, and the tax-base and condition lines mostly repeated their badges, often to say « no effect ».

# Decision

- Every line of the map is a line its card already shows, read by `Tabs/MapLinks.Of(inputs, run)` from the cards' own views; lines of one kind between the same two ends are bundled and summed by `MapBundle.Sum`, each rounded to the cent; none of €0.00. A link is money passed on, money into a bucket, a change to a tax base, or a condition the engine names.
- **The one exception is the pocket line** (`MapPocketLine`): each stretch is the sum of the card lines into the pocket above it less those out of it, of one kind, never a tax figure; its last total is the bar's pocket.
- **The legend is a key** (`MapKey`, `MapKeyView`): it says what each line style means, with nothing to tick. Lines of money are always drawn; a tax-base or condition line is drawn only while a module at either of its ends is chosen (`MapView.ShowingFor`), its total or state still a badge.
- **Beside the chosen card**, under what it receives, its tax-base and condition lines are listed in words, each naming the other module as a button (`MapEffectsView`, `MapEffectText`); hovering such a line shows the same sentence and marks its item.

# Alternatives rejected

- Running totals left out, the pocket line saying nothing: the director would read what reached them only on the bar.
- Keeping the four tick boxes, or one toggle for tax bases and conditions: no case needs one kind of money without the other, and the toggle would show every rule line at once again.
- A word drawn on each tax-base or condition line: no room at 34rem without reworking the layout; a hover alone reaches neither the keyboard nor a screen reader.

# Consequences

- `MapCardsTests` holds every amount to a card's figure or their sum, and each running total to the lines above it, its last to the bar's pocket.
- A stored `netto.map.lines` is no longer read; no viewer preference shapes the map.
- Amounts are placed once per run from every link, so a line drawn only on choosing never moves the others.
