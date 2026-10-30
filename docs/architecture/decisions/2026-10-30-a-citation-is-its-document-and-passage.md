---
type: adr
title: A citation is its document and passage, and the client words it
description: Source keeps a passage key, its document and its address; its title, label and reference are worded in Steps/SourceText.resx and its .fr.resx; a CIR 92 article's title is built from its structured article.
status: draft
decision: proposed
decided: 2026-10-30
tags: [rules, site]
sources:
  - resource: https://app.notion.com/p/409b6b39bd12818f9c82e79a28e87297
generated: {by: claude-opus-5-5/architecture-views, at: 2026-10-30T20:00:00+01:00}
---

# Context

The French story ([story](https://app.notion.com/p/409b6b39bd12818f9c82e79a28e87297), [French by default](/2026-10-30-french-by-default-english-beside-it.md)) needs every text the viewer reads in both languages. The citations under each figure are English prose held in Core: each `Source` carries a `Title` (official names with English glue such as « coordinated text on Fisconetplus, last modified … »), a `Reference` (an English paraphrase of the passage) and a `Label`. That is 136 distinct sources over 97 documents, cited 931 times by the rule sets. Core is meant to return data, never display text ([Core is a plain library](/2026-10-18-core-is-a-plain-library.md)).

# Decision

- A `Source` is its `Passage` (a stable key, unique within its document), its `Url` and its `Document` (the file in `official-docs/`, as [official sources filed and tested](/2026-10-19-official-sources-filed-and-tested.md) has it), plus, for a CIR 92 or AR/CIR 92 article, a structured `Article` (code, number, income year, last modified). `Title`, `Reference` and `Label` leave Core.
- The words live in `Steps/SourceText.resx` (English, moved verbatim) and `SourceText.fr.resx`:
  - `Title_{Document}` for a document written by hand;
  - an optional `Label_{Document}` and an optional label per passage;
  - a required `Reference_{Document}#{passage}`;
  - format strings that build a Fisconetplus article's title and label in each language.
- Every CIR 92 or AR/CIR 92 source is built by `Source.Fisconet`. A citation's dates read `dd.MM.yyyy` in both languages: they identify the document.
- French shows each document's official French name, taken from the filed document. English keeps today's wording.
- A test fails when a source of any rule set lacks its words in either language (`SourceTextTests`).

# Alternatives rejected

- A resx keyed by the English reference text: editing the English would silently lose its French.
- English kept in Core with only French in a resx: the two languages in two places, and no neutral file to pair against.
- Both languages in Core: display text in the engine, outside the parity test, and a third language would mean editing Core.

# Consequences

- Reviewing a rule's citation means opening `SourceText.resx`. A new edition of a document renames its keys, so its passages are re-read.
- The 18 CIR 92 documents shown today by their whole title read « CIR 92, art. N » in lists, in English too.
- [CIR 92 cited from Fisconetplus](/2026-10-19-cir92-cited-from-fisconetplus.md) keeps its rule; `Source.Fisconet` now builds the document, the address and the article, and the client words the title.
