---
type: adr
title: Each complex subsystem has a map of its own, an Artifact updated in each slice
description: Superseded - mechanisms beyond one card got claude.ai maps updated slice by slice; replaced by repo pages and ADRs.
status: deprecated
superseded_by: /2026-10-29-architecture-pages-and-adrs-in-the-repo.md
decided: 2026-10-23
tags: [docs]
sources:
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: https://claude.ai/artifact/AhoWfP6DmKmqWatT2xAc1j
  - resource: https://claude.ai/artifact/H78okHrfkQ1Fjs3FtCYGCY
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The River Map, a claude.ai Artifact, drew the whole river with a card per module and the dated rules it follows. The company car and the copyright royalty had outgrown a card on it.

# Decision

A mechanism that reaches beyond one card or one layer is drawn on an Artifact of its own, with what it hands the year, how it computes, its amounts and their documents, its code and its decisions; the River Map keeps the river from above and links to it. The company car and the copyright royalty moved to their own maps with their diagrams, dated rules and open points; the payment calendar stayed on the River Map. Each map is updated in the slice that changes what it draws (`CLAUDE.md`).

# Alternatives rejected

None recorded at the time.

# Consequences

- The household got its own map on 28.10.2026 ([household](https://claude.ai/artifact/Xq92UJySyaDCQhjKMyE9oR)).
- Only the lead publishes a map; implementers report what their slice changed on it.
- Superseded on 29.10.2026 by [architecture pages and ADRs in the repo](/2026-10-29-architecture-pages-and-adrs-in-the-repo.md): the maps stay as the tax-rule reference but are no longer updated in each slice.
