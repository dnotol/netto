---
type: adr
title: The user declares what Netto cannot see, and the engine tests what follows from their figures
description: Eligibility and statuses are inputs on the Scenario; conditions on the scenario's own figures, such as art. 215's minimum pay, are the engine's to test.
status: stable
decided: 2026-10-19
tags: [engine, law-reading]
sources:
  - resource: https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635
  - resource: https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX
  - resource: git:4c35514 Compute corporate tax with the conditions of the reduced rate
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

Many tax outcomes hang on facts Netto has no way to check: whether the company is small, which financial year it is in, whether a car is a fake hybrid. Others follow from figures Netto already holds. « Goal & scope » ([Notion](https://app.notion.com/p/3fdb6b39bd1281bc81bfce270c576635)) and the River Map's rule of 19 October 2026 (« You declare what Netto cannot see ») drew the line.

# Decision

Eligibility is declared by the user, never decided by the engine. What Core cannot see is a status on the `Scenario` (for example `CompanyStatus(SmallCompany, FirstFourTaxPeriods, FirstThreeFinancialYears)`, the car's engine and fake-hybrid status, the copyright statuses, the household), and the card shows that status's conditions with their texts. A condition on the scenario's own figures is the engine's to test: art. 215's minimum pay and the 20% share of benefits in kind (`CorporateTaxCalculator`).

# Alternatives rejected

None recorded at the time.

# Consequences

- A fact the rules need and the user has not given is refused or asked for, never guessed (a combustion car without its CO2 is refused).
- A card lists only conditions it can source from a filed text (River Map, 20 October 2026).
- The engine later named the conditions one part of the year sets on another ([the year's conditions](/2026-10-25-the-engine-names-the-years-conditions.md)).
