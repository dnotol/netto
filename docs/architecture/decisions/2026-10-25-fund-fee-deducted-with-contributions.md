---
type: adr
title: The social insurance fund's management fee is counted at the rate the user gives and deducted with the contributions
description: Law reading confirmed by the operator - the fee is one of the contributions the director pays their fund (art. 52, 7°).
status: stable
decided: 2026-10-25
tags: [law-reading, rules]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: "git:4930ed7 Add the social insurance fund's management fee: its texts, the rate the user gives, and the fee on each billed quarter"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

A social insurance fund charges a management fee on the contributions, at a rate the minister fixes per fund (AR n° 38, art. 20 §4, shown on the fund's notice). No text that makes contributions deductible names the fee. It was built in the Selected batch of 24.10.2026 (« Social contributions include my fund's management fee ») and decided unattended, to confirm (« Goal & scope », 25.10.2026).

# Decision

The user types their fund's rate on Director pay; none by default, never a guessed rate, refused outside 0 to 10% (`fund_fee_rate_out_of_range`). The fee is that rate on each of the four billed quarters, rounded (`SocialContributionCalculator.Fee`), a `fund_fee` step after the contributions. Netto reads it as one of the contributions the law makes the director pay their fund (CIR 92 art. 52, 7°, with Com.IR 52/124), so it is deducted with them as the director's own; when the company pays the contributions, it pays the fee with them, as pay then deducted. The operator confirmed the reading on 25.10.2026.

# Alternatives rejected

None recorded at the time.

# Consequences

- The card's details say it is Netto's reading ([the reading that never overstates the gain](/2026-10-22-the-reading-that-never-overstates-the-gain.md)); an official text naming the fee would settle it.
- The calendar's quarters, When to pay, the pay slip, the split and Compare plans carry the fee.
- Saved-plan format 7 holds the rate (`fundFeeRate`).
