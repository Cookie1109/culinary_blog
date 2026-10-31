"""Check OpenAPI operation inventory and referenced integration evidence, without inferring assertions."""

import argparse
import pathlib
import re
import xml.etree.ElementTree as ET

root = pathlib.Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--trx", help="Full integration TRX from the current source")
args = parser.parse_args()
operations = set()
path = None
for line in (root / "docs/specs/phase-0/openapi.v1.yaml").read_text(encoding="utf-8").splitlines():
    match = re.match(r"^  (/.*):$", line)
    if match:
        path = match[1]
    match = re.match(r"^    (get|post|put|patch|delete):$", line)
    if match and path:
        operations.add(f"{match[1].upper()} {path}")
rows = {}
for line in (root / "docs/specs/srs-completion-phases/reports/package-G05-endpoint-matrix.md").read_text(encoding="utf-8").splitlines():
    if re.match(r"^\| (GET|POST|PUT|PATCH|DELETE) /", line):
        cells = [cell.strip().strip("`") for cell in line.split("|")[1:-1]]
        if cells[0] in rows:
            raise SystemExit(f"Duplicate operation: {cells[0]}")
        rows[cells[0]] = cells
if set(rows) != operations:
    raise SystemExit(f"Missing: {operations - set(rows)}; stale: {set(rows) - operations}")
passed = set()
if args.trx:
    document = ET.parse(args.trx)
    for result in document.iter():
        if result.tag.endswith("UnitTestResult") and result.get("outcome") == "Passed":
            passed.add(result.attrib["testName"])
for operation, cells in rows.items():
    for symbol in cells[2:4]:
        cls, method = symbol.split(".")
        source = root / "backend/tests/CulinaryBlog.IntegrationTests" / (cls + ".cs")
        if not re.search(r"\b" + re.escape(method) + r"\(", source.read_text(encoding="utf-8")):
            raise SystemExit(f"Missing test symbol: {operation} -> {symbol}")
        if args.trx and not any(f"{symbol}(" in name or name.endswith(symbol) for name in passed):
            raise SystemExit(f"No passing TRX case: {operation} -> {symbol}")
print(f"Matrix: {len(rows)} operations, happy/error symbols exist" + (" and passed in supplied TRX" if args.trx else "; runtime not checked"))
