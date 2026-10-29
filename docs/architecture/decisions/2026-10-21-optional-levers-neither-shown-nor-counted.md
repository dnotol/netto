---
type: adr
title: Optional levers join the plan from a menu, and what is out neither shows nor counts
description: PlanComposition holds which levers are in; an absent lever is its None in the Scenario, its values kept for when it comes back.
status: stable
decided: 2026-10-21
tags: [site, product]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:cc4d968 Let the optional modules join or leave the plan from a Composition menu
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

With vouchers, allowances and rent built, every card showed whether or not the user used it, and an empty card still sat in the river. « Goal & scope » ([Notion](https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635)) and the River Map's rule of 21 October 2026 (« The levers join your plan from the Composition menu ») made them optional.

# Decision

Each optional module is a switch, « in your plan », in a menu (today « Add to plan », `CompositionMenu`). `PlanComposition` holds which are in; `RiverInputs.Scenario` gates each lever by it, passing the lever's `None` while it is out. What is in the plan shows and counts; what is not neither shows nor counts, so every figure traces back to a card on the page. Taking a lever out keeps its values for when it comes back, and a notice offers Undo. Company year, the split, Director pay, Corporate tax and Dividend are always there.

# Alternatives rejected

None recorded at the time.

# Consequences

- A new lever is also an `OptionalModule`, a flag of `PlanComposition`, a gate in `RiverInputs.Scenario` and the menu's texts, as CLAUDE.md lists.
- Applying a move is the one other action that adds a module ([moves](/2026-10-22-moves-found-greedily-from-a-baseline.md)).
- The composition is stored with each saved plan ([saved plans](/2026-10-22-saved-plans-in-a-strict-versioned-format.md)).
- The household joined the same way on 28 October 2026 ([household](/2026-10-28-household-taxed-jointly.md)).
