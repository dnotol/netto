---
type: adr
title: The payment calendar is a record the engine builds beside the ledger, and steps carry no date
description: When each payment falls is Calendar/PaymentCalendar, built from the year and a date; a step stays the year's total, undated.
status: stable
decided: 2026-10-22
tags: [engine]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281688bb5c59f769478c9
  - resource: https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:4da1923 Build the payment calendar's advance-payment schedules from each surcharge's top-up by deadline"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

« I know what to pay and when » ([story](https://app.notion.com/p/3fdb6b39bd1281688bb5c59f769478c9)) needed the dates of the advance payments, the monthly pay, INASTI's quarters and the assessments' balances. Time had already been declared a lens over the river, not a module in it ([time is a lens](/2026-10-19-time-is-a-lens.md)). The decision was taken unattended on 22.10.2026, to review, and recorded on the River Map and on « Engine architecture ».

# Decision

The engine builds the calendar from the year and a date, as a record beside the ledger like the pay statement: `Netto.Core/Calendar/PaymentCalendar`, through `PaymentCalendar.Of(year, date)`. A step stays the year's total and carries no date; the calendar only places the year's own figures in time. The first deadline not yet past on the date, and what more by it cancels each surcharge (`FirstOpen`, `TopUps`), are worked out there once. A year or a region without the rules of its deadlines gives a null calendar, never an exception.

# Alternatives rejected

None recorded at the time.

# Consequences

- The calendar applies nothing; the moves stay the one place a move is applied ([moves found greedily](/2026-10-22-moves-found-greedily-from-a-baseline.md)).
- The first open deadline and its top-up are the one figure the payment moves, the « more by » lines (`River/MoreBy`, since 24.10.2026) and When to pay read.
- First a card at the end of the river, it became the When to pay tab on 23.10.2026 ([tabs by purpose](/2026-10-23-tabs-by-purpose.md)).
- What the fund bills each quarter is the calendar's one input of its own, and never reaches the river's figures.
