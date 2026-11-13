---
type: adr
title: A lever's card leads with what the lever adds to the pocket, the plan with it less the plan without
description: One client view model, LeverGain, measures each lever in the plan as two runs at the same pay; the card's summary leads with that gain and ends on the gain over the payout; the payout is a line.
status: stable
accepted_by: human:Johnny Amadeus
decided: 2026-11-12
tags: [site]
sources:
  - resource: https://app.notion.com/p/416b6b39bd1281e1835ded965297ab67
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-12T18:00:00+01:00}
---

# Context

The story « [Every figure I read means one thing, and I see none I do not need](https://app.notion.com/p/416b6b39bd1281e1835ded965297ab67) » found that a lever's card summary read as its gain while stating its payout: « 2.210,20 € dans votre poche · 100 % pour vous » for vouchers that add far less once the dividend they replace is counted. Only the car's and the royalty's cards measured the plan with the lever against the plan without (`CarComparison`, `CopyrightComparison`, following [every gain is measured in cash](/2026-10-23-gains-are-in-cash.md)), each in its own code and further down the card.

# Decision

- **One measure for every lever in the plan**: `River/LeverGain`, a client view model, gives the pocket of the plan's run less the pocket of the run with the lever taken out, at the pay the plan uses, and says when the pay is held. It is a difference of two engine runs, as a share is a division of the engine's figures: no tax rule. It is none while the plan is refused or the lever is out.
- **The card's summary leads with it**, as more, less or none in the pocket, and ends on one rate, the gain over what the company pays out; the car, whose payout is its costs, ends on no rate, nor does a lever that takes from the pocket.
- **What the lever pays out is a figure line**, labelled as a payout.
- The car's and the royalty's comparisons are built on the same measure.
- The run is made only for the card on screen and kept until the inputs change.

# Alternatives rejected

- The measure in `Netto.Core`: Core already has `PisteSearch`, but it measures optimised against optimised, another question; a plain difference of two runs needs nothing the client lacks.
- The summary keeping the payout with « % pour vous »: true, and the figure that read as a gain.
- No rate on any lever: a card's summary ends on one rate, and the gain over the payout is the one that speaks to the gain.

# Consequences

- My plan still suggests nothing: the gain of a lever already in the plan is a fact about the plan, not a move.
- A lever's card costs one more run while it is chosen.
- The « Add to plan » menu still words a payout as what a lever brings; measuring it there would cost a run per lever at each draw.
