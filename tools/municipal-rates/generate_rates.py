"""Turn the SPW « Taux IPP » sheet into Netto.Core's checked-in municipal rates for one tax year.

The SPW file is a legacy .xls, which xlrd 2 reads (requirements.txt). From the repo root, in a
virtual environment outside the repo:

    python -m venv <venv>
    <venv>/Scripts/python -m pip install -r tools/municipal-rates/requirements.txt
    <venv>/Scripts/python -I tools/municipal-rates/generate_rates.py \
        official-docs/municipal-surcharge/<file>.xls 2026 \
        src/Netto.Core/PersonalTax/WalloonMunicipalities.TaxYear2026.cs

It stops on a rate with more than one decimal or outside 0% to 10%, and lists the rows it skips.
"""
import sys
from decimal import Decimal

import xlrd

sys.stdout.reconfigure(encoding="utf-8")

source, tax_year, output = sys.argv[1], int(sys.argv[2]), sys.argv[3]
sheet = xlrd.open_workbook(source).sheet_by_name("Taux IPP")
header = [sheet.cell_value(0, c) for c in range(sheet.ncols)]
assert header[0] == "INS" and header[1] == "Communes", header[:2]
column = header.index(float(tax_year))

rows, skipped = [], []
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
    rows.append((code_value, name.value.strip(), percent / 100))

codes = [row[0] for row in rows]
names = [row[1] for row in rows]
assert len(set(codes)) == len(codes) and len(set(names)) == len(names)


def literal(text):
    return '"' + text.replace("\\", "\\\\").replace('"', '\\"') + '"'


lines = [
    f"// Generated from official-docs/municipal-surcharge, the SPW file « Évolution taux PI et IPP depuis",
    f"// 1990 par commune de la Région wallonne », sheet « Taux IPP », column {tax_year}, by",
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

print(f"{len(rows)} municipalities written, {len(skipped)} skipped: {skipped}")
