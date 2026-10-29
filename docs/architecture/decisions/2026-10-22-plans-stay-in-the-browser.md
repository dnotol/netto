---
type: adr
title: The user's plans stay in their browser or a file they export, never on a server
description: No accounts and nothing sent to a server; saved plans live in localStorage and move between browsers as an exported JSON file.
status: stable
decided: 2026-10-22
tags: [storage, product]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://app.notion.com/p/3fdb6b39bd1281babbb9d47babbc23e6
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:29ebfb6 Save my plans in a side panel, one starred as Today, and measure the river against it
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

Measuring a move needs a baseline, the plan the user has today, so plans had to be saved (« I see what each recommended move gains me », [Notion](https://app.notion.com/p/3fdb6b39bd1281babbb9d47babbc23e6)). A plan holds a director's pay, company figures and household. « Goal & scope » ([Notion](https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635)) and the River Map's rule of 22 October 2026 (« Plans stay in your browser, never on a server ») set where.

# Decision

User data stays in the browser: no accounts, nothing sent to a server. `SavedPlans` keeps the plans in `localStorage` under `netto.plans`, through `IBrowserStorage` (`LocalStorage`, `browser-storage.js`). Saving never replaces another plan. One plan can be starred « Today », the baseline of every gain. The river opens on the plan last opened. Export writes the same document to a JSON file and import reads one back, so plans move between browsers through the user's own files.

# Alternatives rejected

None recorded at the time.

# Consequences

- It rests on every page running in WebAssembly with no endpoint for user figures ([WebAssembly only](/2026-10-18-webassembly-only-nothing-prerendered.md)).
- Clearing the browser's site data loses unsaved and unexported plans; nothing can restore them.
- Viewer preferences (`netto.folds`, `netto.theme`, `netto.map.lines`) live beside the plans, never inside them.
- Private hosting for trusted people is planned since the pivot ([a reference tool](/2026-10-24-a-reference-tool-not-a-product.md)); it would serve the same files and keep this rule.
