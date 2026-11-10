"""Verify all runtime operations and their referenced current TRX cases."""

import argparse
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def verify(matrix, contract, trx):
    operations = {f"{method.upper()} {path}" for path, item in contract["paths"].items()
                  for method in item if method in {"get", "post", "put", "patch", "delete"}}
    seen = set()
    results = [(item.get("testName", ""), item.get("outcome")) for item in ET.parse(trx).iter()
               if item.tag.endswith("UnitTestResult")]
    references = set()
    for entry in matrix["operations"]:
        operation = entry["operation"]
        if operation in seen or operation not in operations:
            raise ValueError(f"Duplicate or absent runtime operation: {operation}")
        seen.add(operation)
        for group in ("happy", "error", "authorization"):
            if not entry["cases"].get(group):
                raise ValueError(f"Missing {group} evidence for {operation}")
            for symbol in entry["cases"][group]:
                cls, method = symbol.split(".")
                source = ROOT / "backend/tests/CulinaryBlog.IntegrationTests" / f"{cls}.cs"
                if not re.search(r"\b" + re.escape(method) + r"\s*\(", source.read_text(encoding="utf-8")):
                    raise ValueError(f"Missing source symbol: {symbol}")
                matches = [outcome for name, outcome in results if re.search(r"(?:^|\.)" + re.escape(symbol) + r"(?:$|\()", name)]
                if not matches or any(outcome != "Passed" for outcome in matches):
                    raise ValueError(f"No complete passing TRX evidence: {symbol}")
                references.add(symbol)
    if operations - seen:
        raise ValueError(f"Runtime operations missing from the matrix: {sorted(operations - seen)}")
    return {"operations": len(seen), "testSymbols": len(references), "runtimeInventory": len(operations)}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--matrix", default=str(ROOT / "backend/tests/CulinaryBlog.IntegrationTests/endpoint-matrix.json"))
    parser.add_argument("--openapi", required=True)
    parser.add_argument("--trx", required=True)
    args = parser.parse_args()
    print(json.dumps(verify(json.loads(Path(args.matrix).read_text(encoding="utf-8")),
                            json.loads(Path(args.openapi).read_text(encoding="utf-8")), args.trx)))
