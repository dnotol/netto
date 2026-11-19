---
type: adr
title: A loss the earlier profits do not cover keeps no reserve, not even one of 0
description: The reserve is kept while what the company holds at year end, its profit after tax plus the earlier profits, unclamped, reaches it; the split, the moves and the year's conditions read that one rule, and from a plan already short of it a move that leaves the company no less is still taken.
status: stable
accepted_by: human:Johnny Amadeus
decided: 2026-11-19
tags: [engine]
sources:
  - resource: https://app.notion.com/p/41db6b39bd1281048465e9fe03fb3f59
generated: {by: claude-opus-5-5/architecture-views, at: 2026-11-19T18:00:00+01:00}
---

# Context

[The user sets what the company keeps](/2026-10-19-one-reserve-for-the-company.md) made « the reserve is kept » one rule, `YearResult.ReserveKept`, read by the year, the split's search and the moves. It compared the most the year can pay out, clamped at zero, with the reserve. At a reserve of 0 a pay that left the company at a loss passed (`0 >= 0`). Past the pay where the dividend reaches zero, each euro of pay still reached the pocket while nothing weighed what the company lost, so the split's best pay climbed to the ceiling and the moves raised the pay into money the company does not have. The ceiling allows it: it caps the payslip gross at the profit before pay, while the company bears the contributions it pays for the director and the tax on what it cannot deduct of the levers. Found by the operator on 2026-11-19 ([work item](https://app.notion.com/p/41db6b39bd1281048465e9fe03fb3f59)).

Netto weighs one year. A loss does carry forward (CIR 92 art. 206) and saves 20–25% of corporate tax later, but next year's profit must refill it before any dividend, so in cash it costs far more than it saves. Weighing that is a question for several years, which « Goal & scope » leaves out.

# Decision

The reserve is kept while what the company holds at year end reaches it: the year's profit after tax and the profits kept from earlier years, unclamped (`YearResult.Held`). `ReserveKept(held, reserve)` stays the one rule: the year (`KeepsReserve`), the split's level at each pay, the moves and the year's condition on the reserve (`PayOutCoversReserve`) read it. A loss the earlier profits cover still keeps the reserve, since they absorb it first.

A plan can already miss the reserve, at a loss whatever the pay or at a pay the user set. From such a plan the moves still take a move that leaves the company no less than the plan does, so the director's payments are not lost with it, and never one that deepens the shortfall. When no pay keeps the reserve the split has no best pay, and the pay curve says so: at a reserve of 0, that the company ends the year at a loss.

# Alternatives rejected

- Lowering the pay ceiling to the pay where the company breaks even: it removes the stretch of curve drawn at a loss but not a loss from the tax on what the company cannot deduct, and the moves would still need the rule. It may still come for the curve's axis.
- Pricing the loss's carry-forward: it needs next year, which Netto does not model.
- Refusing every move from a plan short of its reserve: it would hide the director's advance payments in a loss year for no reason.

# Consequences

- The split's best pay never leaves the company at a loss the earlier profits do not cover; on the made-up company paying its director's contributions, the curve's last pays are dashed.
- A company at a loss whatever the pay has no best pay; when the profit before pay is zero or less the curve is not drawn at all, as before.
- At a reserve of 0, the pay curve's key and its sentence without a best pay say « at a loss » (`PayCurveText`), and its best pay is named the best « without a loss » (`PayCurveFigures`).
- The bar's own wording at a loss is a separate work item.
