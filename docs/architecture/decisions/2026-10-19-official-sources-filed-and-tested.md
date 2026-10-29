---
type: adr
title: Rules come only from official sources, filed in the repository and checked by a test
description: Every Source names its file in official-docs/; SourceDocumentTests fails on a document INDEX.md does not list as current for that year.
status: stable
decided: 2026-10-19
tags: [rules]
sources:
  - resource: https://app.notion.com/p/3feb6b39bd12819bb78fd72ab4977d1a
  - resource: git:75c8fd7 File official documents by the income years they apply to
  - resource: git:0bdd36d Fail a test when a source cites a document not current for its year
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

Netto states tax figures a director acts on. An amount must be checkable against the text it came from, even after the issuer replaces that text. The rule was set on 19 October 2026 with the first rule sets ([Work Item](https://app.notion.com/p/3feb6b39bd12819bb78fd72ab4977d1a)).

# Decision

Rules come only from official, current sources. Each document is filed in `official-docs/<layer>/` as it was read, named `<issuer>_<short-title>_<version date>`, and listed in `official-docs/INDEX.md` with the income years it applies to and its status (`current`, `superseded by …`, `not yet applicable`). Each `Source` names its file in `Document`. An amount that is not published is marked so, never guessed. A newer edition is filed after the operator's go to download, and the old one is marked superseded, never deleted.

# Alternatives rejected

None recorded at the time.

# Consequences

- `Rules/SourceDocumentTests` fails when a rule set's amount, or a source a step cites, points to a document missing from the folder, listed at another URL, not current, or not applying to that income year. It also fails on a public static `Source` outside a rule set.
- A document several rule sets cite is written once, in `Rules/SharedSources`.
- `official-docs/` is checked for newer editions when a story is analysed and yearly.
- Articles of CIR 92 are cited from Fisconetplus ([CIR 92 from Fisconetplus](/2026-10-19-cir92-cited-from-fisconetplus.md)).
