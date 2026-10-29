---
okf_version: "0.2"
---

# Decisions

Every decision taken on Netto, one file each, dated the day it was decided. A decision under debate lives in Notion (« Netto Development ») and lands here once taken; an ADR is never rewritten, a changed decision is a new one and the old one is marked deprecated. The pages that explain how the code is shaped are in the [architecture index](../index.html).

## The engine

- [The tax engine is a plain library the browser can run](/2026-10-18-core-is-a-plain-library.md): Netto.Core has no ASP.NET Core dependency and nothing the browser cannot run; the dependency graph points one way.
- [Dividends are modelled in steady state, each year paying out its own profit](/2026-10-19-dividends-in-steady-state.md): The year's dividend comes out of its own profit after tax, at the rate of its payment the following year; the whole distribution takes the declared rate.
- [The user sets what the company keeps, and the dividend is what is left](/2026-10-19-one-reserve-for-the-company.md): One reserve for the whole company at year end; the dividend is profit after tax plus earlier profits less that reserve, never below zero, never refused.
- [One year simulation composes the layers in the law's order, and the cards only read its steps](/2026-10-19-one-year-simulation-in-the-laws-order.md): YearSimulation.Run composes static layer calculators into one ledger of tagged steps; no tax rule lives outside Netto.Core.
- [The user declares what Netto cannot see, and the engine tests what follows from their figures](/2026-10-19-the-user-declares-what-netto-cannot-see.md): Eligibility and statuses are inputs on the Scenario; conditions on the scenario's own figures, such as art. 215's minimum pay, are the engine's to test.
- [What the company pays besides pay comes off its profit before the split, and is never pay](/2026-10-20-levers-come-off-the-profit-before-the-split.md): Vouchers, allowances, rent, a car and a royalty come off what the company earns in the law's order; the split shares what they leave; what is undeductible is added back.
- [The pay slip withholds by the administration's key formula unless the user types what was withheld](/2026-10-20-withholding-by-the-key-formula.md): Twelve equal months, each withheld by the key formula with the rules in force when paid; the year's withholding is their sum unless given.
- [The company car and the copyright royalty are modules that hand the year plain figures](/2026-10-22-car-and-royalty-behind-plain-contracts.md): Car and royalty live in their own Core folders and hand the layers BenefitInKind and MovableIncomePaid; no layer knows either.
- [The payment calendar is a record the engine builds beside the ledger, and steps carry no date](/2026-10-22-payment-calendar-beside-the-ledger.md): When each payment falls is Calendar/PaymentCalendar, built from the year and a date; a step stays the year's total, undated.
- [The year folds over its levers in the law's order, each behind one contract](/2026-10-24-levers-behind-one-contract.md): Lever and LeverYear, ordered by LeverOrder.For(year); what each hands the pay is one record, TaxedWithPay.
- [A refusal is a typed value carrying its key and the input it refuses](/2026-10-24-refusal-is-a-typed-value.md): Refusal(Key, RefusedInput), declared only in Core; the client routes it to its card through an exhaustive RiverErrors.Owner.
- [INASTI's yearly company contribution is the year's first step, a cost of the company](/2026-10-25-inasti-company-contribution-is-the-first-step.md): CompanyContributionRules by balance-sheet band; it comes off what the company earns before the levers, and is no lever.
- [The engine names the conditions one part of the year sets on another](/2026-10-25-the-engine-names-the-years-conditions.md): YearConditions.Of(year) returns sealed YearCondition records, each saying whether it holds; the map and cards only word them.
- [A director with a partner is taxed jointly, and the river counts the household's tax less the partner filing alone](/2026-10-28-household-taxed-jointly.md): The household is no lever but facts on Scenario.Household; PersonalTaxCalculator taxes two columns; the quotient cap counts the partner's income.

## Rules and their sources

- [A CIR 92 article is cited from Fisconetplus's coordinated text for its income year](/2026-10-19-cir92-cited-from-fisconetplus.md): Never an SPF summary page, which can lag a reform, nor Justel's CIR 92, frozen since 2002; Source.Fisconet builds the citation.
- [Rules come only from official sources, filed in the repository and checked by a test](/2026-10-19-official-sources-filed-and-tested.md): Every Source names its file in official-docs/; SourceDocumentTests fails on a document INDEX.md does not list as current for that year.
- [Each layer has one rule set per income year, every amount with its source](/2026-10-19-one-rule-set-per-layer-and-income-year.md): A new income year is a new rule-set instance found through For(year), never an edit; every amount is a RuleAmount carrying its Source.
- [The region is a parameter of every rule lookup, and a year or region without rules gives null](/2026-10-24-region-is-a-parameter-of-every-rule-lookup.md): Scenario.Region, Wallonia unless given; only Wallonia has rules, others are refused; no lookup throws.
- [What a decree changes within the year is rule data, and each month withholds by the rules in force on its first day](/2026-10-25-a-decree-within-the-year-is-rule-data.md): WithholdingRules.Later holds the rules from a date; InMonth picks a month's; November 2026 is the first case.

## Readings of the law

- [No part of a copyright royalty counts toward the minimum director pay of the reduced corporate rate](/2026-10-22-royalty-never-counts-toward-minimum-pay.md): Law reading - art. 215 tests the taxable pay without a royalty's part taxed as pay (TaxablePayWithoutRequalified).
- [Where no text settles a rule, Netto takes the reading that never overstates the gain](/2026-10-22-the-reading-that-never-overstates-the-gain.md): An unsettled point of law is read the cautious way, and the card says it is Netto's reading.
- [The social insurance fund's management fee is counted at the rate the user gives and deducted with the contributions](/2026-10-25-fund-fee-deducted-with-contributions.md): Law reading confirmed by the operator - the fee is one of the contributions the director pays their fund (art. 52, 7°).

## Saved plans

- [The user's plans stay in their browser or a file they export, never on a server](/2026-10-22-plans-stay-in-the-browser.md): No accounts and nothing sent to a server; saved plans live in localStorage and move between browsers as an exported JSON file.
- [Saved plans use a versioned format of their own, read as saved or not at all](/2026-10-22-saved-plans-in-a-strict-versioned-format.md): PlanStorage maps RiverInputs to its own records with a version; a plan is read strictly, never filled with defaults, and an unreadable document is copied aside.
- [An older saved plan is brought to the current format one version at a time, by frozen literal patches](/2026-10-24-plans-migrated-a-version-at-a-time.md): PlanStorage.Patches add each version's new fields as frozen JSON; StoredPlanDocuments holds a document of every version.

## Searches

- [The split finds the best pay by running the year at every pay](/2026-10-19-the-split-searches-the-best-pay.md): SplitSearch draws net in pocket on a grid of pays, adds the reduced-rate jump, and refines the best pay on each side to the euro; no formula of its own.
- [Moves are found greedily, each gain the difference of two runs of the year](/2026-10-22-moves-found-greedily-from-a-baseline.md): MoveSearch takes the finders' best move until none adds a euro; no move is refused or leaves less than the reserve; Apply is the one place a move is applied.
- [The moves are searched once per baseline and date, after the page has painted, never on an edit](/2026-10-23-moves-searched-once-per-baseline-after-paint.md): MovesBaseline is Today, else the plan as opened or saved; MovesSource searches once per baseline and date, after plan-paint.js.

## The site

- [Components come from MudBlazor and every text from a resource file](/2026-10-18-mudblazor-and-resx-text.md): MudBlazor for components; English UI copy in a .resx beside each component, read through IStringLocalizer, never inline.
- [Every page runs in WebAssembly and nothing is prerendered](/2026-10-18-webassembly-only-nothing-prerendered.md): The server only serves files; every component runs in the browser, so no user figure ever reaches a server.
- [Numbers are formatted on the invariant culture, and the app's culture is never set](/2026-10-19-invariant-culture-for-numbers.md): Every amount goes through Formats (invariant culture with the euro sign); MudBlazor inputs take Formats.Culture.
- [Optional levers join the plan from a menu, and what is out neither shows nor counts](/2026-10-21-optional-levers-neither-shown-nor-counted.md): PlanComposition holds which levers are in; an absent lever is its None in the Scenario, its values kept for when it comes back.
- [The plan page splits into tabs by purpose, each with its own address](/2026-10-23-tabs-by-purpose.md): Plan, Optimisation, When to pay and Compare plans are PlanTab values under /plan/{Tab}, one page keeping the plan across them.
- [The map's lines are read from the cards' own views, never computed by the page](/2026-10-25-map-lines-read-from-the-cards.md): MapLinks.Of reads each card's figure and drain lines; MapBundle.Sum adds them; MapCardsTests holds every amount to a card's.
- [The Plan tab is the map, with the chosen module's card beside it](/2026-10-27-the-plan-is-the-map.md): /plan draws the river from above and pins the chosen module's editable card beside it; the column of cards goes.
- [The company's expenses are the bar's fifth bucket, Expenses, last](/2026-10-28-expenses-are-the-fifth-bucket.md): INASTI's company contribution and a car's costs land in Expenses; the bar starts before them and its reach leaves them out.

## Build and launch

- [The Aspire AppHost is only a local launcher](/2026-10-18-aspire-apphost-as-launcher.md): Netto.AppHost starts Netto.Web under the Aspire dashboard and nothing else; ServiceDefaults waits for an API.
- [The build fails on any warning, with the .NET analysers pinned](/2026-10-24-warnings-are-errors.md): TreatWarningsAsErrors with analysers at 10.0-recommended, held by BuildPolicyTests.

## Product

- [Netto's shape is a river of modules, each taking its part and passing the rest on](/2026-10-19-a-river-of-modules.md): One page at /plan follows the money through module cards to five buckets, the bar of buckets always in view as the summary.
- [Defaults exist for rates and statuses, never for money, and every input says where its value comes from](/2026-10-19-defaults-for-rates-never-for-money.md): InputOrigin marks each input as example, default, derived or yours; no net figure is built on an amount the user did not give without saying so.
- [Time is a lens over the river, not a module in it](/2026-10-19-time-is-a-lens.md): The year's steps carry no date; when each bucket is paid is a separate view over the whole river.
- [Every gain is measured in cash, and a company car is weighed in cash against the same car bought privately](/2026-10-23-gains-are-in-cash.md): The muddy pocket goes; the gain over Today is cash alone, and the car card states what the car takes from the pocket.
- [Netto is a reference tool for its operator and a few trusted people, with no commercial goal](/2026-10-24-a-reference-tool-not-a-product.md): The pivot - no freemium, no deadline, no public repository required; scope lives on Notion's « Goal & scope ».

## Documentation

- [Each complex subsystem has a map of its own, an Artifact updated in each slice](/2026-10-23-subsystem-maps-as-artifacts.md) *(deprecated)*: Superseded - mechanisms beyond one card got claude.ai maps updated slice by slice; replaced by repo pages and ADRs.
- [The architecture pages and the decisions taken live in the repository; the claude.ai maps stay as a frozen rule reference](/2026-10-29-architecture-pages-and-adrs-in-the-repo.md): docs/architecture holds the pages and ADRs; Notion keeps decisions under debate; the maps are kept and linked but no longer updated.
