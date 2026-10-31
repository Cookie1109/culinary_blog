"""Enforce Application-only unit line coverage from one Cobertura report."""

import argparse
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


def check(report):
    report = Path(report)
    if report.is_dir():
        # Coverlet writes <run-id>/coverage.cobertura.xml. TRX also copies
        # attachments below <test-run>/In/<host>; those are not additional runs.
        reports = list(report.glob("*/coverage.cobertura.xml"))
        if len(reports) != 1:
            raise ValueError("Expected exactly one canonical unit coverage report")
        report = reports[0]
    root = ET.parse(report).getroot()
    packages = root.findall("./packages/package[@name='CulinaryBlog.Application']")
    if len(packages) != 1:
        raise ValueError("Expected exactly one CulinaryBlog.Application package")
    # Count executable lines per source file, merging generated async classes.
    lines = {}
    branches = packages[0].get("branch-rate", "unknown")
    for cls in packages[0].findall("./classes/class"):
        for line in cls.findall("./lines/line"):
            key = (cls.attrib["filename"], int(line.attrib["number"]))
            hits = int(line.attrib["hits"])
            if hits < 0:
                raise ValueError("Negative hit count")
            lines[key] = lines.get(key, False) or hits > 0
    if not lines:
        raise ValueError("Application has no executable lines")
    covered, total = sum(lines.values()), len(lines)
    print(f"Application unit lines: {covered}/{total} ({covered / total:.2%}); branch-rate={branches}")
    return 0 if covered * 100 >= total * 80 else 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", help="Cobertura XML or a fresh unit results directory")
    args = parser.parse_args()
    try:
        sys.exit(check(args.report))
    except (ET.ParseError, OSError, ValueError, KeyError) as error:
        print(f"Coverage gate: {error}", file=sys.stderr)
        sys.exit(2)
