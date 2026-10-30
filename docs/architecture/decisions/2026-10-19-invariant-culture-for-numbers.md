---
type: adr
title: Numbers are formatted on the invariant culture, and the app's culture is never set
description: Every amount goes through Formats (invariant culture with the euro sign); MudBlazor inputs take Formats.Culture.
status: deprecated
superseded_by: /2026-10-30-french-by-default-english-beside-it.md
decided: 2026-10-19
tags: [site]
sources:
  - resource: git:4f90391 Format amounts on the invariant culture with the euro and minus signs
  - resource: git:f7ab9e9 Record the number-format, numeric-field and resource-key conventions
generated: {by: claude-opus-5-5/architecture-views-0.7.0, at: 2026-10-29T14:00:00+01:00}
---
# Context

An English page shows Belgian amounts in a browser whose language can be anything. The first cards needed amounts that read the same in every browser and in the tests, on 19 October 2026.

# Decision

Amounts read as `€50,000.00` whatever the browser's language. Every number goes through `Web.Client/Formats.cs`, whose `Formats.Culture` is the invariant culture with the euro sign, not a named culture. MudBlazor inputs receive `Culture="Formats.Culture"` (a `RiverNumericField` sets it itself). The app's culture is never set. A rule amount is formatted by its key's suffix (`_rate` as a percent).

# Alternatives rejected

None recorded at the time. The comment on `Formats` explains why a named culture such as en-IE is not used: since the client is published without ICU data, a culture created by name throws.

# Consequences

- On 24 October 2026 the client was published with `InvariantGlobalization` (`git:bdae5cf Publish the client without ICU data`), so no ICU data is downloaded; `Architecture/ClientGlobalizationTests` guards it.
- The browser and the tests format alike, so a test can assert on displayed text.
