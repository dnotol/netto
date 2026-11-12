---
type: adr
title: My plan's picture draws only what reads alone: a badge for what is lost, no word on a condition's line, a Sankey node the money its name names
description: A map box is badged only for a rule lost; a rule that holds, a tax base and a condition's line carry no badge or word, the Effects list saying them; a tax base's line says its amount; the Sankey has no key and each node says what its name names.
status: draft
decided: 2026-11-12
tags: [site]
sources:
  - resource: https://app.notion.com/p/416b6b39bd1281e1835ded965297ab67
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-12T23:00:00+01:00}
---

# Context

The story « [Every figure I read means one thing, and I see none I do not need](https://app.notion.com/p/416b6b39bd1281e1835ded965297ab67) » found labels on the picture that meant nothing without their card: « paie : oui », « ATN : oui », « abaisse », « seuil abaissé », « quotient », and « +1.617,50 € » alone in a box, which a map box, some thirteen characters wide at the narrowest, has no room to explain. [The map drawn by owner](/2026-10-29-the-map-drawn-by-owner.md) and [the map's lines read from the cards](/2026-10-29-map-lines-read-from-the-cards-the-pocket-line-summing-them.md) had each tax base and condition say « its total or state » as a badge; [My plan's picture is the map or a Sankey](/2026-11-11-my-plans-picture-is-the-map-or-a-sankey.md) gave the Sankey a key that explained how it is drawn, a « net » suffix, and node amounts that were each module's inflow: Corporate tax showed the profit entering it.

# Decision

- **A label reads alone or is not drawn.**
- **A box carries a badge only for what is lost**: the reduced rate (« taux perdu »), an amount kept that the profit does not cover (« trop gardé »), one badge where two rules lose the same thing. A rule that holds or has no effect has none.
- **A condition's line carries no word**; its rule and state are the Effects list's beside the card and the line's title.
- **A tax base has no badge**: its dotted line says its amount while a module at either end is chosen, and may stand over the condition line that runs beside it between the same two modules.
- **Two boxes keep the room their lines need** without a badge: Corporate tax while a dividend is paid, Director pay while a car or a royalty moves money (`MapLayout.BoxOf`).
- The partner's box says « imputé » over what the quotient imputes.
- **A Sankey node says the money its name names** (`SankeyView.Says`): Corporate tax the tax, Dividend what is paid out, Company year the profit its card says; Director pay keeps the pay with its contributions, its card's own headline. The Sankey has no key and no « net »; the car's contribution is said in one sentence under it.

# Alternatives rejected

- Rewording every badge to read alone: the narrowest box has no room for « taux réduit perdu : rémunération ».
- Keeping « +X » in the box with its words only heard: a sighted viewer sees an unexplained amount.
- Wider boxes: a change of the map's layout, which belongs to the story that recomposes My plan.

# Consequences

- Once accepted this amends the two 2026-10-29 decisions and the 2026-11-11 one where they give a badge to a tax base or a holding rule, a word to a condition's line, or a key to the Sankey; they stay otherwise.
- Which rule lost the reduced rate is read in the Effects list, not in the box.
- The Sankey's bands stay hidden from assistive technology, its nodes and the car's sentence being all it reads.
