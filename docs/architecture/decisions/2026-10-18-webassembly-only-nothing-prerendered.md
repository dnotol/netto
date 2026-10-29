---
type: adr
title: Every page runs in WebAssembly and nothing is prerendered
description: The server only serves files; every component runs in the browser, so no user figure ever reaches a server.
status: stable
decided: 2026-10-18
tags: [site]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd12812bb953cffb7371eb0a
  - resource: git:08d1b26 Add the Blazor WebAssembly app and its ASP.NET Core host
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

Netto works on a director's own pay, dividends and household. The project skeleton ([Work Item](https://app.notion.com/p/3fdb6b39bd12812bb953cffb7371eb0a)) set up a Blazor Web App on 18 October 2026 and had to choose where components run.

# Decision

Every page is a WebAssembly component and none is prerendered. `Netto.Web/Components/App.razor` sets `InteractiveWebAssemblyRenderMode(prerender: false)` once, on `<Routes>` and `<HeadOutlet>`; pages carry no `@rendermode` of their own. `Netto.Web` registers only `AddInteractiveWebAssemblyComponents` and serves the static assets and the bundle: it has no API and no endpoint that receives a user's figures. A splash shows until `.netto-app`, the layout's root, exists.

# Alternatives rejected

None recorded at the time.

# Consequences

- The tax engine has to run in the browser, which is why `Netto.Core` is a plain library ([Core is a plain library](/2026-10-18-core-is-a-plain-library.md)).
- The page is empty while the runtime downloads; the splash covers it.
- Prerendering, a server component or an endpoint receiving user figures needs the operator, as CLAUDE.md says.
- `--no-launch-profile` runs the host in Production, which serves no WebAssembly assets; worktrees serve `Netto.Web` with the `http` profile.
- User data can stay in the browser ([plans stay in the browser](/2026-10-22-plans-stay-in-the-browser.md)).
