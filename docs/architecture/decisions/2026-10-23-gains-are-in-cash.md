---
type: adr
title: Every gain is measured in cash, and a company car is weighed in cash against the same car bought privately
description: The muddy pocket goes; the gain over Today is cash alone, and the car card states what the car takes from the pocket.
status: stable
decided: 2026-10-23
tags: [product, site]
sources:
  - resource: https://app.notion.com/p/402b6b39bd12815c9a13db118e4ab3a9
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: https://claude.ai/artifact/AhoWfP6DmKmqWatT2xAc1j
  - resource: "git:7bc5402 Weigh the gain over Today in cash alone"
  - resource: "git:85f1c9b Drop the muddy pocket from the engine"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

On 22.10.2026 a company car got a bucket of its own on the bar, the « muddy pocket », and the gain over Today counted it. The operator decided to drop it in « I read my plan in figures, and my car is weighed in cash » ([story](https://app.notion.com/p/402b6b39bd12815c9a13db118e4ab3a9)).

# Decision

The gain over Today, and every gain the moves measure, is in cash and nothing else (`BaselineGain`). A car's costs are a cost of the company, left out of the river's start like its running costs. The car card asks what the same car would cost the director privately, none until given and read by no rule; it states in cash what the car takes from the pocket, with it and without it at the same pay, and once that private cost is given, what the company car gains or costs against it.

# Alternatives rejected

- The muddy pocket of 22.10.2026, a bucket holding a car's value beside the cash: reversed by the operator on 23.10.2026.

# Consequences

- The bar had four buckets whatever the plan held, until a fifth, Expenses, joined on 28.10.2026 for the car's costs and INASTI's company contribution ([Expenses is the fifth bucket](/2026-10-28-expenses-are-the-fifth-bucket.md)); the cash comparison stood.
- Saved plans moved to format 6 for the private cost; a plan saved before opens with no private cost.
- `MoveSearch` compares the `NetInPocket` of two runs, a cash figure ([moves found greedily](/2026-10-22-moves-found-greedily-from-a-baseline.md)).
