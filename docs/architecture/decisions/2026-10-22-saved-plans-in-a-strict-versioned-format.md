---
type: adr
title: Saved plans use a versioned format of their own, read as saved or not at all
description: PlanStorage maps RiverInputs to its own records with a version; a plan is read strictly, never filled with defaults, and an unreadable document is copied aside.
status: stable
decided: 2026-10-22
tags: [storage]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281babbb9d47babbc23e6
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:679ea71 Keep saved plans in a versioned format that reads a plan as it was saved or not at all
  - resource: git:26850d6 Say in CLAUDE.md how saved plans are kept and compared
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

Saved plans ([plans stay in the browser](/2026-10-22-plans-stay-in-the-browser.md)) outlive the code that wrote them, and every new input changes `RiverInputs`. A plan read back with a missing figure silently defaulted would show a pocket the user never planned. Decided on 22 October 2026 with the first saved plans.

# Decision

`River/PlanStorage.cs` stores plans in a document of its own (`StoredBook`: `version`, `today`, `opened`, `plans`), never `RiverInputs` serialised. Each value is stored with its origin. Reading is strict: the source-generated serializer disallows unmapped members and requires every constructor parameter, so a plan reads as saved or is unreadable, never filled with defaults. A document Netto cannot read is copied to `netto.plans.unreadable` before anything overwrites `netto.plans`. A document from a newer Netto is refused the same way. A new input is a new field and a new version.

# Alternatives rejected

- Serialising `RiverInputs` directly: rejected, since the stored shape would change with every refactoring of the inputs (CLAUDE.md, « never `RiverInputs` serialised »).

# Consequences

- `WebClient/River/RiverInputsShapeTests` freezes the inputs' shape; `PlanStorageTests` and `SavedPlansTests` hold the reading.
- Older versions are brought forward a version at a time ([plans migrated a version at a time](/2026-10-24-plans-migrated-a-version-at-a-time.md)); the version is 9 today.
- Raising `PlanStorage.Version` is exclusive to one slice of a story.
- An unreadable older plan is written back inside a document stamped with the current version, so a later build cannot upgrade it (noted while drawing the pages on 29 October 2026).
