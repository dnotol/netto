---
type: adr
title: The build fails on any warning, with the .NET analysers pinned
description: TreatWarningsAsErrors with analysers at 10.0-recommended, held by BuildPolicyTests.
status: stable
decided: 2026-10-24
tags: [build]
sources:
  - resource: https://app.notion.com/p/403b6b39bd1281b988c7e240f8369c20
  - resource: https://app.notion.com/p/403b6b39bd12812cbbadf7baedb6ecee
  - resource: "git:a05fdf6 Fail the build on any warning, with the analyzers pinned"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The engine-lever story of 24.10.2026 ([story](https://app.notion.com/p/403b6b39bd1281b988c7e240f8369c20)) opened with a gate where the operator chose the build policy, beside the review item « Warnings as errors, invariant globalization and a measured bar height ».

# Decision

The build treats every warning as an error (`TreatWarningsAsErrors`), with the .NET analysers pinned at `10.0-recommended`, for every project. `Architecture/BuildPolicyTests` fails when a project drops either.

# Alternatives rejected

None recorded at the time.

# Consequences

- « `dotnet build` clean » in the definition of done means no warning at all.
- `SupportedPlatform=browser` on `Netto.Core` raises CA1416 for an API the browser cannot run, which is then fixed, never suppressed ([Core is a plain library](/2026-10-18-core-is-a-plain-library.md)).
- The client publishes without ICU data the same day (`InvariantGlobalization`, [invariant culture](/2026-10-19-invariant-culture-for-numbers.md)).
