---
type: adr
title: An older saved plan is brought to the current format one version at a time, by frozen literal patches
description: PlanStorage.Patches add each version's new fields as frozen JSON; StoredPlanDocuments holds a document of every version.
status: stable
decided: 2026-10-24
tags: [storage]
sources:
  - resource: https://app.notion.com/p/403b6b39bd1281b988c7e240f8369c20
  - resource: https://app.notion.com/p/401b6b39bd12817eb786e606c9865f9f
  - resource: "git:5d6e19d Bring an older saved plan to the current format one frozen patch at a time"
  - resource: "git:e9a4381 Hold every saved-plan format to a frozen document and the inputs it means"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

Saved plans are read strictly, as saved or not at all ([strict versioned format](/2026-10-22-saved-plans-in-a-strict-versioned-format.md)). The review of 22.10.2026 found that the migrations copied whole records per format and read their defaults from the live example (`RiverInputs.Example`), so a change to the example would silently change what an old plan reads as. It was fixed in the engine-lever story of 24.10.2026.

# Decision

An older document is brought forward one version at a time: `PlanStorage.Upgraded` runs `Patches[from-1]` for each version up to the current one, each patch adding only the members its version brought, as frozen literal JSON, never from `RiverInputs.Example`, and refusing a member already present. `StoredPlanDocuments` (tests) holds a captured document of every version and the inputs it means.

# Alternatives rejected

- Copying whole records per format and reading defaults from the live example: the review finding of 22.10.2026.

# Consequences

- A new input is a new field, a new `PlanStorage.Version` (`RiverInputsShapeTests` freezes the shape), one patch adding it as frozen JSON, and the version's document in `StoredPlanDocuments`. The version is 9 as of 29.10.2026.
- A story may add to the format it raised before it leaves its branch, so some fields joined a version without raising it.
- A plan that patches and reads cleanly is stored again in the current format; one that does not is kept as stored. Such a plan is written back inside a document stamped with the current version, so a later build cannot patch it again.
