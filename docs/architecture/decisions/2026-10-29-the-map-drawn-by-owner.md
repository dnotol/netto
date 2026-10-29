---
type: adr
title: The map is drawn by whose money each module moves, every euro reaching you on one pocket line
description: Three columns (you, ways across, the company); the company's line down its own with each lever's share across to a gate; what reaches you joins one gold pocket line with its running total; an amount is said once.
status: stable
decided: 2026-10-29
tags: [site]
sources:
  - resource: https://app.notion.com/p/408b6b39bd1281d4a428cfda4b307820
  - resource: https://app.notion.com/p/408b6b39bd1281bfa280d16d78ce86cc
  - resource: https://app.notion.com/p/408b6b39bd1281a1baccd8a20efdebd8
  - resource: https://link.springer.com/content/pdf/10.1007/3-540-63938-1_67.pdf
generated: {by: claude-opus-5-5/architecture-views, at: 2026-10-29T22:00:00+01:00}
---

# Context

The map ([the plan is the map](/2026-10-27-the-plan-is-the-map.md)) stacked Company year, every lever and the split in one middle chain that belonged neither to the director nor to the company, though the household and the payslip are the person's and the car's costs and the expenses the company's. The story « I read on the map whose money each module moves » ([story](https://app.notion.com/p/408b6b39bd1281d4a428cfda4b307820)) regrouped it, proved at the narrowest map (34rem) over 800 compositions by two prototypes before it was built. After the first build the operator judged the top more readable and the bottom still a tangle: four long parallel gold lines and the pay's taxes crossing them. Research put line crossings first (Purchase, 1997), then short continuous paths and an amount said once where the eye already is.

# Decision

- **Three columns by owner** (`MapOwner`: You, Across, Company), labelled: Household above Director pay in yours; every lever in the plan, the royalty and the dividend in the middle as dashed gates; Company year, the split and Corporate tax in the company's. Rows keep the river's order.
- **The company's line** runs down its own column from Company year into the split; each lever takes its share across to its gate (`MapEnd.Beside`), the share its card's « The company pays » figure, the line's amount after each lever its card's « Left after … ». The car takes no share: its costs leave the line into Expenses, the viewer's contribution joins it.
- **A gate says what crosses and what reaches you** (`MapGates`, `MapGateWords`); the pay crosses under a « Pay · €x » pill; Household's box says its quotient.
- **The pocket line**: every line into the pocket joins one gold line down the left of your column at its module's point (`MapPocketLine`, `MapEnd.OnPocketLine`); after each join it says what has reached you so far, and its last total is the bar's pocket. The car's contribution leaves it at the car's row. No total is said out of a point money only leaves, nor below zero.
- **An amount is said once** (`MapLink.Unsaid`): a join, the pay (its pill), a tax base into Corporate tax (its badge), and a line into a gate whose box says the same amount carry none; every other line keeps its amount on a stretch of its own.
- **No plan crosses more lines than its baseline** (`MapCrossings.txt`, held by `MapLayoutTests`).

# Alternatives rejected

- Zones only on the old layout, and owner frames around the old chain: neither put a lever on the border between the two sides it acts on.
- A share for the car off the company's line: its costs are not paid to the viewer.
- Each gold line running down into the pocket bucket: long parallel lines whose owner cannot be told without tracing them, crossed by the pay's taxes (every lever: 55 crossings, 8 with the pocket line).
- An amount on every take as well as in its gate: each figure twice.

# Consequences

- The narrowest map with every lever is 730 px tall, against 577 before the story (accepted by the operator).
- `MapLayoutTests` and `MapAmountsTests` hold every composition at 34rem and 1,184 px; `MapCardsTests` holds each running total to the lines above it and the last to the bar's pocket.
- A new module reaching the pocket joins the pocket line at its row; a new lever is a gate with its lines in `MapGates`.
- The lines and the legend: [map lines read from the cards, the pocket line summing them](/2026-10-29-map-lines-read-from-the-cards-the-pocket-line-summing-them.md); the pipeline is drawn on the [map lines page](../map-lines.html).
