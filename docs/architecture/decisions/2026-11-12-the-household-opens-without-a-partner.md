---
type: adr
title: The household opens without a partner: adding it to the plan moves nothing until the viewer ticks the box
description: The example's household status is Alone by default; the v8 to v9 patch gives a plan that held no household the same status; the saved-plan version stays 9 and a plan saved at 9 keeps what it holds.
status: stable
accepted_by: human:Johnny Amadeus
decided: 2026-11-12
tags: [site, plans]
sources:
  - resource: https://app.notion.com/p/416b6b39bd1281e1835ded965297ab67
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-12T23:30:00+01:00}
---

# Context

[A couple is taxed jointly](/2026-10-28-household-taxed-jointly.md) said nothing of the status a household opens with; the example opened it married, by default, with a partner of no income. Adding « Your household » to the example plan therefore raised the pocket by the marital quotient before the viewer had said anything, which the story « [Every figure I read means one thing, and I see none I do not need](https://app.notion.com/p/416b6b39bd1281e1835ded965297ab67) » found misleading. The operator decided on 2026-11-12 that a partner is the viewer's to tick.

# Decision

- **The household's status is Alone by default** (`RiverInputs.Example`, `InputOrigin.Default`): a status may have a default, as [defaults exist for rates and statuses, never for money](/2026-10-19-defaults-for-rates-never-for-money.md) allows, and this one changes no figure.
- **The patch from version 8 to version 9 gives a plan that held no household the same status** (`PlanStorage`, the literal `PartnerAssumed`). This rewrites one frozen literal of a patch already on main, against [plans migrated a version at a time](/2026-10-24-plans-migrated-a-version-at-a-time.md): it is allowed here because that household was out of the plan, so no figure of any stored plan moves, and the alternative is a new version for a default.
- **The version stays 9.** A plan saved at version 9 is read as saved: one that stored the old default keeps it, and shows a ticked partner when its household is added.

# Alternatives rejected

- Raising the version to rewrite stored defaults: a format change for a status that moves no figure of a saved plan.
- Leaving the patch as it was: a plan migrated from version 8 would open its household with a partner while a new plan opens it alone.

# Consequences

- Adding the household to a plan leaves the pocket where it was; the fixtures that want a partner say so.
- A version-8 plan opened after this change gets a different household default from one opened before it.
