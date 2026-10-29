---
type: adr
title: Each layer has one rule set per income year, every amount with its source
description: A new income year is a new rule-set instance found through For(year), never an edit; every amount is a RuleAmount carrying its Source.
status: stable
decided: 2026-10-19
tags: [rules, engine]
sources:
  - resource: https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98
  - resource: git:c72f94e Compute the final INASTI contributions for income year 2026
  - resource: git:dbe0ce1 Record the rule-set, rounding and Tax-Calc conventions in CLAUDE.md
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

Belgian amounts change every income year, and some rules change within one. « Engine architecture » ([Notion](https://app.notion.com/p/3feb6b39bd12819eba87eff6b9ff6c98)), decided on 19 October 2026 after the gross-to-net slice, asked where that variation should live.

# Decision

Year-to-year change is data. Each layer has one rules record, with one instance per income year (`SocialContributionRules.IncomeYear2026`, `CorporateTaxRules`, `PersonalTaxRules`, `CarRules` and the others), found through `For(year)` or `For(year, region)`. A year without rules gives `null`, never an exception. Every amount is a `RuleAmount(Key, Value, Source)`; rates are fractions (`0.205m`). A rule that changes between years is rule data, never a test on the year in code. Rounding happens where the administration rounds, through `Money` (`RoundToCent`, `DownToCent`, `UpToCent`).

# Alternatives rejected

- A strategy class per year or per region everywhere: rejected in « Engine architecture » (« Why not Strategy everywhere »). The legal dependencies are fixed, results must stay comparable with Tax-Calc line by line, and region × year × event would multiply classes; an interface goes only where the algorithm itself differs.

# Consequences

- A new income year is a data drop: new instances and their documents.
- `SourceDocumentTests` reflects over every rule set ([official sources, filed and tested](/2026-10-19-official-sources-filed-and-tested.md)).
- The region joined as a parameter of every lookup later ([region](/2026-10-24-region-is-a-parameter-of-every-rule-lookup.md)), and a change within a year as rule data ([a decree within the year](/2026-10-25-a-decree-within-the-year-is-rule-data.md)).
