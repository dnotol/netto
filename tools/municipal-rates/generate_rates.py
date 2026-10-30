"""Turn the SPW « Taux IPP » sheet into Netto.Core's checked-in municipal rates for one tax year.

The rates are the SPW's; the names are Statbel's French names (its REFNIS list, filed beside the SPW
file), because the SPW file drops some accents and misspells two names. The SPW file is a legacy
.xls, which xlrd 2 reads (requirements.txt). From the repo root, in a virtual environment outside
the repo:

    python -m venv <venv>
    <venv>/Scripts/python -m pip install -r tools/municipal-rates/requirements.txt
    <venv>/Scripts/python -I tools/municipal-rates/generate_rates.py \
        official-docs/municipal-surcharge/<SPW file>.xls \
        official-docs/municipal-surcharge/<Statbel file>.csv 2026 \
        src/Netto.Core/PersonalTax/WalloonMunicipalities.TaxYear2026.cs

It stops on a rate with more than one decimal or outside 0% to 10%, and on a name that differs from
Statbel's by more than accents (the SPW typos below excepted), and lists the rows it skips.
"""
import sys
import unicodedata
from decimal import Decimal

import xlrd

sys.stdout.reconfigure(encoding="utf-8")

source, statbel_source, tax_year, output = sys.argv[1], sys.argv[2], int(sys.argv[3]), sys.argv[4]

# Typos of the SPW file (a Y typed as X), by INS code: the name the SPW file gives, which Statbel's
# name replaces. Any other difference that is not an accent stops the run.
SPW_TYPOS = {85007: "ChinX", 91141: "Xvoir"}

# The SPW file lists Bastogne under 82003, its code before the merger with Bertogne; Statbel gives
# the merged municipality 82039. A code Statbel lacks is looked up by the exact SPW name instead.
MERGED = {82003: 82039}


def without_accents(text):
    return "".join(c for c in unicodedata.normalize("NFD", text) if not unicodedata.combining(c))


def read_statbel(path):
    """INS code -> French name of every municipality (a code not ending in 000), from the REFNIS list:
    UTF-8 with a byte order mark, `|` separated, header « Code INS|Entités administratives|... »."""
    with open(path, encoding="utf-8-sig") as file:
        lines = file.read().splitlines()
    assert lines[0].split("|")[:2] == ["Code INS", "Entités administratives"], lines[0]
    names = {}
    for line in lines[1:]:
        cells = line.split("|")
        if len(cells[0]) == 5 and cells[0].isdigit() and not cells[0].endswith("000"):
            names[int(cells[0])] = cells[1].strip()
    return names


statbel = read_statbel(statbel_source)
sheet = xlrd.open_workbook(source).sheet_by_name("Taux IPP")
header = [sheet.cell_value(0, c) for c in range(sheet.ncols)]
assert header[0] == "INS" and header[1] == "Communes", header[:2]
column = header.index(float(tax_year))

rows, skipped, corrected = [], [], []
for r in range(1, sheet.nrows):
    code, name, rate = (sheet.cell(r, c) for c in (0, 1, column))
    if code.ctype != xlrd.XL_CELL_NUMBER:
        break  # the regional average and the statistics below the list
    code_value = int(code.value)
    assert code_value == code.value and 10_000 <= code_value <= 99_999, (r, code.value)
    if rate.ctype != xlrd.XL_CELL_NUMBER:
        skipped.append((code_value, name.value.strip(), str(rate.value).strip()))
        continue
    percent = Decimal(str(round(rate.value, 1)))
    assert abs(float(percent) - rate.value) < 1e-9, (r, rate.value)  # at most one decimal (CIR 92 art. 468)
    assert Decimal(0) <= percent <= Decimal(10), (r, percent)
    spw_name = name.value.strip()
    official = statbel.get(MERGED.get(code_value, code_value))
    assert official is not None, f"{code_value} {spw_name}: not in the Statbel list"
    if code_value in SPW_TYPOS:
        assert spw_name == SPW_TYPOS[code_value], (code_value, spw_name)
        corrected.append((code_value, spw_name, official))
    else:
        assert without_accents(official) == without_accents(spw_name), (code_value, spw_name, official)
    rows.append((code_value, official, percent / 100))

codes = [row[0] for row in rows]
names = [row[1] for row in rows]
assert len(set(codes)) == len(codes) and len(set(names)) == len(names)


def literal(text):
    return '"' + text.replace("\\", "\\\\").replace('"', '\\"') + '"'


lines = [
    f"// Generated from official-docs/municipal-surcharge, the SPW file « Évolution taux PI et IPP depuis",
    f"// 1990 par commune de la Région wallonne », sheet « Taux IPP », column {tax_year}, with the names of",
    f"// Statbel's REFNIS list (official-docs/municipal-surcharge), by",
    f"// tools/municipal-rates/generate_rates.py. Do not edit by hand: regenerate (official-docs/README.md).",
]
for code, name, value in skipped:
    lines.append(f"// Not listed: {name} ({code}), « {value} ».")
lines += [
    "",
    "namespace Netto.Core.PersonalTax;",
    "",
    "internal static partial class WalloonMunicipalities",
    "{",
    f"    public static IReadOnlyList<Municipality> TaxYear{tax_year} {{ get; }} =",
    "    [",
]
for code, name, value in rows:
    lines.append(f"        new({code}, {literal(name)}, TaxYear{tax_year}Rate({value:.3f}m)),")
lines += ["    ];", "}", ""]

with open(output, "w", encoding="utf-8", newline="\n") as file:
    file.write("\n".join(lines))

print(f"{len(rows)} municipalities written, {len(skipped)} skipped: {skipped}; SPW typos corrected: {corrected}")
