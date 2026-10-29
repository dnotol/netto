---
type: adr
title: Time is a lens over the river, not a module in it
description: The year's steps carry no date; when each bucket is paid is a separate view over the whole river.
status: stable
decided: 2026-10-19
tags: [product, engine]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

Advance payments, INASTI's quarters and the assessment a year later all say when money moves, but the river follows where it goes. The River Map's rule of 19 October 2026 (« Time is a lens, not a module ») and « Goal & scope » ([Notion](https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635)) kept the two apart.

# Decision

The river has no time axis: a `Step` is the year's total, undated, and the map and the bar draw no time. When each bucket is paid is a view over the whole river, built from the same year's figures, never a module the money flows through.

# Alternatives rejected

None recorded at the time.

# Consequences

- The payment calendar arrived as a record beside the ledger, not as steps with dates ([payment calendar](/2026-10-22-payment-calendar-beside-the-ledger.md)), and became the When to pay tab ([tabs by purpose](/2026-10-23-tabs-by-purpose.md)).
- Every amount the calendar shows is the river's own; the calendar only says when.
