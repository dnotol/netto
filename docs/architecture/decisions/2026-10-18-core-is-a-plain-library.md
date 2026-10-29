---
type: adr
title: The tax engine is a plain library the browser can run
description: Netto.Core has no ASP.NET Core dependency and nothing the browser cannot run; the dependency graph points one way.
status: stable
decided: 2026-10-18
tags: [engine, build]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd12812bb953cffb7371eb0a
  - resource: git:6755338 Set up the solution with the engine library and its unit tests
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

With every page running in WebAssembly ([decided the same day](/2026-10-18-webassembly-only-nothing-prerendered.md)), the tax engine runs in the browser tab. The solution was set up on 18 October 2026 with the engine as its own project ([Work Item](https://app.notion.com/p/3fdb6b39bd12812bb953cffb7371eb0a)).

# Decision

`Netto.Core` is the tax engine and a plain class library. It references no ASP.NET Core package and declares `SupportedPlatform=browser`, so any API the browser cannot run raises CA1416; the fix is in the code, never a suppression. Projects reference one way: `Netto.Web.Client` → `Netto.Core`, `Netto.Web` → `Netto.Web.Client`, `Netto.AppHost` → `Netto.Web`, and the tests → `Netto.Core` and `Netto.Web.Client`.

# Alternatives rejected

None recorded at the time.

# Consequences

- `Architecture/CoreDependencyTests` fails when Core gains an ASP.NET Core dependency.
- Every tax rule lives in Core, where the unit tests reach it without a browser ([one year simulation](/2026-10-19-one-year-simulation-in-the-laws-order.md)).
- Anything Core does runs on the page's one thread: long searches wait for the first paint ([moves searched after paint](/2026-10-23-moves-searched-once-per-baseline-after-paint.md)).
