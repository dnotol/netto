---
type: adr
title: A director with a partner is taxed jointly, and the river counts the household's tax less the partner filing alone
description: The household is no lever but facts on Scenario.Household; PersonalTaxCalculator taxes two columns; the quotient cap counts the partner's income.
status: stable
decided: 2026-10-28
tags: [engine, law-reading]
sources:
  - resource: https://app.notion.com/p/403b6b39bd1281d9a0d5e1ae66489bb2
  - resource: https://app.notion.com/p/407b6b39bd128170b9f5e4d63783758a
  - resource: https://claude.ai/artifact/Xq92UJySyaDCQhjKMyE9oR
  - resource: "git:e88abce Let a scenario declare the director's household: alone, or a spouse or legal cohabitant taxed jointly outside the year the couple starts or ends"
  - resource: "git:b3e0190 Measure the river's share of a joint year from the partner filing alone, so an earning partner no longer charges the pay with a quotient the partner never gets"
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---

# Context

A married or legally cohabiting director is taxed jointly: one assessment, each partner's income on its own scale, the marital quotient moving part of the higher professional income to the other. « I can declare my partner and be taxed jointly » ([story](https://app.notion.com/p/403b6b39bd1281d9a0d5e1ae66489bb2)) was built attended in three slices; its figures were checked against Tax-Calc « exercice 2026 » on two cases, every line to the cent.

# Decision

The household is no lever: it takes nothing off the profit and hands nothing to the pay. Its facts are `Scenario.Household` (`Core/PersonalTax/Household.cs`), and `PersonalTaxCalculator` taxes a couple in two columns (`JointAssessment`). The river counts the household's tax less what the partner would owe filing alone on their own professional income (`PersonalTaxCalculator.PartnerAlone`), so the partner's own tax and withholding never enter the bar. The quotient's cap counts the receiving partner's own income: the administration's reading in the bill's explanations (DOC 56 1243/001), which Tax-Calc applies. The municipal surcharge is taken once on the household.

# Alternatives rejected

- The household with the director's income at zero as the baseline, used first on 28.10.2026: it charged the pay for a quotient the partner would receive were the director earning nothing (about €4,928 with a partner earning €30,000). Changed unattended the same day, one commit to revert.
- The cap read on the share alone, a possible reading of the law's wording.
- The municipal surcharge rounded per column: Tax-Calc takes it once on the household.

# Consequences

- The household is its own optional map module right of Director pay, with a dotted tax-base line for the quotient (operator); saved plans are at version 8.
- Dependants joined on 29.10.2026 as more facts of the same household (« I can declare my children and dependants », [story](https://app.notion.com/p/407b6b39bd128170b9f5e4d63783758a)); saved plans at version 9.
- The amounts, worked example and open points are on the [household map](https://claude.ai/artifact/Xq92UJySyaDCQhjKMyE9oR).
