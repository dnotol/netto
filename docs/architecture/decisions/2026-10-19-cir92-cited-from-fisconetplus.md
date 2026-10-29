---
type: adr
title: A CIR 92 article is cited from Fisconetplus's coordinated text for its income year
description: Never an SPF summary page, which can lag a reform, nor Justel's CIR 92, frozen since 2002; Source.Fisconet builds the citation.
status: stable
decided: 2026-10-19
tags: [rules]
sources:
  - resource: git:feb9cfe Record in CLAUDE.md which CIR 92 text an amount cites
  - resource: git:fbf3d2b Compute the surcharge on the director's income tax and the bonus on advance payments
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

Filing official sources ([official sources, filed and tested](/2026-10-19-official-sources-filed-and-tested.md)) left open which text of the income tax code to cite. In October 2026 the SPF's rates page still showed the pre-reform 2026 tax-free amount. The rule was written into CLAUDE.md on 19 October 2026 (`git:feb9cfe`); the code helper followed the next day.

# Decision

An amount from CIR 92 cites the administration's coordinated article for that income year on Fisconetplus (« CIR 92, art. 130 (revenus 2026) »), which carries the year's indexed amounts. `Source.Fisconet(folder, article, incomeYear, lastModified, recordId, reference)` builds the title, the Fisconetplus URL and the file name under `official-docs/`, and cites it in lists as « CIR 92, art. N ».

# Alternatives rejected

- An SPF summary page: it can lag a reform (the rates page still showed the pre-reform tax-free amount in October 2026).
- Justel's CIR 92: frozen since 2002.

# Consequences

- When Fisconetplus is down, articles wait: on 28 October 2026 the household's quotient cited the law of 15.07.2026 and the indexation notices meanwhile, a Backlog item.
- Each article is a file per income year, so a new year files the articles again.
