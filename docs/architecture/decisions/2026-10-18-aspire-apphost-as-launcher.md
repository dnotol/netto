---
type: adr
title: The Aspire AppHost is only a local launcher
description: Netto.AppHost starts Netto.Web under the Aspire dashboard and nothing else; ServiceDefaults waits for an API.
status: stable
decided: 2026-10-18
tags: [build]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd12812bb953cffb7371eb0a
  - resource: git:557bdd1 Add the Aspire AppHost that launches the web host locally
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

The project skeleton of 18 October 2026 ([Work Item](https://app.notion.com/p/3fdb6b39bd12812bb953cffb7371eb0a)) wanted one way to start the app from Rider and from the command line.

# Decision

`Netto.AppHost` is an Aspire AppHost with one resource, `AddProject<Projects.Netto_Web>("web")`. It is a launcher only: no ServiceDefaults (telemetry, health checks, resilience) until there is an API to observe, as its project file says. DCP and the dashboard come from NuGet (`AspireUseCliBundle` false, ASPIRE010 silenced), so Rider and `dotnet run` launch it without the Aspire CLI. The app answers on port 5274 and the dashboard on 15187.

# Alternatives rejected

None recorded at the time.

# Consequences

- The AppHost carries a `MessagePack` security pin, to be dropped once Aspire's own graph resolves a patched version.
- Rider's run holds ports 5274 and 15187, so a worktree serves `Netto.Web` alone on 5290.
- Nothing runs besides the web host: the architecture pages draw the AppHost only as a launcher.
