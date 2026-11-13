---
type: adr
title: IT and phone is a module behind BenefitInKind, weighed in cash like the car, counted for one period of calendar days
description: A PC, tablet, phone, subscription and internet line the company provides are one optional lever with its own card, handing the pay their flat values through TaxedWithPay.BenefitInKind; one period for the set, its days counted by the type the car uses; beside the flat allowances for the same costs the plan is refused.
status: draft
decided: 2026-11-13
tags: [engine, site]
sources:
  - resource: https://app.notion.com/p/3ffb6b39bd12814093f5e7da17f6aeaa
  - resource: https://app.notion.com/p/417b6b39bd1281828cf0c79fa4f33acd
generated: {by: claude-fable-5-2/architecture-views, at: 2026-11-13T12:00:00+01:00}
---

# Context

A check of the benefits in kind against the official texts on 13.11.2026 found two gaps, each a work item the operator picked: [a car held part of the year](https://app.notion.com/p/417b6b39bd1281828cf0c79fa4f33acd) was taxed for the whole of it, and [the PC, phone and internet the company owns](https://app.notion.com/p/3ffb6b39bd12814093f5e7da17f6aeaa) were not modelled at all. [The car and the royalty behind plain contracts](/2026-10-22-car-and-royalty-behind-plain-contracts.md) already foresaw that « a laptop or a phone the company owns would hand the year the same four `BenefitInKind` figures », and [gains are in cash](/2026-10-23-gains-are-in-cash.md) that such a thing is weighed on its own card against buying it privately.

The texts: art. 18 §3, 10 and §4 AR/CIR 92 (72 a PC, 36 a tablet or a mobile phone, 60 the internet connection, 48 a phone subscription, a year, less the beneficiary's contribution); circular 2017/C/82 (per appliance, the internet connection once whatever their number, only what the company itself owns or holds the contract of); circular 2018/C/63 (§25–26: for part of the taxable period, by calendar days of provision, the first counted, the last not, the rule the car's FAQ gives; §23: a contribution comes off its own benefit only); circular 2021/C/20 (§48, §52: no internet allowance beside an internet line the company pays, no computer allowance beside a PC or a tablet it provides).

# Decision

- **One optional lever, « IT and phone » (« Informatique et téléphonie »)**, in `Netto.Core/Equipment/` with its rule set, calculator and result, composed in `EquipmentLever` between the rent and the car in `LeverOrder.For(year)`. It hands the pay the flat values through `TaxedWithPay.BenefitInKind`, summed with the car's, so the taxable pay, INASTI's base and both of art. 215's tests take it with no change to a layer. Its costs come off the profit as the car's do, deductible in full with no add-back (art. 198 §1, 9° and 9°bis name vehicles only), and end in the Expenses bucket.
- **Its facts are counts and one yes or no**: PCs, tablets, mobile phones and phone subscriptions as numbers, the internet connection as provided or not; tablets apart from phones because only a tablet excludes the computer allowance. One contribution for the set, refused above the set's flat total; that no piece's share exceeds its own value is a condition the user declares.
- **One period for the set**, two optional dates, counted like the car's: the span clipped to the income year, its calendar days over the year's, the first day counted and the last not. The arithmetic is one Core type outside `CompanyCar/`, `DaysHeld`, that the car's calculator and the equipment's both use: the car's benefit and its yearly minimum are counted for the days held, as the FAQ on art. 36 §2 says (question 4). Each piece's value for the days is rounded to the cent before it is multiplied by its count, Netto's reading where the administration gives no worked example with cents. Pieces arriving on different dates are the user's to approximate.
- **Beside the flat allowance for the same cost the plan is refused**, on the equipment's input, as the rent is beside the office allowance; the engine also names the rule as a `YearCondition` so the map draws it between the two modules.
- **It is weighed in cash on its card and is no idea**: the card leads with the plan with it less the plan without it, then against what the same things would cost privately, none until given. Like the car it stays in « Add to plan »: without the private cost it only takes cash from the pocket.
- **What Netto cannot see is declared**: the device is the company's and the contract in its name, the subscription is not also reimbursed on invoices. VAT on the private share is not counted: VAT is out of Netto's scope.
- **Director pay names each benefit by its source**, the car's and the equipment's, never one under the other's name.

# Alternatives rejected

- **The flat values typed on Director pay, with no card**: not weighed in cash, the costs hidden in the running costs, a lever's facts on a card that is always in the plan, and nowhere for the allowance exclusion to sit.
- **Dates per piece**: exact, but ten date fields and a heavier saved format for a case the user can approximate.
- **Taken as provided all year, the limit a condition**: the same known gap the car's story closes.
- **A contribution per piece**: exact under §23, five fields for a rare case; one stored-format change if it is ever needed.
- **An idea in Ideas**: its gain cannot be measured without the private cost, which Netto never guesses.

# Consequences

- The saved plan gains the lever's fields in version 10, the version the car's dates raise in the same story.
- `MapConditions` draws art. 215's 20% condition from the car while it carries a benefit, else from the equipment.
- A third benefit in kind follows the same path: a folder in Core, a lever, its card, its row on the map.
- That no layer holds equipment-specific code is checked at review, as for the car.
