# Official documents

The official texts Netto's rules are taken from, kept as they were when an amount was read from them. Every amount in `Netto.Core` cites its document in a `Source`; this folder holds the document itself, so a figure can be checked against what the text said at the time even after the issuer has replaced it.

## What is current

A document is current **for an income year**, never in general: the 2025 return explanations are outdated for income year 2026 and still current for the 2025 rule set the Tax-Calc tests use. So documents are filed by layer, and `INDEX.md` says, for each one, which income years it applies to and its status:

| Status | Meaning |
|---|---|
| `current` | The latest edition for the income years it applies to. |
| `superseded by <file>` | A newer edition replaced it. Kept, never deleted: an older rule set may still cite it. |
| `not yet applicable` | Published for an income year Netto has no rule set for yet. |

*Applies to* holds the income years whose rule sets may cite the document, and nothing else, so the tests can read it:

- one year, `2026`, for a text written for that year (a coordinated article « revenus 2026 », an indexation notice);
- a range, `2025 to 2026`, for a text that holds for several years and no later one;
- an open range, `2017 onwards`, for a text that holds until it is changed (a decree, a circular's method). When a change arrives, the old row's range is closed at the last year it holds for and the new text gets its own row.

An income year that borrows a document until its own edition is published (the latest municipal rates, the latest return explanations) is in the range, and *Notes* says it is borrowed. When that year's own edition is filed, the year comes off the borrowed document's range.

*Notes* says the rest: the tax years, what the document gives, why a year is borrowed. Income year Y is taxed in tax year (assessment year) Y + 1, and many documents are titled by the tax year.

Read *Applies to* and *Status* before taking an amount: a rule set for income year Y cites only documents that apply to Y and are `current`. Each `Source` in `Netto.Core` names its file in `Document`, the path the *File* column gives, and `SourceDocumentTests` fails when an amount of a rule set, or a source its steps cite, points to a document that is missing here or from this folder, listed at another URL, not `current`, or not applying to that income year.

## Filing a document

1. Put it in the folder of the layer it serves (`corporate-tax/`, `personal-tax/`, `social-contributions/`, `municipal-surcharge/`), named `<issuer>_<short-title>_<version date>.<ext>`, in lower case with hyphens. The version date is the one the issuer gives (publication, version or last-modified date), never the download date.
2. A web page whose content changes in place (a rates page, a contributions page) is saved as a dated snapshot, PDF or HTML, the day an amount is read from it.
3. Add its row to `INDEX.md`: file, issuer, title, URL, version date, date retrieved, the income years it applies to, status, notes.
4. A new edition is a new file, never an overwrite. Mark the old row `superseded by <new file>`, and point each `Source` that cited it at the new file.

Downloading a document needs the operator's go.

## Municipal surcharge rates

The SPW file holds every Walloon municipality's rate per tax year. `Netto.Core` carries them as checked-in data (`PersonalTax/WalloonMunicipalities.TaxYear<YYYY>.cs`), generated from the file, so the browser never downloads anything. Each year, once the SPW adds a column:

1. File the new edition here and mark the old one superseded.
2. Generate the new year's data with `tools/municipal-rates/generate_rates.py` (its header says how to run it). It checks every rate has at most one decimal and lies between 0% and 10%, and lists the rows it skips (a merged municipality has no rate of its own).
3. Point the matching `MunicipalRates` income year at the new data, with the file's version in its `Source` and the file in its `Document`.
