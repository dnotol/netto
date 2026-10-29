---
type: adr
title: The company car and the copyright royalty are modules that hand the year plain figures
description: Car and royalty live in their own Core folders and hand the layers BenefitInKind and MovableIncomePaid; no layer knows either.
status: stable
decided: 2026-10-22
tags: [engine]
sources:
  - resource: https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98
  - resource: https://app.notion.com/p/3fdb6b39bd1281b9831de3edd7da2a42
  - resource: https://app.notion.com/p/401b6b39bd128164b84ec739e9ee4124
  - resource: "git:8223ce8 Hand the year a car's benefit in kind as plain figures"
  - resource: "git:826aac4 Give income years 2026 and 2025 the rules of a copyright royalty, and take it within its limits"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

The company car and the copyright royalty each touch several layers at once. A car is a benefit in kind taxed as pay but never paid in cash, a cost the company deducts at a rate set by its order date, engine and CO2, and an add-back on corporate tax. A royalty is deducted in full, taxed apart within its limits and as pay beyond them. The « Engine architecture » page recorded on 22.10.2026, while the [car](https://app.notion.com/p/3fdb6b39bd1281b9831de3edd7da2a42) and [royalty](https://app.notion.com/p/401b6b39bd128164b84ec739e9ee4124) stories were built, how they would join the engine.

# Decision

Each mechanism is a module of its own in Core (`Netto.Core/CompanyCar/`, `Netto.Core/Copyright/`) with its rule set, calculator and result, and hands the year only plain figures through one generic record: `BenefitInKind` (`TaxedAsPay`, `Contribution`, `Costs`, `NotDeductible`) for the car, `MovableIncomePaid` (`Amount`, `TaxedApart`, `TaxedAsPay`, `Withheld`, `PaidApart`) for the royalty. Nothing car- or royalty-specific enters DirectorPay, PersonalTax, SocialContributions or CorporateTax; the layers only ever see amounts.

# Alternatives rejected

None recorded at the time.

# Consequences

- A laptop or a phone the company owns would hand the year the same four `BenefitInKind` figures.
- Since 24.10.2026 each module is composed in its lever, `CarLever` and `CopyrightLever` ([levers behind one contract](/2026-10-24-levers-behind-one-contract.md)), which hands the pay `TaxedWithPay.BenefitInKind` and `TaxedWithPay.Movable`.
- Each module's card lives in `Web.Client/River/CompanyCar/` and `River/Copyright/`; its rules, amounts and worked examples are on its claude.ai map ([car](https://claude.ai/artifact/AhoWfP6DmKmqWatT2xAc1j), [royalty](https://claude.ai/artifact/H78okHrfkQ1Fjs3FtCYGCY)).
- That no layer holds car- or royalty-specific code is checked at review, not by a test.
