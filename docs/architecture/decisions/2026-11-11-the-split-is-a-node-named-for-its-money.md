---
type: adr
title: The split is a node of the map named for the money it holds, not a card with inputs
description: « To share » (« À partager ») keeps the split's place on the map with a read-only card; the pay is typed on the Director pay card; the pay curve and the best pay are in Optimise; no card of My plan suggests a payment.
status: stable
accepted_by: human:Johnny Amadeus
decided: 2026-11-11
tags: [site]
sources:
  - resource: https://app.notion.com/p/411b6b39bd1281389895d66c562a99e9
  - resource: https://claude.ai/artifact/LxwVWoynXrEmVXA8pFskz4
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-11T14:00:00+01:00}
---

# Context

« The split » (« La répartition ») was a module of the plan with a card: the pay's slider and field, the pay curve with « move to the best pay », and the amounts each side takes ([the split searches the best pay](/2026-10-19-the-split-searches-the-best-pay.md)). It named a decision, where every other node of the map names money; and its best-pay button was a suggestion inside the plan, which My plan no longer makes ([three views](/2026-11-11-three-views-one-question-each.md)). Moving the curve and the best-pay button to Optimise amends where [the split searches the best pay](/2026-10-19-the-split-searches-the-best-pay.md) drew them; the search itself is unchanged. Decided with the operator on 2026-11-11 ([story](https://app.notion.com/p/411b6b39bd1281389895d66c562a99e9)).

# Decision

- **The node is named for the money it holds**: « To share » / « À partager », what the levers and the company's costs leave: the profit before pay plus a royalty, the amount the line into it says. It keeps the split's place on the map, its ends and its name in links (`plan?card=split`), so no plan's layout changes. Chosen, it shows a read-only card of what each side takes, as a bucket does ([a bucket is chosen like a module](/2026-11-06-a-bucket-is-chosen-like-a-module.md)).
- **Its inputs are typed where their money is**: the pay on the Director pay card, with the company present or skipped; the royalty on the Copyright card and what the company keeps on the Dividend card, where they already were.
- **Its conditions stand on the module each constrains**: the reduced rate and the minimum pay on Corporate tax, the reserve on Dividend, the royalty's two smallest pays on Copyright. On the map the condition lines still run between the module and the node.
- **The pay curve and the best pay are in Optimise**: the curve is the tuning question drawn. Clicking it and « move to the best pay » set the plan's own pay, said as such; a listed move is still applied only by « Apply ».
- **No card of My plan suggests a payment**: the « pay X more by a date » lines leave the Director pay and Corporate tax cards; the surcharge stays as a figure, its top-up in Optimise's advance-payment move and the « by date » lens.

# Alternatives rejected

- « Profit before pay » as the node's name: Company year's summary already says « before your pay » of the profit before the levers.
- Removing `RiverModule.Split`: a new kind of end across every route of the map, for the same drawing.
- A node that cannot be chosen: its lines' highlighting, its « receives » line and keyboard reach would go.
- The amount in the node's box: said twice, and every plan's layout would change.

# Consequences

- The map's lines of the split are read from the read-only card's figures ([map lines read from the cards](/2026-10-29-map-lines-read-from-the-cards-the-pocket-line-summing-them.md)).
- A refusal of the pay is the Director pay card's, with or without the company (`RiverErrors.Owner`).
- The search (`SplitSearch`) and the concept, the split between pay and dividend, keep their names.
