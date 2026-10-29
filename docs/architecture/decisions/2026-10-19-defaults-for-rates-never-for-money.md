---
type: adr
title: Defaults exist for rates and statuses, never for money, and every input says where its value comes from
description: InputOrigin marks each input as example, default, derived or yours; no net figure is built on an amount the user did not give without saying so.
status: stable
decided: 2026-10-19
tags: [product, site]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:acaac88 Give the river's inputs one origin model, starting with the municipal rate
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

The river opens on an example so it is never empty, but a figure built on an amount the user never gave would mislead. « Goal & scope » ([Notion](https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635)) and the River Map's rules of 19 October 2026 (« Defaults for rates and statuses, never for money », « It opens on a labelled example ») set the rule.

# Decision

Every input of the river carries an `InputOrigin` (`Web.Client/River/InputOrigin.cs`): `Example`, `None`, `Default`, `Derived` or `Yours`, shown beside the field as a chip. Defaults exist for rates and statuses only. An amount the example gives says « example » until the user types theirs; an amount the example holds none of is `None` and shows no chip (refined on 23 October 2026); a typed zero is yours. A module whose amounts are not filled in is skipped.

# Alternatives rejected

None recorded at the time.

# Consequences

- `WebClient/River/ExampleOriginsTests` holds which amounts the example gives.
- A combustion car without its CO2 is refused, never guessed: the same rule applied to facts.
- Saved plans store each value with its origin ([saved plans](/2026-10-22-saved-plans-in-a-strict-versioned-format.md)).
